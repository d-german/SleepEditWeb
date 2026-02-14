using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SleepEditWeb.Models;
using SleepEditWeb.Services;

namespace SleepEditWeb.Controllers;

[Route("ProtocolViewer")]
public sealed class ProtocolViewerController : Controller
{
    private static readonly string[] DefaultTechNames =
    [
        "Damon German, BS, RPSGT",
        "Rosanna German. RPSGT"
    ];

    private static readonly string[] DefaultMaskStyles =
    [
        "Respironics Comfort Select",
        "F&P Flexifit HC407"
    ];

    private static readonly string[] DefaultMaskSizes =
    [
        "small",
        "medium",
        "large"
    ];

    private readonly IProtocolStarterService _starterService;
    private readonly ProtocolEditorFeatureOptions _featureOptions;

    public ProtocolViewerController(
        IProtocolStarterService starterService,
        IOptions<ProtocolEditorFeatureOptions> featureOptions)
    {
        _starterService = starterService;
        _featureOptions = featureOptions.Value;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        if (!IsEnabled())
        {
            return NotFound();
        }

        var initialDocument = _starterService.Create();
        var model = BuildViewModel(initialDocument, DateTime.UtcNow);
        return View(model);
    }

    private bool IsEnabled()
    {
        return _featureOptions.ProtocolEditorEnabled;
    }

    private static ProtocolViewerViewModel BuildViewModel(ProtocolDocument initialDocument, DateTime utcNow)
    {
        return new ProtocolViewerViewModel
        {
            InitialDocument = initialDocument,
            InitialTechNames = DefaultTechNames,
            InitialMaskStyles = DefaultMaskStyles,
            InitialMaskSizes = DefaultMaskSizes,
            InitialStudyDate = utcNow.ToString("M/d/yyyy", CultureInfo.InvariantCulture)
        };
    }
}
