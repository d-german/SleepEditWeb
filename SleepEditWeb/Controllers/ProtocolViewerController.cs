using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SleepEditWeb.Models;
using SleepEditWeb.Infrastructure.ProtocolPersistence;
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
    private readonly IProtocolRepository _repository;
    private readonly ProtocolEditorFeatureOptions _featureOptions;
    private readonly ILogger<ProtocolViewerController> _logger;

    public ProtocolViewerController(
        IProtocolStarterService starterService,
        IProtocolRepository repository,
        IOptions<ProtocolEditorFeatureOptions> featureOptions,
        ILogger<ProtocolViewerController> logger)
    {
        _starterService = starterService;
        _repository = repository;
        _featureOptions = featureOptions.Value;
        _logger = logger;
    }

    [HttpGet("")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Index([FromQuery] Guid? protocolId = null)
    {
        if (!IsEnabled())
        {
            _logger.LogWarning("ProtocolViewer index denied because feature is disabled.");
            return NotFound();
        }

        _logger.LogInformation("ProtocolViewer index requested. ProtocolId: {ProtocolId}", protocolId);
        Response.Headers.CacheControl = "no-store, no-cache, max-age=0";
        Response.Headers.Pragma = "no-cache";

        var initialDocument = LoadProtocolDocument(protocolId);
        var model = BuildViewModel(initialDocument, DateTime.UtcNow);
        _logger.LogInformation("ProtocolViewer model created with {SectionCount} sections.", model.InitialDocument.Sections.Count);
        return View(model);
    }

    private ProtocolDocument LoadProtocolDocument(Guid? protocolId)
    {
        if (protocolId.HasValue)
        {
            var version = _repository.GetProtocol(protocolId.Value);
            if (version != null)
            {
                _logger.LogInformation("ProtocolViewer loaded specific protocol {ProtocolId}.", protocolId.Value);
                return version.Document;
            }

            _logger.LogWarning("ProtocolViewer could not find protocol {ProtocolId}. Falling back to default.", protocolId.Value);
        }

        return _starterService.Create();
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
