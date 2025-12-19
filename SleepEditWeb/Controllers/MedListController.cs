using Microsoft.AspNetCore.Mvc;

namespace SleepEditWeb.Controllers;

public class MedListController : Controller
{
    private static readonly List<string> MedList = GetMedList();

    // GET
    public Task<IActionResult> Index()
    {
        var selectedMeds = HttpContext.Session.GetString("SelectedMeds");
        var selectedMedsList = selectedMeds != null ? selectedMeds.Split(',').ToList() : [];
        ViewBag.SelectedMeds = selectedMedsList;
        return Task.FromResult<IActionResult>(View(MedList));
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

        // Handle addition to the MedList
        if (isAddition)
        {
            if (!MedList.Contains(cleanMed, StringComparer.OrdinalIgnoreCase))
            {
                MedList.Add(cleanMed);
                SaveMedListToFile();
                message = $"Medication '{cleanMed}' has been added to the master list.";
            }
            else
            {
                message = $"Medication '{cleanMed}' already exists in the list.";
            }
        }
        // Handle removal from the MedList
        else if (isRemoval)
        {
            if (MedList.Remove(cleanMed))
            {
                SaveMedListToFile();
                message = $"Medication '{cleanMed}' has been removed from the master list.";
            }
            else
            {
                message = $"Medication '{cleanMed}' does not exist in the list.";
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

        return Json(new { 
            success = true, 
            message, 
            selectedMeds = GetSelectedMedsFromSession(),
            medList = MedList 
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

    // GET - Diagnostic endpoint to check file status
    [HttpGet]
    public IActionResult DiagnosticInfo()
    {
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var filePath = Path.Combine(basePath, "Resources", "medlist.txt");
        var resourcesDir = Path.Combine(basePath, "Resources");
        
        var info = new
        {
            BasePath = basePath,
            FilePath = filePath,
            FileExists = System.IO.File.Exists(filePath),
            ResourcesDirExists = Directory.Exists(resourcesDir),
            MedListCount = MedList.Count,
            FirstFiveMeds = MedList.Take(5).ToList(),
            DirectoryContents = Directory.Exists(resourcesDir) 
                ? Directory.GetFiles(resourcesDir).Select(Path.GetFileName).ToList() 
                : new List<string?>()
        };
        
        return Json(info);
    }
    
    private static List<string> GetMedList()
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "medlist.txt");
        
        Console.WriteLine($"[MedList] Looking for medlist.txt at: {filePath}");
        Console.WriteLine($"[MedList] File exists: {System.IO.File.Exists(filePath)}");

        // Read all lines from the included file
        if (!System.IO.File.Exists(filePath))
        {
            Console.WriteLine("[MedList] WARNING: medlist.txt not found!");
            return ["No medications found!"];
        }
        
        var storedList = System.IO.File.ReadAllLines(filePath);
        Console.WriteLine($"[MedList] Loaded {storedList.Length} medications from file");
        return storedList.ToList();
    }

    /// <summary>
    /// Save the current medication list to the 'medlist.txt' file.
    /// This overwrites the file with the updated data.
    /// </summary>
    private static void SaveMedListToFile()
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "medlist.txt");
        try
        {
            System.IO.File.WriteAllLines(filePath, MedList);
        }
        catch (Exception ex)
        {
            // Log the exception or show an error
            Console.WriteLine($"Error saving MedList to file: {ex.Message}");
        }
    }
}