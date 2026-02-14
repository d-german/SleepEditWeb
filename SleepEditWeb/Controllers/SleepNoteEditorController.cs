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
    private readonly ILogger<SleepNoteEditorController> _logger;

    public SleepNoteEditorController(
        IMedicationRepository repository,
        ISleepNoteEditorSessionStore sessionStore,
        ISleepNoteEditorOrchestrator orchestrator,
        IDrugInfoService drugInfoService,
        IOptions<SleepNoteEditorFeatureOptions> featureOptions,
        ILogger<SleepNoteEditorController> logger)
    {
        _repository = repository;
        _sessionStore = sessionStore;
        _orchestrator = orchestrator;
        _drugInfoService = drugInfoService;
        _featureOptions = featureOptions.Value;
        _logger = logger;
    }

    [HttpGet("")]
    [HttpGet("~/")]
    public IActionResult Index()
    {
        if (!IsEnabled())
        {
            _logger.LogWarning("SleepNoteEditor index denied because feature is disabled.");
            return NotFound();
        }

        _logger.LogInformation("SleepNoteEditor index requested.");
        var snapshot = _sessionStore.Load();
        var model = BuildViewModel(snapshot);
        return View(model);
    }

    [HttpGet("State")]
    public IActionResult State()
    {
        if (!IsEnabled())
        {
            _logger.LogWarning("SleepNoteEditor state denied because feature is disabled.");
            return NotFound();
        }

        _logger.LogDebug("SleepNoteEditor state requested.");
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
            _logger.LogWarning("SearchMedications denied because feature is disabled.");
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(term))
        {
            _logger.LogDebug("SearchMedications requested with empty term.");
            return Json(Array.Empty<string>());
        }

        _logger.LogDebug("SearchMedications requested. TermLength: {Length}", term.Length);
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
            _logger.LogWarning("SleepNoteEditor DrugInfo denied because feature is disabled.");
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            _logger.LogWarning("SleepNoteEditor DrugInfo rejected because name was empty.");
            return BadRequest(new { error = "Drug name is required" });
        }

        _logger.LogInformation("SleepNoteEditor DrugInfo requested for name: {DrugName}", name);
        var info = await _drugInfoService.GetDrugInfoAsync(name);
        _logger.LogInformation("SleepNoteEditor DrugInfo completed for name: {DrugName}. Found: {Found}", name, info?.Found);
        return Json(info);
    }

    [HttpPost("Complete")]
    [ValidateAntiForgeryToken]
    public IActionResult Complete([FromBody] CompleteMedicationToolRequest? request)
    {
        if (!IsEnabled())
        {
            _logger.LogWarning("SleepNoteEditor Complete denied because feature is disabled.");
            return NotFound();
        }

        if (request == null)
        {
            _logger.LogWarning("SleepNoteEditor Complete rejected because payload was null.");
            return BadRequest(new { error = "Request payload is required." });
        }

        _logger.LogInformation(
            "SleepNoteEditor Complete requested. SelectedCount: {Count}, Mode: {Mode}, CursorIndex: {CursorIndex}",
            request.SelectedMedications?.Count ?? 0,
            request.Mode,
            request.CursorIndex);
        var result = _orchestrator.Complete(new SleepNoteEditorCompletionRequest
        {
            EditorContent = request.EditorContent ?? string.Empty,
            SelectedMedications = request.SelectedMedications ?? [],
            Mode = request.Mode,
            CursorIndex = request.CursorIndex
        });
        _logger.LogInformation(
            "SleepNoteEditor Complete completed. AppliedMode: {Mode}, UnknownCount: {UnknownCount}",
            result.AppliedMode,
            result.UnknownMedications.Count);

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
            _logger.LogWarning("SleepNoteEditor Save denied because feature is disabled.");
            return NotFound();
        }

        if (request == null)
        {
            _logger.LogWarning("SleepNoteEditor Save rejected because payload was null.");
            return BadRequest(new { error = "Request payload is required." });
        }

        _logger.LogInformation("SleepNoteEditor Save requested. ContentLength: {Length}", request.Content?.Length ?? 0);
        _sessionStore.SaveDocument(request.Content ?? string.Empty);
        _logger.LogInformation("SleepNoteEditor Save completed successfully.");
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
