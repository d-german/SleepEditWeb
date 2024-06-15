using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace SleepEditWeb.Controllers;

public class MedListController : Controller
{
	private readonly HttpClient _httpClient;
	private static List<string> MedList;

	public MedListController(HttpClient httpClient)
	{
		_httpClient = httpClient;
		MedList = GetMedList().Result;
	}

	// GET
	public async Task<IActionResult> Index()
	{
		return View(MedList);
	}

	// POST
	[HttpPost]
	public IActionResult Index(string selectedMed)
	{
		// Handle the form submission here
		// For example, you can save the selected medication or perform an action with it
		ViewBag.Message = $"You selected: {selectedMed}";
		return View(MedList);
	}

	private  async Task<List<string>> GetMedList()
	{
		var apiUrl = "https://rxnav.nlm.nih.gov/REST/displaynames.json";
		var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
		var response = await _httpClient.SendAsync(request);

		var jsonString = await response.Content.ReadAsStringAsync();

		var options = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = true
		};

		var model = JsonSerializer.Deserialize<RootObject>(jsonString, options);

		return model.DisplayTermsList.Term;
	}
}

public class DisplayTermsList
{
	[JsonPropertyName("term")]
	public List<string> Term { get; set; }
}

public class RootObject
{
	[JsonPropertyName("displayTermsList")]
	public DisplayTermsList DisplayTermsList { get; set; }
}