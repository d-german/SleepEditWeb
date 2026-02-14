using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SleepEditWeb.Models;

namespace SleepEditWeb.Controllers;

public class HomeController : Controller
{
	private readonly ILogger<HomeController> _logger;

	public HomeController(ILogger<HomeController> logger)
	{
		_logger = logger;
	}

	public IActionResult Index()
	{
		_logger.LogDebug("Home index requested.");
		return View();
	}

	public IActionResult Privacy()
	{
		_logger.LogDebug("Home privacy page requested.");
		return View();
	}

	[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
	public IActionResult Error()
	{
		_logger.LogWarning("Home error page requested. TraceIdentifier: {TraceIdentifier}", HttpContext.TraceIdentifier);
		return View(new ErrorViewModel
		{
			RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
		});
	}
}
