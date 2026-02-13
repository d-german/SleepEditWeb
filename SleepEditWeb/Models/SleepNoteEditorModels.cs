namespace SleepEditWeb.Models;

public enum EditorInsertionMode
{
    InsertAtCursor = 0,
    ReplaceMedicationSection = 1,
    CopyToClipboard = 2
}

public sealed class MedicationSelection
{
    public required string Name { get; init; }

    public bool IsKnown { get; init; }
}

public sealed class MedicationNarrative
{
    public required string Text { get; init; }

    public IReadOnlyList<string> UnknownMedications { get; init; } = [];
}

public sealed class EditorInsertionResult
{
    public required string UpdatedContent { get; init; }

    public required EditorInsertionMode AppliedMode { get; init; }

    public string? CopyText { get; init; }
}

public sealed class SleepNoteEditorSnapshot
{
    public string DocumentContent { get; init; } = string.Empty;

    public IReadOnlyList<MedicationSelection> SelectedMedications { get; init; } = [];

    public DateTimeOffset LastUpdatedUtc { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class SleepNoteEditorViewModel
{
    public required string InitialContent { get; init; }

    public required IReadOnlyList<MedicationSelection> SelectedMedications { get; init; }

    public required IReadOnlyList<string> MedicationSuggestions { get; init; }
}

public sealed class SleepNoteEditorFeatureOptions
{
    public const string SectionName = "Features";

    public bool SleepNoteEditorEnabled { get; init; } = true;
}
