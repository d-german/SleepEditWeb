using SleepEditWeb.Models;

namespace SleepEditWeb.Protocol.Domain;

public static class ProtocolTreeMapper
{
    public static ProtocolTreeDocument ToDomain(ProtocolDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return new ProtocolTreeDocument(
            Id: document.Id,
            LinkId: document.LinkId,
            LinkText: document.LinkText ?? string.Empty,
            Text: document.Text ?? string.Empty,
            Sections: document.Sections.Select(ToDomainNode).ToList());
    }

    public static ProtocolDocument ToMutable(ProtocolTreeDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return new ProtocolDocument
        {
            Id = document.Id,
            LinkId = document.LinkId,
            LinkText = document.LinkText ?? string.Empty,
            Text = document.Text ?? string.Empty,
            Sections = document.Sections.Select(ToMutableNode).ToList()
        };
    }

    private static ProtocolTreeNode ToDomainNode(ProtocolNodeModel node)
    {
        return new ProtocolTreeNode(
            Id: node.Id,
            LinkId: node.LinkId,
            LinkText: node.LinkText ?? string.Empty,
            Text: node.Text ?? string.Empty,
            Kind: ToDomainKind(node.Kind),
            SubText: node.SubText
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .ToList(),
            Children: node.Children.Select(ToDomainNode).ToList());
    }

    private static ProtocolNodeModel ToMutableNode(ProtocolTreeNode node)
    {
        return new ProtocolNodeModel
        {
            Id = node.Id,
            LinkId = node.LinkId,
            LinkText = node.LinkText ?? string.Empty,
            Text = node.Text ?? string.Empty,
            Kind = ToMutableKind(node.Kind),
            SubText = node.SubText
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .ToList(),
            Children = node.Children.Select(ToMutableNode).ToList()
        };
    }

    private static ProtocolTreeNodeKind ToDomainKind(ProtocolNodeKind kind)
    {
        return kind switch
        {
            ProtocolNodeKind.Root => ProtocolTreeNodeKind.Root,
            ProtocolNodeKind.Section => ProtocolTreeNodeKind.Section,
            _ => ProtocolTreeNodeKind.SubSection
        };
    }

    private static ProtocolNodeKind ToMutableKind(ProtocolTreeNodeKind kind)
    {
        return kind switch
        {
            ProtocolTreeNodeKind.Root => ProtocolNodeKind.Root,
            ProtocolTreeNodeKind.Section => ProtocolNodeKind.Section,
            _ => ProtocolNodeKind.SubSection
        };
    }
}
