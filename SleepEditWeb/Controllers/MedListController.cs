using Microsoft.AspNetCore.Mvc;
using SleepEditWeb.Data;

namespace SleepEditWeb.Controllers;

public class MedListController : Controller
{
    private readonly IMedicationRepository _repository;

    public MedListController(IMedicationRepository repository)
    {
        _repository = repository;
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

        // Determine action based on special character
        selectedMed = selectedMed.Trim();
        var isAddition = selectedMed.StartsWith("+");
        var isRemoval = selectedMed.StartsWith("-");

        // Get clean medication name without special character
        var cleanMed = isAddition || isRemoval ? selectedMed[1..].Trim() : selectedMed;

        string message;

        // Handle addition to the MedList (master list)
        if (isAddition)
        {
            if (_repository.AddUserMedication(cleanMed))
            {
                message = $"Medication '{cleanMed}' has been added to the master list.";
            }
            else
            {
                message = $"Medication '{cleanMed}' already exists in the list.";
            }
        }
        // Handle removal from the MedList (master list - user meds only)
        else if (isRemoval)
        {
            if (_repository.RemoveUserMedication(cleanMed))
            {
                message = $"Medication '{cleanMed}' has been removed from the master list.";
            }
            else
            {
                // Could be system med or not found
                var exists = _repository.MedicationExists(cleanMed);
                message = exists 
                    ? $"Medication '{cleanMed}' is a system medication and cannot be removed."
                    : $"Medication '{cleanMed}' does not exist in the list.";
            }
        }
        // No special character: Add medication to user's session list
        else
        {
            if (selectedMed.Equals("cls", StringComparison.CurrentCultureIgnoreCase))
            {
                HttpContext.Session.Remove("SelectedMeds");
                message = "Selected medications cleared.";
            }
            else
            {
                var selectedMeds = HttpContext.Session.GetString("SelectedMeds");
                var selectedMedsList = selectedMeds != null ? selectedMeds.Split(',').ToList() : [];
                selectedMedsList.Add(cleanMed);
                HttpContext.Session.SetString("SelectedMeds", string.Join(",", selectedMedsList));
                message = $"Added: {cleanMed}";
            }
        }

        return Json(new
        {
            success = true,
            message,
            selectedMeds = GetSelectedMedsFromSession(),
            medList = _repository.GetAllMedicationNames()
        });
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

    // GET - Diagnostic endpoint to check database status
    [HttpGet]
    public IActionResult DiagnosticInfo()
    {
        var stats = _repository.GetStats();
        
        // Get database path if repository is LiteDbMedicationRepository
        var databasePath = (_repository as LiteDbMedicationRepository)?.DatabasePath ?? "unknown";
        
        var info = new
        {
            DatabasePath = databasePath,
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
