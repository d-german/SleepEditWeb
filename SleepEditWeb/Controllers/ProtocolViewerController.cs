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
    private readonly ILogger<ProtocolViewerController> _logger;

    public ProtocolViewerController(
        IProtocolStarterService starterService,
        IOptions<ProtocolEditorFeatureOptions> featureOptions,
        ILogger<ProtocolViewerController> logger)
    {
        _starterService = starterService;
        _featureOptions = featureOptions.Value;
        _logger = logger;
    }

    [HttpGet("")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Index()
    {
        if (!IsEnabled())
        {
            _logger.LogWarning("ProtocolViewer index denied because feature is disabled.");
            return NotFound();
        }

        _logger.LogInformation("ProtocolViewer index requested.");
        Response.Headers.CacheControl = "no-store, no-cache, max-age=0";
        Response.Headers.Pragma = "no-cache";

        var initialDocument = _starterService.Create();
        var model = BuildViewModel(initialDocument, DateTime.UtcNow);
        _logger.LogInformation("ProtocolViewer model created with {SectionCount} sections.", model.InitialDocument.Sections.Count);
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
