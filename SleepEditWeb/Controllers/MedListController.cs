using Microsoft.AspNetCore.Mvc;
using SleepEditWeb.Data;
using SleepEditWeb.Services;

using System.Collections.Immutable;
namespace SleepEditWeb.Controllers;

public class MedListController : Controller
{
    private readonly IMedicationRepository _repository;
    private readonly IDrugInfoService _drugInfoService;
    private readonly ILogger<MedListController> _logger;

    public MedListController(
        IMedicationRepository repository,
        IDrugInfoService drugInfoService,
        ILogger<MedListController> logger)
    {
        _repository = repository;
        _drugInfoService = drugInfoService;
        _logger = logger;
    }

    // GET
    public IActionResult Index()
    {
        _logger.LogInformation("MedList index requested.");
        var selectedMeds = HttpContext.Session.GetString("SelectedMeds");
        var selectedMedsList = selectedMeds != null
            ? selectedMeds.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToImmutableArray()
            : ImmutableArray<string>.Empty;

        ViewBag.SelectedMeds = selectedMedsList;

        var medList = _repository.GetAllMedicationNames().ToImmutableArray();
        return View(medList);
    }

    // POST - AJAX endpoint
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddMedication([FromBody] MedRequest request)
    {
        var selectedMed = request?.SelectedMed;
        _logger.LogInformation("AddMedication requested. RawInputPresent: {HasInput}", !string.IsNullOrWhiteSpace(selectedMed));

        // Trim and validate input
        if (string.IsNullOrWhiteSpace(selectedMed))
        {
            _logger.LogWarning("AddMedication rejected because input was empty.");
            return Json(new { success = false, message = "Invalid input. Please try again.", selectedMeds = GetSelectedMedsFromSession() });
        }

        selectedMed = selectedMed.Trim();
        var isAddition = selectedMed.StartsWith("+");
        var isRemoval = selectedMed.StartsWith("-");
        var cleanMed = isAddition || isRemoval ? selectedMed[1..].Trim() : selectedMed;

        string message = (isAddition, isRemoval) switch
        {
            (true, _) => PerformMasterListAddition(cleanMed),
            (_, true) => PerformMasterListRemoval(cleanMed),
            _ => UpdateUserSessionList(selectedMed, cleanMed)
        };

        _logger.LogInformation(
            "AddMedication completed. Addition: {IsAddition}, Removal: {IsRemoval}, CleanNameLength: {Length}",
            isAddition,
            isRemoval,
            cleanMed.Length);

        return Json(new
        {
            success = true,
            message,
            selectedMeds = GetSelectedMedsFromSession(),
            medList = _repository.GetAllMedicationNames()
        });
    }

    private string PerformMasterListAddition(string medicationName)
    {
        return _repository.AddUserMedication(medicationName)
            ? $"Medication '{medicationName}' has been added to the master list."
            : $"Medication '{medicationName}' already exists in the list.";
    }

    private string PerformMasterListRemoval(string medicationName)
    {
        if (_repository.RemoveUserMedication(medicationName))
        {
            return $"Medication '{medicationName}' has been removed from the master list.";
        }

        return _repository.MedicationExists(medicationName)
            ? $"Medication '{medicationName}' is a system medication and cannot be removed."
            : $"Medication '{medicationName}' does not exist in the list.";
    }

    private string UpdateUserSessionList(string rawInput, string medicationName)
    {
        if (rawInput.Equals("cls", StringComparison.CurrentCultureIgnoreCase))
        {
            HttpContext.Session.Remove("SelectedMeds");
            return "Selected medications cleared.";
        }

        var selectedMeds = HttpContext.Session.GetString("SelectedMeds");
        var selectedMedsList = selectedMeds != null ? selectedMeds.Split(',').ToList() : [];
        selectedMedsList.Add(medicationName);
        HttpContext.Session.SetString("SelectedMeds", string.Join(",", selectedMedsList));
        
        return $"Added: {medicationName}";
    }

    private List<string> GetSelectedMedsFromSession()
    {
        var selectedMeds = HttpContext.Session.GetString("SelectedMeds");
        return selectedMeds != null ? selectedMeds.Split(',').ToList() : [];
    }

    public class MedRequest
    {
        public string? SelectedMed { get; set; }
    }

    // GET - Drug info lookup from OpenFDA
    [HttpGet]
    public async Task<IActionResult> DrugInfo(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            _logger.LogWarning("DrugInfo rejected because drug name was empty.");
            return BadRequest(new { error = "Drug name is required" });
        }

        _logger.LogInformation("DrugInfo requested for name: {DrugName}", name);
        var info = await _drugInfoService.GetDrugInfoAsync(name);
        _logger.LogInformation("DrugInfo completed for name: {DrugName}. Found: {Found}", name, info?.Found);
        return Json(info);
    }

    // GET - Diagnostic endpoint to check database status
    [HttpGet]
    public IActionResult DiagnosticInfo()
    {
        _logger.LogInformation("DiagnosticInfo requested.");
        var stats = _repository.GetStats();

        var info = new
        {
            DatabasePath = _repository.DatabasePath,
            TotalMedicationCount = stats.TotalCount,
            SystemMedicationCount = stats.SystemMedCount,
            UserMedicationCount = stats.UserMedCount,
            SeedVersion = stats.SeedVersion,
            LastSeeded = stats.LastSeeded,
            LoadedFrom = stats.LoadedFrom,
            FirstFiveMeds = _repository.GetAllMedicationNames().Take(5).ToList()
        };

        return Json(info);
    }
}
