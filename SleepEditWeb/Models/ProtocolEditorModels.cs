using SleepEditWeb.Protocol.Domain;

namespace SleepEditWeb.Models;

public enum ProtocolNodeKind
{
    Root = 0,
    Section = 1,
    SubSection = 2
}

public sealed class ProtocolDocument
{
    public int Id { get; set; } = -1;

    public int LinkId { get; set; } = -1;

    public string LinkText { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public List<ProtocolNodeModel> Sections { get; set; } = [];
}

public sealed class ProtocolNodeModel
{
    public int Id { get; set; } = -1;

    public int LinkId { get; set; } = -1;

    public string LinkText { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public ProtocolNodeKind Kind { get; set; } = ProtocolNodeKind.SubSection;

    public List<string> SubText { get; set; } = [];

    public List<ProtocolNodeModel> Children { get; set; } = [];
}

public sealed class ProtocolEditorSnapshot
{
    public ProtocolDocument Document { get; set; } = new();

    public List<ProtocolDocument> UndoHistory { get; set; } = [];

    public List<ProtocolDocument> RedoHistory { get; set; } = [];

    public List<ProtocolTreeDocument> UndoDomainHistory { get; set; } = [];

    public List<ProtocolTreeDocument> RedoDomainHistory { get; set; } = [];

    public DateTimeOffset LastUpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ProtocolEditorViewModel
{
    public ProtocolDocument InitialDocument { get; set; } = new();
}

public sealed class ProtocolEditorFeatureOptions
{
    public const string SectionName = "Features";

    public bool ProtocolEditorEnabled { get; init; } = true;
}

public sealed class ProtocolEditorStartupOptions
{
    public const string SectionName = "ProtocolEditor";

    public string DefaultProtocolPath { get; init; } = string.Empty;

    public string StartupProtocolPath { get; init; } = string.Empty;

    public string SaveProtocolPath { get; init; } = string.Empty;
}
