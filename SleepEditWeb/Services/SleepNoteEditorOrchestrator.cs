using SleepEditWeb.Data;
using Microsoft.Extensions.Logging;
using SleepEditWeb.Models;

namespace SleepEditWeb.Services;

public interface ISleepNoteEditorOrchestrator
{
    SleepNoteEditorCompletionResult Complete(SleepNoteEditorCompletionRequest request);
}

public sealed class SleepNoteEditorCompletionRequest
{
    public required string EditorContent { get; init; }

    public required IReadOnlyList<string> SelectedMedications { get; init; }

    public required EditorInsertionMode Mode { get; init; }

    public int CursorIndex { get; init; }
}

public sealed class SleepNoteEditorCompletionResult
{
    public required string UpdatedContent { get; init; }

    public required string Narrative { get; init; }

    public required EditorInsertionMode AppliedMode { get; init; }

    public IReadOnlyList<string> UnknownMedications { get; init; } = [];

    public string? CopyText { get; init; }

    public IReadOnlyList<MedicationSelection> SelectedMedications { get; init; } = [];
}

public sealed class SleepNoteEditorOrchestrator : ISleepNoteEditorOrchestrator
{
    private readonly IMedicationRepository _repository;
    private readonly IMedicationNarrativeBuilder _narrativeBuilder;
    private readonly IEditorInsertionService _insertionService;
    private readonly ISleepNoteEditorSessionStore _sessionStore;
    private readonly ILogger<SleepNoteEditorOrchestrator> _logger;

    public SleepNoteEditorOrchestrator(
        IMedicationRepository repository,
        IMedicationNarrativeBuilder narrativeBuilder,
        IEditorInsertionService insertionService,
        ISleepNoteEditorSessionStore sessionStore,
        ILogger<SleepNoteEditorOrchestrator> logger)
    {
        _repository = repository;
        _narrativeBuilder = narrativeBuilder;
        _insertionService = insertionService;
        _sessionStore = sessionStore;
        _logger = logger;
    }

    public SleepNoteEditorCompletionResult Complete(SleepNoteEditorCompletionRequest request)
    {
        _logger.LogInformation(
            "SleepNoteEditorOrchestrator.Complete requested. SelectedCount: {Count}, Mode: {Mode}, CursorIndex: {CursorIndex}",
            request.SelectedMedications.Count,
            request.Mode,
            request.CursorIndex);
        var knownMedicationNames = _repository
            .GetAllMedicationNames()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var narrative = _narrativeBuilder.Build(request.SelectedMedications, knownMedicationNames);
        var insertion = _insertionService.Apply(request.EditorContent, narrative.Text, request.Mode, request.CursorIndex);
        var selected = BuildSelections(request.SelectedMedications, knownMedicationNames);

        PersistSnapshot(insertion.UpdatedContent, selected);
        _logger.LogInformation(
            "SleepNoteEditorOrchestrator.Complete completed. AppliedMode: {Mode}, UnknownCount: {UnknownCount}",
            insertion.AppliedMode,
            narrative.UnknownMedications.Count);

        return new SleepNoteEditorCompletionResult
        {
            UpdatedContent = insertion.UpdatedContent,
            Narrative = narrative.Text,
            AppliedMode = insertion.AppliedMode,
            UnknownMedications = narrative.UnknownMedications,
            CopyText = insertion.CopyText,
            SelectedMedications = selected
        };
    }

    private void PersistSnapshot(string content, IReadOnlyList<MedicationSelection> selections)
    {
        _sessionStore.Save(new SleepNoteEditorSnapshot
        {
            DocumentContent = content,
            SelectedMedications = selections,
            LastUpdatedUtc = DateTimeOffset.UtcNow
        });
    }

    private static IReadOnlyList<MedicationSelection> BuildSelections(
        IReadOnlyList<string> selectedMedicationNames,
        IReadOnlySet<string> knownMedicationNames)
    {
        return selectedMedicationNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(name => new MedicationSelection
            {
                Name = name,
                IsKnown = knownMedicationNames.Contains(name)
            })
            .ToList();
    }
}
