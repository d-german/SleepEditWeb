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

    // POST
    [HttpPost]
    public IActionResult Index(string selectedMed)
    {
        // Trim and validate input
        if (string.IsNullOrWhiteSpace(selectedMed))
        {
            ViewBag.Message = "Invalid input. Please try again.";
            return View(MedList);
        }

        // Determine action based on special character
        selectedMed = selectedMed.Trim(); // Remove extra spaces around user input
        var isAddition = selectedMed.StartsWith($"+");
        var isRemoval = selectedMed.StartsWith($"-");

        // Get clean medication name without special character
        var cleanMed = isAddition || isRemoval ? selectedMed[1..].Trim() : selectedMed;

        // Handle addition to the MedList
        if (isAddition)
        {
            if (!MedList.Contains(cleanMed, StringComparer.OrdinalIgnoreCase))
            {
                MedList.Add(cleanMed); // Add to list
                SaveMedListToFile(); // Persist changes on disk
                ViewBag.Message = $"Medication '{cleanMed}' has been successfully added to the list.";
            }
            else
            {
                ViewBag.Message = $"Medication '{cleanMed}' already exists in the list.";
            }
        }
        // Handle removal from the MedList
        else if (isRemoval)
        {
            if (MedList.Remove(cleanMed)) // Remove from list
            {
                SaveMedListToFile(); // Persist changes on disk
                ViewBag.Message = $"Medication '{cleanMed}' has been successfully removed from the list.";
            }
            else
            {
                ViewBag.Message = $"Medication '{cleanMed}' does not exist in the list.";
            }
        }
        // No special character: Add medication to user's session list (default behavior)
        else
        {
            if (selectedMed.Equals("cls", StringComparison.CurrentCultureIgnoreCase))
            {
                HttpContext.Session.Remove("SelectedMeds");
                ViewBag.Message = "Selected medications cleared.";
            }
            else
            {
                var selectedMeds = HttpContext.Session.GetString("SelectedMeds");
                var selectedMedsList = selectedMeds != null ? selectedMeds.Split(',').ToList() : [];
                selectedMedsList.Add(cleanMed);
                HttpContext.Session.SetString("SelectedMeds", string.Join(",", selectedMedsList));
                ViewBag.Message = $"You selected: {cleanMed}";
                ViewBag.SelectedMeds = selectedMedsList;
            }
        }

        // Return updated MedList to the view
        return View(MedList);
    }

    private static List<string> GetMedList()
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "medlist.txt");

        // Read all lines from the included file
        if (!System.IO.File.Exists(filePath)) return ["No medications found!"];
        var storedList = System.IO.File.ReadAllLines(filePath);
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