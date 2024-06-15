using Microsoft.AspNetCore.Mvc;

namespace SleepEditWeb.Controllers;

public class MedListController : Controller
{
	// GET
	public IActionResult Index()
	{
		return View();
	}
}