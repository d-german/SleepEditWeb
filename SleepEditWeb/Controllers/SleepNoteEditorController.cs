using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SleepEditWeb.Data;
using SleepEditWeb.Models;
using SleepEditWeb.Services;

namespace SleepEditWeb.Controllers;

[Route("SleepNoteEditor")]
public sealed class SleepNoteEditorController : Controller
{
    private readonly IMedicationRepository _repository;
    private readonly ISleepNoteEditorSessionStore _sessionStore;
    private readonly ISleepNoteEditorOrchestrator _orchestrator;
    private readonly IDrugInfoService _drugInfoService;
    private readonly SleepNoteEditorFeatureOptions _featureOptions;

    public SleepNoteEditorController(
        IMedicationRepository repository,
        ISleepNoteEditorSessionStore sessionStore,
        ISleepNoteEditorOrchestrator orchestrator,
        IDrugInfoService drugInfoService,
        IOptions<SleepNoteEditorFeatureOptions> featureOptions)
    {
        _repository = repository;
        _sessionStore = sessionStore;
        _orchestrator = orchestrator;
        _drugInfoService = drugInfoService;
        _featureOptions = featureOptions.Value;
    }

    [HttpGet("")]
    [HttpGet("~/")]
    public IActionResult Index()
    {
        if (!IsEnabled())
        {
            return NotFound();
        }

        var snapshot = _sessionStore.Load();
        var model = BuildViewModel(snapshot);
        return View(model);
    }

    [HttpGet("State")]
    public IActionResult State()
    {
        if (!IsEnabled())
        {
            return NotFound();
        }

        var snapshot = _sessionStore.Load();
        return Json(new
        {
            content = snapshot.DocumentContent,
            selectedMedications = snapshot.SelectedMedications,
            lastUpdatedUtc = snapshot.LastUpdatedUtc
        });
    }

    [HttpGet("SearchMedications")]
    public IActionResult SearchMedications(string term)
    {
        if (!IsEnabled())
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(term))
        {
            return Json(Array.Empty<string>());
        }

        var normalizedTerm = term.Trim();
        var startsWithMatches = _repository.SearchMedications(normalizedTerm).ToList();

        if (startsWithMatches.Count >= 20)
        {
            return Json(startsWithMatches.Take(20));
        }

        var containsMatches = _repository
            .GetAllMedicationNames()
            .Where(name => name.Contains(normalizedTerm, StringComparison.OrdinalIgnoreCase))
            .Except(startsWithMatches, StringComparer.OrdinalIgnoreCase)
            .Take(20 - startsWithMatches.Count);

        return Json(startsWithMatches.Concat(containsMatches).ToList());
    }

    [HttpGet("DrugInfo")]
    public async Task<IActionResult> DrugInfo(string name)
    {
        if (!IsEnabled())
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { error = "Drug name is required" });
        }

        var info = await _drugInfoService.GetDrugInfoAsync(name);
        return Json(info);
    }

    [HttpPost("Complete")]
    [ValidateAntiForgeryToken]
    public IActionResult Complete([FromBody] CompleteMedicationToolRequest? request)
    {
        if (!IsEnabled())
        {
            return NotFound();
        }

        if (request == null)
        {
            return BadRequest(new { error = "Request payload is required." });
        }

        var result = _orchestrator.Complete(new SleepNoteEditorCompletionRequest
        {
            EditorContent = request.EditorContent ?? string.Empty,
            SelectedMedications = request.SelectedMedications ?? [],
            Mode = request.Mode,
            CursorIndex = request.CursorIndex
        });

        return Json(new
        {
            success = true,
            content = result.UpdatedContent,
            narrative = result.Narrative,
            mode = result.AppliedMode,
            copyText = result.CopyText,
            selectedMedications = result.SelectedMedications,
            unknownMedications = result.UnknownMedications
        });
    }

    [HttpPost("Save")]
    [ValidateAntiForgeryToken]
    public IActionResult Save([FromBody] SaveSleepNoteEditorRequest? request)
    {
        if (!IsEnabled())
        {
            return NotFound();
        }

        if (request == null)
        {
            return BadRequest(new { error = "Request payload is required." });
        }

        _sessionStore.SaveDocument(request.Content ?? string.Empty);
        return Json(new { success = true });
    }

    private bool IsEnabled()
    {
        return _featureOptions.SleepNoteEditorEnabled;
    }

    private SleepNoteEditorViewModel BuildViewModel(SleepNoteEditorSnapshot snapshot)
    {
        return new SleepNoteEditorViewModel
        {
            InitialContent = snapshot.DocumentContent,
            SelectedMedications = snapshot.SelectedMedications,
            MedicationSuggestions = _repository.GetAllMedicationNames().Take(200).ToList()
        };
    }

    public sealed class CompleteMedicationToolRequest
    {
        public string? EditorContent { get; init; }

        public List<string>? SelectedMedications { get; init; }

        public EditorInsertionMode Mode { get; init; }

        public int CursorIndex { get; init; }
    }

    public sealed class SaveSleepNoteEditorRequest
    {
        public string? Content { get; init; }
    }
}
