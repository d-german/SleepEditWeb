using System.Text.Json;
using SleepEditWeb.Models;

namespace SleepEditWeb.Services;

public interface IProtocolEditorService
{
    ProtocolEditorSnapshot Load();

    ProtocolEditorSnapshot AddSection(string text);

    ProtocolEditorSnapshot AddChild(int parentId, string text);

    ProtocolEditorSnapshot RemoveNode(int nodeId);

    ProtocolEditorSnapshot UpdateNode(int nodeId, string text, int linkId, string linkText);

    ProtocolEditorSnapshot MoveNode(int nodeId, int parentId, int targetIndex);

    ProtocolEditorSnapshot AddSubText(int nodeId, string value);

    ProtocolEditorSnapshot RemoveSubText(int nodeId, string value);

    ProtocolEditorSnapshot Undo();

    ProtocolEditorSnapshot Redo();

    ProtocolEditorSnapshot Reset();

    string ExportXml();
}

public sealed class ProtocolEditorService : IProtocolEditorService
{
    private readonly IProtocolEditorSessionStore _sessionStore;
    private readonly IProtocolXmlService _xmlService;

    public ProtocolEditorService(
        IProtocolEditorSessionStore sessionStore,
        IProtocolXmlService xmlService)
    {
        _sessionStore = sessionStore;
        _xmlService = xmlService;
    }

    public ProtocolEditorSnapshot Load()
    {
        return _sessionStore.Load();
    }

    public ProtocolEditorSnapshot AddSection(string text)
    {
        return ApplyMutation(document =>
        {
            document.Sections.Add(CreateNode(document, text, ProtocolNodeKind.Section));
        });
    }

    public ProtocolEditorSnapshot AddChild(int parentId, string text)
    {
        return ApplyMutation(document =>
        {
            var parent = FindNode(document.Sections, parentId);
            if (parent == null)
            {
                return;
            }

            parent.Children.Add(CreateNode(document, text, ProtocolNodeKind.SubSection));
        });
    }

    public ProtocolEditorSnapshot RemoveNode(int nodeId)
    {
        return ApplyMutation(document =>
        {
            if (!TryDetachNode(document, nodeId, out var removed))
            {
                return;
            }

            ClearInboundLinks(document.Sections, removed.Id);
        });
    }

    public ProtocolEditorSnapshot UpdateNode(int nodeId, string text, int linkId, string linkText)
    {
        return ApplyMutation(document =>
        {
            var node = FindNode(document.Sections, nodeId);
            if (node == null)
            {
                return;
            }

            node.Text = text ?? string.Empty;
            node.LinkId = linkId;
            node.LinkText = linkText ?? string.Empty;
        });
    }

    public ProtocolEditorSnapshot MoveNode(int nodeId, int parentId, int targetIndex)
    {
        return ApplyMutation(document =>
        {
            var moving = FindNode(document.Sections, nodeId);
            if (moving == null || ContainsNode(moving, parentId))
            {
                return;
            }

            if (!TryDetachNode(document, nodeId, out var detached))
            {
                return;
            }

            var target = ResolveTargetList(document, parentId, detached.Kind);
            if (target == null)
            {
                return;
            }

            var index = Math.Clamp(targetIndex, 0, target.Count);
            detached.Kind = parentId == 0 ? ProtocolNodeKind.Section : ProtocolNodeKind.SubSection;
            target.Insert(index, detached);
        });
    }

    public ProtocolEditorSnapshot AddSubText(int nodeId, string value)
    {
        return ApplyMutation(document =>
        {
            var node = FindNode(document.Sections, nodeId);
            if (node == null || string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            node.SubText.Add(value.Trim());
        });
    }

    public ProtocolEditorSnapshot RemoveSubText(int nodeId, string value)
    {
        return ApplyMutation(document =>
        {
            var node = FindNode(document.Sections, nodeId);
            if (node == null || string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var index = node.SubText.FindIndex(item => item.Equals(value, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                node.SubText.RemoveAt(index);
            }
        });
    }

    public ProtocolEditorSnapshot Undo()
    {
        var snapshot = _sessionStore.Load();
        if (snapshot.UndoHistory.Count == 0)
        {
            return snapshot;
        }

        var undoHistory = CloneHistory(snapshot.UndoHistory);
        var restored = undoHistory[^1];
        undoHistory.RemoveAt(undoHistory.Count - 1);

        var redoHistory = CloneHistory(snapshot.RedoHistory);
        redoHistory.Add(CloneDocument(snapshot.Document));

        var next = new ProtocolEditorSnapshot
        {
            Document = restored,
            UndoHistory = undoHistory,
            RedoHistory = redoHistory,
            LastUpdatedUtc = DateTimeOffset.UtcNow
        };

        _sessionStore.Save(next);
        return next;
    }

    public ProtocolEditorSnapshot Redo()
    {
        var snapshot = _sessionStore.Load();
        if (snapshot.RedoHistory.Count == 0)
        {
            return snapshot;
        }

        var redoHistory = CloneHistory(snapshot.RedoHistory);
        var restored = redoHistory[^1];
        redoHistory.RemoveAt(redoHistory.Count - 1);

        var undoHistory = CloneHistory(snapshot.UndoHistory);
        undoHistory.Add(CloneDocument(snapshot.Document));

        var next = new ProtocolEditorSnapshot
        {
            Document = restored,
            UndoHistory = undoHistory,
            RedoHistory = redoHistory,
            LastUpdatedUtc = DateTimeOffset.UtcNow
        };

        _sessionStore.Save(next);
        return next;
    }

    public ProtocolEditorSnapshot Reset()
    {
        _sessionStore.Reset();
        return _sessionStore.Load();
    }

    public string ExportXml()
    {
        var snapshot = _sessionStore.Load();
        return _xmlService.Serialize(snapshot.Document);
    }

    private ProtocolEditorSnapshot ApplyMutation(Action<ProtocolDocument> mutation)
    {
        var snapshot = _sessionStore.Load();
        var current = CloneDocument(snapshot.Document);
        var original = CloneDocument(snapshot.Document);
        mutation(current);

        var undoHistory = CloneHistory(snapshot.UndoHistory);
        undoHistory.Add(original);

        if (undoHistory.Count > 100)
        {
            undoHistory.RemoveAt(0);
        }

        var next = new ProtocolEditorSnapshot
        {
            Document = current,
            UndoHistory = undoHistory,
            RedoHistory = [],
            LastUpdatedUtc = DateTimeOffset.UtcNow
        };

        _sessionStore.Save(next);
        return next;
    }

    private static void ClearInboundLinks(List<ProtocolNodeModel> nodes, int removedId)
    {
        foreach (var node in nodes)
        {
            if (node.LinkId == removedId)
            {
                node.LinkId = -1;
                node.LinkText = string.Empty;
            }

            ClearInboundLinks(node.Children, removedId);
        }
    }

    private static List<ProtocolNodeModel>? ResolveTargetList(ProtocolDocument document, int parentId, ProtocolNodeKind movingKind)
    {
        if (movingKind == ProtocolNodeKind.Section)
        {
            return parentId == 0 ? document.Sections : null;
        }

        if (parentId == 0)
        {
            return null;
        }

        var parent = FindNode(document.Sections, parentId);
        if (parent == null)
        {
            return null;
        }

        return parent.Children;
    }

    private static bool TryDetachNode(ProtocolDocument document, int nodeId, out ProtocolNodeModel removed)
    {
        if (TryDetachFromList(document.Sections, nodeId, out removed))
        {
            return true;
        }

        foreach (var section in document.Sections)
        {
            if (TryDetachFromList(section.Children, nodeId, out removed))
            {
                return true;
            }
        }

        removed = new ProtocolNodeModel();
        return false;
    }

    private static bool TryDetachFromList(List<ProtocolNodeModel> nodes, int nodeId, out ProtocolNodeModel removed)
    {
        for (var index = 0; index < nodes.Count; index++)
        {
            if (nodes[index].Id == nodeId)
            {
                removed = nodes[index];
                nodes.RemoveAt(index);
                return true;
            }

            if (TryDetachFromList(nodes[index].Children, nodeId, out removed))
            {
                return true;
            }
        }

        removed = new ProtocolNodeModel();
        return false;
    }

    private static ProtocolNodeModel? FindNode(List<ProtocolNodeModel> nodes, int id)
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

    private static bool ContainsNode(ProtocolNodeModel node, int id)
    {
        if (node.Id == id)
        {
            return true;
        }

        return node.Children.Any(child => ContainsNode(child, id));
    }

    private static ProtocolNodeModel CreateNode(ProtocolDocument document, string text, ProtocolNodeKind kind)
    {
        return new ProtocolNodeModel
        {
            Id = NextId(document),
            LinkId = -1,
            LinkText = string.Empty,
            Text = string.IsNullOrWhiteSpace(text) ? "New Node" : text.Trim(),
            Kind = kind,
            SubText = [],
            Children = []
        };
    }

    private static int NextId(ProtocolDocument document)
    {
        var max = document.Sections.Select(GetMaxId).DefaultIfEmpty(0).Max();
        return max + 1;
    }

    private static int GetMaxId(ProtocolNodeModel node)
    {
        var childMax = node.Children.Select(GetMaxId).DefaultIfEmpty(node.Id).Max();
        return Math.Max(node.Id, childMax);
    }

    private static ProtocolDocument CloneDocument(ProtocolDocument document)
    {
        var json = JsonSerializer.Serialize(document);
        return JsonSerializer.Deserialize<ProtocolDocument>(json) ?? new ProtocolDocument();
    }

    private static List<ProtocolDocument> CloneHistory(List<ProtocolDocument> history)
    {
        return history.Select(CloneDocument).ToList();
    }
}
