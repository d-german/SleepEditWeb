using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace SleepEditWeb.Controllers;

public class MedListController : Controller
{
    private readonly HttpClient _httpClient;
    private static List<string> MedList;

    private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public MedListController(HttpClient httpClient)
    {
        _httpClient = httpClient;
        MedList = GetMedList().Result;
    }

    // GET
    public async Task<IActionResult> Index()
    {
        var selectedMeds = HttpContext.Session.GetString("SelectedMeds");
        var selectedMedsList = selectedMeds != null ? selectedMeds.Split(',').ToList() : new List<string>();
        ViewBag.SelectedMeds = selectedMedsList;
        return View(MedList);
    }

    // POST
    [HttpPost]
    public IActionResult Index(string selectedMed)
    {
        var selectedMeds = HttpContext.Session.GetString("SelectedMeds");
        var selectedMedsList = selectedMeds != null ? selectedMeds.Split(',').ToList() : new List<string>();
        selectedMedsList.Add(selectedMed);
        HttpContext.Session.SetString("SelectedMeds", string.Join(",", selectedMedsList));
        ViewBag.Message = $"You selected: {selectedMed}";
        ViewBag.SelectedMeds = selectedMedsList;
        return View(MedList);
    }

    private static async Task<List<string>> GetMedList()
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "medlist.txt");

        // Read all lines from the included file
        if (System.IO.File.Exists(filePath))
        {
            var storedList = await System.IO.File.ReadAllLinesAsync(filePath);
            return storedList.ToList();
        }

        // Fallback in case the file is not found
        return ["No medications found!"];
    }
}

public class DisplayTermsList
{
    [JsonPropertyName("term")] public List<string> Term { get; set; }
}

public class RootObject
{
    [JsonPropertyName("displayTermsList")] public DisplayTermsList DisplayTermsList { get; set; }
}