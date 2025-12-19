using System.Reflection;
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
        // Check embedded resource
        var assembly = Assembly.GetExecutingAssembly();
        var embeddedResourceNames = assembly.GetManifestResourceNames();
        // Get all files in /app directory
        var appDirContents = new List<string>();
        try
        {
            if (Directory.Exists("/app"))
            {
                appDirContents = Directory.GetFileSystemEntries("/app", "*", SearchOption.AllDirectories)
                    .Take(50)
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            appDirContents.Add("Error: " + ex.Message);
        }
        var info = new
        {
            BasePath = basePath,
            CurrentDirectory = Directory.GetCurrentDirectory(),
            FilePath = filePath,
            FileExists = System.IO.File.Exists(filePath),
            ResourcesDirExists = Directory.Exists(resourcesDir),
            EmbeddedResourceNames = embeddedResourceNames.ToList(),
            MedListCount = MedList.Count,
            FirstFiveMeds = MedList.Take(5).ToList(),
            LoadedFrom = _loadedFrom,
            DirectoryContents = Directory.Exists(resourcesDir) 
                ? Directory.GetFiles(resourcesDir).Select(Path.GetFileName).ToList() 
                : new List<string?>(),
            AppDirContents = appDirContents
        };
        return Json(info);
    }
    private static string _loadedFrom = "not loaded";
    private static List<string> GetMedList()
    {
        // First, try to load from embedded resource (most reliable in containers)
        var assembly = Assembly.GetExecutingAssembly();
        var resourceStream = assembly.GetManifestResourceStream("medlist.txt");
        if (resourceStream != null)
        {
            Console.WriteLine("[MedList] Loading from embedded resource");
            _loadedFrom = "embedded resource";
            using var reader = new StreamReader(resourceStream);
            var content = reader.ReadToEnd();
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Console.WriteLine($"[MedList] Loaded {lines.Length} medications from embedded resource");
            return lines.ToList();
        }
        Console.WriteLine("[MedList] Embedded resource not found, trying file system");
        // Fallback to file system
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "medlist.txt");
        Console.WriteLine($"[MedList] Looking for medlist.txt at: {filePath}");
        Console.WriteLine($"[MedList] File exists: {System.IO.File.Exists(filePath)}");
        if (!System.IO.File.Exists(filePath))
        {
            Console.WriteLine("[MedList] WARNING: medlist.txt not found!");
            _loadedFrom = "not found";
            return ["No medications found!"];
        }
        _loadedFrom = "file system";
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
            // Ensure directory exists
            var dir = Path.GetDirectoryName(filePath);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            System.IO.File.WriteAllLines(filePath, MedList);
        }
        catch (Exception ex)
        {
            // Log the exception or show an error
            Console.WriteLine($"Error saving MedList to file: {ex.Message}");
        }
    }
}
