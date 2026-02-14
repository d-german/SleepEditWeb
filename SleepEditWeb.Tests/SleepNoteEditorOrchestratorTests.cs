using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using SleepEditWeb.Data;
using SleepEditWeb.Models;
using SleepEditWeb.Services;

namespace SleepEditWeb.Tests;

[TestFixture]
public class SleepNoteEditorOrchestratorTests
{
    private Mock<IMedicationRepository> _repository;
    private Mock<IMedicationNarrativeBuilder> _narrativeBuilder;
    private Mock<IEditorInsertionService> _insertionService;
    private Mock<ISleepNoteEditorSessionStore> _sessionStore;
    private SleepNoteEditorOrchestrator _orchestrator;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<IMedicationRepository>();
        _narrativeBuilder = new Mock<IMedicationNarrativeBuilder>();
        _insertionService = new Mock<IEditorInsertionService>();
        _sessionStore = new Mock<ISleepNoteEditorSessionStore>();

        _orchestrator = new SleepNoteEditorOrchestrator(
            _repository.Object,
            _narrativeBuilder.Object,
            _insertionService.Object,
            _sessionStore.Object,
            NullLogger<SleepNoteEditorOrchestrator>.Instance);
    }

    [Test]
    public void Complete_UsesCollaborators_MapsOutput_AndPersistsSnapshot()
    {
        // Arrange
        var request = new SleepNoteEditorCompletionRequest
        {
            EditorContent = "Initial content",
            SelectedMedications = new[] { "Aspirin", "UnknownMed" },
            Mode = EditorInsertionMode.InsertAtCursor,
            CursorIndex = 4
        };

        var medicationNarrative = new MedicationNarrative
        {
            Text = "Medications: Aspirin, UnknownMed [UNKNOWN MEDICATION].",
            UnknownMedications = new[] { "UnknownMed" }
        };

        var insertionResult = new EditorInsertionResult
        {
            UpdatedContent = "Updated content",
            AppliedMode = EditorInsertionMode.CopyToClipboard,
            CopyText = "Medications: Aspirin, UnknownMed [UNKNOWN MEDICATION]."
        };

        SleepNoteEditorSnapshot? persistedSnapshot = null;
        var saveWindowStart = DateTimeOffset.UtcNow;

        _repository.Setup(repo => repo.GetAllMedicationNames()).Returns(new[] { "aspirin" });
        _narrativeBuilder
            .Setup(builder => builder.Build(
                request.SelectedMedications,
                It.Is<IReadOnlySet<string>>(set =>
                    set.Contains("Aspirin"))))
            .Returns(medicationNarrative);

        _insertionService
            .Setup(service => service.Apply(
                request.EditorContent,
                medicationNarrative.Text,
                request.Mode,
                request.CursorIndex))
            .Returns(insertionResult);

        _sessionStore
            .Setup(store => store.Save(It.IsAny<SleepNoteEditorSnapshot>()))
            .Callback<SleepNoteEditorSnapshot>(snapshot => persistedSnapshot = snapshot);

        // Act
        var result = _orchestrator.Complete(request);
        var saveWindowEnd = DateTimeOffset.UtcNow;

        // Assert
        Assert.That(result.UpdatedContent, Is.EqualTo(insertionResult.UpdatedContent));
        Assert.That(result.Narrative, Is.EqualTo(medicationNarrative.Text));
        Assert.That(result.AppliedMode, Is.EqualTo(insertionResult.AppliedMode));
        Assert.That(result.UnknownMedications, Is.EqualTo(medicationNarrative.UnknownMedications));
        Assert.That(result.CopyText, Is.EqualTo(insertionResult.CopyText));

        Assert.That(result.SelectedMedications.Count, Is.EqualTo(2));
        Assert.That(result.SelectedMedications[0].Name, Is.EqualTo("Aspirin"));
        Assert.That(result.SelectedMedications[0].IsKnown, Is.True);
        Assert.That(result.SelectedMedications[1].Name, Is.EqualTo("UnknownMed"));
        Assert.That(result.SelectedMedications[1].IsKnown, Is.False);

        _repository.Verify(repo => repo.GetAllMedicationNames(), Times.Once);
        _narrativeBuilder.Verify(builder => builder.Build(
            request.SelectedMedications,
            It.IsAny<IReadOnlySet<string>>()), Times.Once);
        _insertionService.Verify(service => service.Apply(
            request.EditorContent,
            medicationNarrative.Text,
            request.Mode,
            request.CursorIndex), Times.Once);
        _sessionStore.Verify(store => store.Save(It.IsAny<SleepNoteEditorSnapshot>()), Times.Once);

        Assert.That(persistedSnapshot, Is.Not.Null);
        Assert.That(persistedSnapshot!.DocumentContent, Is.EqualTo(insertionResult.UpdatedContent));
        Assert.That(persistedSnapshot.SelectedMedications.Count, Is.EqualTo(2));
        Assert.That(persistedSnapshot.SelectedMedications[0].Name, Is.EqualTo("Aspirin"));
        Assert.That(persistedSnapshot.SelectedMedications[0].IsKnown, Is.True);
        Assert.That(persistedSnapshot.SelectedMedications[1].Name, Is.EqualTo("UnknownMed"));
        Assert.That(persistedSnapshot.SelectedMedications[1].IsKnown, Is.False);
        Assert.That(persistedSnapshot.LastUpdatedUtc, Is.GreaterThanOrEqualTo(saveWindowStart));
        Assert.That(persistedSnapshot.LastUpdatedUtc, Is.LessThanOrEqualTo(saveWindowEnd));
    }

    [Test]
    public void Complete_SelectedMedicationProjection_TrimsDeduplicatesAndFlagsKnownCaseInsensitive()
    {
        // Arrange
        var request = new SleepNoteEditorCompletionRequest
        {
            EditorContent = "Original",
            SelectedMedications = new[] { " Aspirin ", "aspirin", "  ", "UnknownMed", "unknownmed" },
            Mode = EditorInsertionMode.ReplaceMedicationSection,
            CursorIndex = 0
        };

        var medicationNarrative = new MedicationNarrative
        {
            Text = "Medications: Aspirin, UnknownMed [UNKNOWN MEDICATION].",
            UnknownMedications = new[] { "UnknownMed" }
        };

        var insertionResult = new EditorInsertionResult
        {
            UpdatedContent = "After replace",
            AppliedMode = EditorInsertionMode.ReplaceMedicationSection
        };

        _repository.Setup(repo => repo.GetAllMedicationNames()).Returns(new[] { "ASPIRIN" });
        _narrativeBuilder
            .Setup(builder => builder.Build(request.SelectedMedications, It.IsAny<IReadOnlySet<string>>()))
            .Returns(medicationNarrative);
        _insertionService
            .Setup(service => service.Apply(
                request.EditorContent,
                medicationNarrative.Text,
                request.Mode,
                request.CursorIndex))
            .Returns(insertionResult);

        // Act
        var result = _orchestrator.Complete(request);

        // Assert
        Assert.That(result.SelectedMedications.Count, Is.EqualTo(2));
        Assert.That(result.SelectedMedications[0].Name, Is.EqualTo("Aspirin"));
        Assert.That(result.SelectedMedications[0].IsKnown, Is.True);
        Assert.That(result.SelectedMedications[1].Name, Is.EqualTo("UnknownMed"));
        Assert.That(result.SelectedMedications[1].IsKnown, Is.False);
        _sessionStore.Verify(store => store.Save(It.IsAny<SleepNoteEditorSnapshot>()), Times.Once);
    }
}
