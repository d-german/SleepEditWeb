using SleepEditWeb.Models;

namespace SleepEditWeb.Services;

public interface IEditorInsertionService
{
    EditorInsertionResult Apply(string content, string narrative, EditorInsertionMode mode, int cursorIndex);
}

public sealed class EditorInsertionService : IEditorInsertionService
{
    private const string MedicationsHeading = "Medications:";

    public EditorInsertionResult Apply(string content, string narrative, EditorInsertionMode mode, int cursorIndex)
    {
        var safeContent = content ?? string.Empty;
        var safeNarrative = narrative?.Trim() ?? string.Empty;

        return mode switch
        {
            EditorInsertionMode.InsertAtCursor => InsertAtCursor(safeContent, safeNarrative, cursorIndex),
            EditorInsertionMode.ReplaceMedicationSection => ReplaceMedicationSection(safeContent, safeNarrative),
            _ => CopyToClipboard(safeContent, safeNarrative)
        };
    }

    private static EditorInsertionResult InsertAtCursor(string content, string narrative, int cursorIndex)
    {
        var insertionPoint = Math.Clamp(cursorIndex, 0, content.Length);
        var updated = content.Insert(insertionPoint, BuildInsertionText(narrative));

        return new EditorInsertionResult
        {
            UpdatedContent = updated,
            AppliedMode = EditorInsertionMode.InsertAtCursor,
            CopyText = null
        };
    }

    private static EditorInsertionResult CopyToClipboard(string content, string narrative)
    {
        return new EditorInsertionResult
        {
            UpdatedContent = content,
            AppliedMode = EditorInsertionMode.CopyToClipboard,
            CopyText = narrative
        };
    }

    private static EditorInsertionResult ReplaceMedicationSection(string content, string narrative)
    {
        var lines = SplitLines(content);
        var headingIndex = FindMedicationHeading(lines);

        if (headingIndex < 0)
        {
            return AppendMedicationSection(content, narrative);
        }

        var nextHeading = FindNextHeading(lines, headingIndex + 1);
        var updated = ReplaceLines(lines, headingIndex, nextHeading, narrative);

        return new EditorInsertionResult
        {
            UpdatedContent = updated,
            AppliedMode = EditorInsertionMode.ReplaceMedicationSection,
            CopyText = null
        };
    }

    private static EditorInsertionResult AppendMedicationSection(string content, string narrative)
    {
        var suffix = content.EndsWith("\n", StringComparison.Ordinal) || content.Length == 0
            ? string.Empty
            : Environment.NewLine;

        var updated = $"{content}{suffix}{MedicationsHeading}{Environment.NewLine}{narrative}{Environment.NewLine}";

        return new EditorInsertionResult
        {
            UpdatedContent = updated,
            AppliedMode = EditorInsertionMode.ReplaceMedicationSection,
            CopyText = null
        };
    }

    private static int FindMedicationHeading(IReadOnlyList<string> lines)
    {
        for (var index = 0; index < lines.Count; index++)
        {
            if (lines[index].Trim().Equals(MedicationsHeading, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private static int FindNextHeading(IReadOnlyList<string> lines, int startIndex)
    {
        for (var index = startIndex; index < lines.Count; index++)
        {
            if (IsHeading(lines[index]))
            {
                return index;
            }
        }

        return lines.Count;
    }

    private static bool IsHeading(string line)
    {
        var trimmed = line.Trim();
        return trimmed.EndsWith(":", StringComparison.Ordinal) && trimmed.Length > 1;
    }

    private static string ReplaceLines(IReadOnlyList<string> lines, int headingIndex, int nextHeading, string narrative)
    {
        var kept = lines.Take(headingIndex + 1).ToList();
        kept.Add(narrative);
        kept.AddRange(lines.Skip(nextHeading));
        return string.Join(Environment.NewLine, kept);
    }

    private static string BuildInsertionText(string narrative)
    {
        return narrative + Environment.NewLine;
    }

    private static string[] SplitLines(string content)
    {
        return content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
    }
}
