namespace SleepEditWeb.Protocol.Domain;

public static class ProtocolTreeFunctions
{
    public static ProtocolTreeNode? FindNode(ProtocolTreeDocument document, int id)
    {
        return FindNode(document.Sections, id);
    }

    public static int NextId(ProtocolTreeDocument document)
    {
        var max = document.Sections.Select(GetMaxId).DefaultIfEmpty(0).Max();
        return max + 1;
    }

    public static ProtocolTreeDocument AddSection(ProtocolTreeDocument document, string text)
    {
        var sections = document.Sections.ToList();
        sections.Add(CreateNode(document, text, ProtocolTreeNodeKind.Section));
        return document with { Sections = sections };
    }

    public static ProtocolTreeDocument AddChild(ProtocolTreeDocument document, int parentId, string text)
    {
        var child = CreateNode(document, text, ProtocolTreeNodeKind.SubSection);
        if (!TryAddChild(document.Sections, parentId, child, out var updatedSections))
        {
            return document;
        }

        return document with { Sections = updatedSections };
    }

    public static ProtocolTreeDocument RemoveNode(ProtocolTreeDocument document, int nodeId)
    {
        if (!TryDetachNode(document.Sections, nodeId, out var removed, out var remainingSections) || removed is null)
        {
            return document;
        }

        var withClearedLinks = ClearInboundLinks(remainingSections, removed.Id);
        return document with { Sections = withClearedLinks };
    }

    public static ProtocolTreeDocument UpdateNode(
        ProtocolTreeDocument document,
        int nodeId,
        string text,
        int linkId,
        string linkText)
    {
        if (!TryUpdateNode(
                document.Sections,
                nodeId,
                node => node with
                {
                    Text = text ?? string.Empty,
                    LinkId = linkId,
                    LinkText = linkText ?? string.Empty
                },
                out var updatedSections))
        {
            return document;
        }

        return document with { Sections = updatedSections };
    }

    public static ProtocolTreeDocument MoveNode(ProtocolTreeDocument document, int nodeId, int parentId, int targetIndex)
    {
        var moving = FindNode(document, nodeId);
        if (moving == null || ContainsNode(moving, parentId))
        {
            return document;
        }

        if (!CanResolveMoveTarget(document, moving.Kind, parentId))
        {
            return document;
        }

        if (!TryDetachNode(document.Sections, nodeId, out var removed, out var remainingSections) || removed is null)
        {
            return document;
        }

        var normalized = removed with
        {
            Kind = parentId == 0 ? ProtocolTreeNodeKind.Section : ProtocolTreeNodeKind.SubSection
        };

        if (parentId == 0)
        {
            var root = remainingSections.ToList();
            var index = Math.Clamp(targetIndex, 0, root.Count);
            root.Insert(index, normalized);
            return document with { Sections = root };
        }

        if (!TryInsertChild(remainingSections, parentId, normalized, targetIndex, out var updatedSections))
        {
            return document;
        }

        return document with { Sections = updatedSections };
    }

    public static ProtocolTreeDocument AddSubText(ProtocolTreeDocument document, int nodeId, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return document;
        }

        var normalized = value.Trim();
        if (!TryUpdateNode(
                document.Sections,
                nodeId,
                node =>
                {
                    var updatedSubText = node.SubText.ToList();
                    updatedSubText.Add(normalized);
                    return node with { SubText = updatedSubText };
                },
                out var updatedSections))
        {
            return document;
        }

        return document with { Sections = updatedSections };
    }

    public static ProtocolTreeDocument RemoveSubText(ProtocolTreeDocument document, int nodeId, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return document;
        }

        if (!TryUpdateNode(
                document.Sections,
                nodeId,
                node =>
                {
                    var updatedSubText = node.SubText.ToList();
                    var index = updatedSubText.FindIndex(item => item.Equals(value, StringComparison.OrdinalIgnoreCase));
                    if (index >= 0)
                    {
                        updatedSubText.RemoveAt(index);
                    }

                    return node with { SubText = updatedSubText };
                },
                out var updatedSections))
        {
            return document;
        }

        return document with { Sections = updatedSections };
    }

    public static bool ContainsNode(ProtocolTreeNode node, int id)
    {
        if (node.Id == id)
        {
            return true;
        }

        return node.Children.Any(child => ContainsNode(child, id));
    }

    private static ProtocolTreeNode? FindNode(IReadOnlyList<ProtocolTreeNode> nodes, int id)
    {
        foreach (var node in nodes)
        {
            if (node.Id == id)
            {
                return node;
            }

            var found = FindNode(node.Children, id);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static ProtocolTreeNode CreateNode(ProtocolTreeDocument document, string text, ProtocolTreeNodeKind kind)
    {
        return new ProtocolTreeNode(
            Id: NextId(document),
            LinkId: -1,
            LinkText: string.Empty,
            Text: string.IsNullOrWhiteSpace(text) ? "New Node" : text.Trim(),
            Kind: kind,
            SubText: Array.Empty<string>(),
            Children: Array.Empty<ProtocolTreeNode>());
    }

    private static int GetMaxId(ProtocolTreeNode node)
    {
        var childMax = node.Children.Select(GetMaxId).DefaultIfEmpty(node.Id).Max();
        return Math.Max(node.Id, childMax);
    }

    private static bool TryAddChild(
        IReadOnlyList<ProtocolTreeNode> nodes,
        int parentId,
        ProtocolTreeNode child,
        out IReadOnlyList<ProtocolTreeNode> updatedNodes)
    {
        var changed = false;
        var result = new List<ProtocolTreeNode>(nodes.Count);

        foreach (var node in nodes)
        {
            if (node.Id == parentId)
            {
                var children = node.Children.ToList();
                children.Add(child);
                result.Add(node with { Children = children });
                changed = true;
                continue;
            }

            if (TryAddChild(node.Children, parentId, child, out var updatedChildren))
            {
                result.Add(node with { Children = updatedChildren });
                changed = true;
                continue;
            }

            result.Add(node);
        }

        updatedNodes = changed ? result : nodes;
        return changed;
    }

    private static bool TryUpdateNode(
        IReadOnlyList<ProtocolTreeNode> nodes,
        int nodeId,
        Func<ProtocolTreeNode, ProtocolTreeNode> updater,
        out IReadOnlyList<ProtocolTreeNode> updatedNodes)
    {
        var changed = false;
        var result = new List<ProtocolTreeNode>(nodes.Count);

        foreach (var node in nodes)
        {
            if (node.Id == nodeId)
            {
                result.Add(updater(node));
                changed = true;
                continue;
            }

            if (TryUpdateNode(node.Children, nodeId, updater, out var updatedChildren))
            {
                result.Add(node with { Children = updatedChildren });
                changed = true;
                continue;
            }

            result.Add(node);
        }

        updatedNodes = changed ? result : nodes;
        return changed;
    }

    private static bool TryDetachNode(
        IReadOnlyList<ProtocolTreeNode> nodes,
        int nodeId,
        out ProtocolTreeNode? removed,
        out IReadOnlyList<ProtocolTreeNode> remainingNodes)
    {
        removed = null;
        var changed = false;
        var result = new List<ProtocolTreeNode>(nodes.Count);

        foreach (var node in nodes)
        {
            if (node.Id == nodeId)
            {
                removed = node;
                changed = true;
                continue;
            }

            if (TryDetachNode(node.Children, nodeId, out var nestedRemoved, out var updatedChildren))
            {
                removed ??= nestedRemoved;
                result.Add(node with { Children = updatedChildren });
                changed = true;
                continue;
            }

            result.Add(node);
        }

        remainingNodes = changed ? result : nodes;
        return removed is not null;
    }

    private static IReadOnlyList<ProtocolTreeNode> ClearInboundLinks(IReadOnlyList<ProtocolTreeNode> nodes, int removedId)
    {
        var changed = false;
        var result = new List<ProtocolTreeNode>(nodes.Count);

        foreach (var node in nodes)
        {
            var updatedChildren = ClearInboundLinks(node.Children, removedId);
            var next = node;

            if (node.LinkId == removedId)
            {
                next = next with
                {
                    LinkId = -1,
                    LinkText = string.Empty
                };
                changed = true;
            }

            if (!ReferenceEquals(updatedChildren, node.Children))
            {
                next = next with { Children = updatedChildren };
                changed = true;
            }

            result.Add(next);
        }

        return changed ? result : nodes;
    }

    private static bool CanResolveMoveTarget(ProtocolTreeDocument document, ProtocolTreeNodeKind movingKind, int parentId)
    {
        if (movingKind == ProtocolTreeNodeKind.Section)
        {
            return parentId == 0;
        }

        if (parentId == 0)
        {
            return false;
        }

        return FindNode(document, parentId) != null;
    }

    private static bool TryInsertChild(
        IReadOnlyList<ProtocolTreeNode> nodes,
        int parentId,
        ProtocolTreeNode child,
        int targetIndex,
        out IReadOnlyList<ProtocolTreeNode> updatedNodes)
    {
        var changed = false;
        var result = new List<ProtocolTreeNode>(nodes.Count);

        foreach (var node in nodes)
        {
            if (node.Id == parentId)
            {
                var children = node.Children.ToList();
                var index = Math.Clamp(targetIndex, 0, children.Count);
                children.Insert(index, child);
                result.Add(node with { Children = children });
                changed = true;
                continue;
            }

            if (TryInsertChild(node.Children, parentId, child, targetIndex, out var updatedChildren))
            {
                result.Add(node with { Children = updatedChildren });
                changed = true;
                continue;
            }

            result.Add(node);
        }

        updatedNodes = changed ? result : nodes;
        return changed;
    }
}
