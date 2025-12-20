using Microsoft.AspNetCore.Mvc;
using SleepEditWeb.Data;
using SleepEditWeb.Services;
using CSharpFunctionalExtensions;

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
        List<string> selectedMedsList = selectedMeds != null ? [..selectedMeds.Split(',')] : [];
        ViewBag.SelectedMeds = selectedMedsList;

        List<string> medList = [.._repository.GetAllMedicationNames()];
        return View(medList);
    }

    // POST - AJAX endpoint
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddMedication([FromBody] MedRequest request)
    {
        var result = ValidateAndProcessMedication(request);
        var (_, isFailure, message, error) = result;

        return Json(new
        {
            success = !isFailure,
            message = isFailure ? error : message,
            selectedMeds = GetSelectedMedsFromSession(),
            medList = (List<string>)[.._repository.GetAllMedicationNames()]
        });
    }

    private Result<string> ValidateAndProcessMedication(MedRequest request)
    {
        var selectedMed = request?.SelectedMed;

        if (string.IsNullOrWhiteSpace(selectedMed))
        {
            return Result.Failure<string>("Invalid input. Please try again.");
        }

        selectedMed = selectedMed.Trim();
        var isAddition = selectedMed.StartsWith("+");
        var isRemoval = selectedMed.StartsWith("-");
        var cleanMed = isAddition || isRemoval ? selectedMed[1..].Trim() : selectedMed;

        return (isAddition, isRemoval) switch
        {
            (true, _) => PerformMasterListAddition(cleanMed),
            (_, true) => PerformMasterListRemoval(cleanMed),
            _ => Result.Success(UpdateUserSessionList(selectedMed, cleanMed))
        };
    }

    private Result<string> PerformMasterListAddition(string medicationName)
    {
        var result = _repository.AddUserMedication(medicationName);
        var (_, isFailure, error) = result;

        return isFailure 
            ? Result.Failure<string>(error) 
            : Result.Success($"Medication '{medicationName}' has been added to the master list.");
    }

    private Result<string> PerformMasterListRemoval(string medicationName)
    {
        var result = _repository.RemoveUserMedication(medicationName);
        var (_, isFailure, error) = result;

        return isFailure 
            ? Result.Failure<string>(error) 
            : Result.Success($"Medication '{medicationName}' has been removed from the master list.");
    }

    private string UpdateUserSessionList(string rawInput, string medicationName)
    {
        if (rawInput.Equals("cls", StringComparison.CurrentCultureIgnoreCase))
        {
            HttpContext.Session.Remove("SelectedMeds");
            return "Selected medications cleared.";
        }

        var selectedMeds = HttpContext.Session.GetString("SelectedMeds");
        List<string> selectedMedsList = selectedMeds != null ? [..selectedMeds.Split(',')] : [];
        selectedMedsList.Add(medicationName);
        HttpContext.Session.SetString("SelectedMeds", string.Join(",", selectedMedsList));
        
        return $"Added: {medicationName}";
    }

    private List<string> GetSelectedMedsFromSession()
    {
        var selectedMeds = HttpContext.Session.GetString("SelectedMeds");
        return selectedMeds != null ? [..selectedMeds.Split(',')] : [];
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

        var result = await _drugInfoService.GetDrugInfoAsync(name);
        var (_, isFailure, info, error) = result;

        if (isFailure)
        {
            return Json(new { found = false, errorMessage = error });
        }

        return Json(new 
        { 
            found = true,
            name = info.Name,
            genericName = info.GenericName,
            purpose = info.Purpose,
            uses = info.Uses,
            warnings = info.Warnings,
            dosage = info.Dosage,
            manufacturer = info.Manufacturer,
            source = info.Source
        });
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
            FirstFiveMeds = (List<string>)[.._repository.GetAllMedicationNames().Take(5)]
        };

        return Json(info);
    }
}