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
        var selectedMeds = HttpContext.Session.GetString("SelectedMeds");
        var selectedMedsList = selectedMeds != null ? selectedMeds.Split(',').ToList() : [];
        selectedMedsList.Add(selectedMed);
        HttpContext.Session.SetString("SelectedMeds", string.Join(",", selectedMedsList));
        ViewBag.Message = $"You selected: {selectedMed}";
        ViewBag.SelectedMeds = selectedMedsList;
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
}