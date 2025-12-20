using Microsoft.AspNetCore.Mvc;
using SleepEditWeb.Data;
using SleepEditWeb.Services;

namespace SleepEditWeb.Controllers;

public class MedListController : Controller
{
    private readonly IMedicationRepository _repository;
    private readonly IDrugInfoService _drugInfoService;

    public MedListController(IMedicationRepository repository, IDrugInfoService drugInfoService)
    {
        _repository = repository;
        _drugInfoService = drugInfoService;
    }

    // GET
    public IActionResult Index()
    {
        var selectedMeds = HttpContext.Session.GetString("SelectedMeds");
        var selectedMedsList = selectedMeds != null ? selectedMeds.Split(',').ToList() : [];
        ViewBag.SelectedMeds = selectedMedsList;

        var medList = _repository.GetAllMedicationNames().ToList();
        return View(medList);
    }

    // POST - AJAX endpoint
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddMedication([FromBody] MedRequest request)
    {
        var selectedMed = request?.SelectedMed;

        // Trim and validate input
        if (string.IsNullOrWhiteSpace(selectedMed))
        {
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
            return BadRequest(new { error = "Drug name is required" });

        var info = await _drugInfoService.GetDrugInfoAsync(name);
        return Json(info);
    }

    // GET - Diagnostic endpoint to check database status
    [HttpGet]
    public IActionResult DiagnosticInfo()
    {
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