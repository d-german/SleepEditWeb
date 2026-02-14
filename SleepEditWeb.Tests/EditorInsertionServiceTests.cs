using SleepEditWeb.Models;
using SleepEditWeb.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace SleepEditWeb.Tests;

[TestFixture]
public class EditorInsertionServiceTests
{
    private EditorInsertionService _service;

    [SetUp]
    public void SetUp()
    {
        _service = new EditorInsertionService(NullLogger<EditorInsertionService>.Instance);
    }

    [Test]
    public void Apply_InsertAtCursor_ClampsCursorAndInsertsTrimmedNarrative()
    {
        // Arrange
        const string content = "ABCDE";
        const string narrative = "  Medications: Aspirin.  ";

        // Act
        var result = _service.Apply(content, narrative, EditorInsertionMode.InsertAtCursor, -10);

        // Assert
        Assert.That(result.AppliedMode, Is.EqualTo(EditorInsertionMode.InsertAtCursor));
        Assert.That(result.UpdatedContent, Is.EqualTo($"Medications: Aspirin.{Environment.NewLine}ABCDE"));
        Assert.That(result.CopyText, Is.Null);
    }

    [Test]
    public void Apply_CopyToClipboard_DoesNotModifyContent_AndSetsCopyText()
    {
        // Arrange
        const string content = "Existing note";
        const string narrative = "Medications: Aspirin.";

        // Act
        var result = _service.Apply(content, narrative, EditorInsertionMode.CopyToClipboard, 3);

        // Assert
        Assert.That(result.AppliedMode, Is.EqualTo(EditorInsertionMode.CopyToClipboard));
        Assert.That(result.UpdatedContent, Is.EqualTo(content));
        Assert.That(result.CopyText, Is.EqualTo(narrative));
    }

    [Test]
    public void Apply_ReplaceMedicationSection_WhenHeadingExists_ReplacesOnlyMedicationBlock()
    {
        // Arrange
        var content = string.Join(
            Environment.NewLine,
            "History:",
            "Old history",
            "Medications:",
            "Old med line 1",
            "Old med line 2",
            "Assessment:",
            "Plan");

        const string narrative = "Medications: Aspirin.";

        // Act
        var result = _service.Apply(content, narrative, EditorInsertionMode.ReplaceMedicationSection, 0);

        // Assert
        var expected = string.Join(
            Environment.NewLine,
            "History:",
            "Old history",
            "Medications:",
            "Medications: Aspirin.",
            "Assessment:",
            "Plan");

        Assert.That(result.AppliedMode, Is.EqualTo(EditorInsertionMode.ReplaceMedicationSection));
        Assert.That(result.UpdatedContent, Is.EqualTo(expected));
        Assert.That(result.CopyText, Is.Null);
    }

    [Test]
    public void Apply_ReplaceMedicationSection_WhenHeadingMissing_AppendsMedicationSection()
    {
        // Arrange
        var content = string.Join(Environment.NewLine, "History:", "Old history");
        const string narrative = "Medications: none documented.";

        // Act
        var result = _service.Apply(content, narrative, EditorInsertionMode.ReplaceMedicationSection, 0);

        // Assert
        var expected =
            $"{content}{Environment.NewLine}Medications:{Environment.NewLine}{narrative}{Environment.NewLine}";

        Assert.That(result.AppliedMode, Is.EqualTo(EditorInsertionMode.ReplaceMedicationSection));
        Assert.That(result.UpdatedContent, Is.EqualTo(expected));
        Assert.That(result.CopyText, Is.Null);
    }
}
