namespace SleepEditWeb.Protocol.Domain;

public enum ProtocolTreeNodeKind
{
    Root = 0,
    Section = 1,
    SubSection = 2
}

public sealed record ProtocolTreeDocument(
    int Id,
    int LinkId,
    string LinkText,
    string Text,
    IReadOnlyList<ProtocolTreeNode> Sections)
{
    public static ProtocolTreeDocument Empty { get; } = new(
        Id: -1,
        LinkId: -1,
        LinkText: string.Empty,
        Text: string.Empty,
        Sections: Array.Empty<ProtocolTreeNode>());
}

public sealed record ProtocolTreeNode(
    int Id,
    int LinkId,
    string LinkText,
    string Text,
    ProtocolTreeNodeKind Kind,
    IReadOnlyList<string> SubText,
    IReadOnlyList<ProtocolTreeNode> Children);
