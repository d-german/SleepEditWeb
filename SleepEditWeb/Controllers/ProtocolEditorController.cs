using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SleepEditWeb.Models;
using SleepEditWeb.Services;

namespace SleepEditWeb.Controllers;

[Route("ProtocolEditor")]
public sealed class ProtocolEditorController : Controller
{
    private readonly IProtocolEditorService _service;
    private readonly ProtocolEditorFeatureOptions _featureOptions;
    private readonly ProtocolEditorStartupOptions _startupOptions;
    private readonly ILogger<ProtocolEditorController> _logger;

    public ProtocolEditorController(
        IProtocolEditorService service,
        IOptions<ProtocolEditorFeatureOptions> featureOptions,
        IOptions<ProtocolEditorStartupOptions> startupOptions,
        ILogger<ProtocolEditorController> logger)
    {
        _service = service;
        _featureOptions = featureOptions.Value;
        _startupOptions = startupOptions.Value;
        _logger = logger;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        if (!IsEnabled())
        {
            return NotFound();
        }

        var snapshot = _service.Load();
        return View(new ProtocolEditorViewModel
        {
            InitialDocument = snapshot.Document
        });
    }

    [HttpGet("State")]
    public IActionResult State()
    {
        if (!IsEnabled())
        {
            return NotFound();
        }

        return Json(ToStateResponse(_service.Load()));
    }

    [HttpPost("AddSection")]
    [ValidateAntiForgeryToken]
    public IActionResult AddSection([FromBody] AddSectionRequest? request)
    {
        if (!IsEnabled())
        {
            return NotFound();
        }

        var snapshot = _service.AddSection(request?.Text ?? "New Section");
        return Json(ToStateResponse(snapshot));
    }

    [HttpPost("AddChild")]
    [ValidateAntiForgeryToken]
    public IActionResult AddChild([FromBody] AddChildRequest? request)
    {
        if (!IsEnabled())
        {
            return NotFound();
        }

        if (request == null)
        {
            return BadRequest(new { error = "Request payload is required." });
        }

        var snapshot = _service.AddChild(request.ParentId, request.Text ?? "New Node");
        return Json(ToStateResponse(snapshot));
    }

    [HttpPost("RemoveNode")]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveNode([FromBody] RemoveNodeRequest? request)
    {
        if (!IsEnabled())
        {
            return NotFound();
        }

        if (request == null)
        {
            return BadRequest(new { error = "Request payload is required." });
        }

        var snapshot = _service.RemoveNode(request.NodeId);
        return Json(ToStateResponse(snapshot));
    }

    [HttpPost("UpdateNode")]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateNode([FromBody] UpdateNodeRequest? request)
    {
        if (!IsEnabled())
        {
            return NotFound();
        }

        if (request == null)
        {
            return BadRequest(new { error = "Request payload is required." });
        }

        var snapshot = _service.UpdateNode(
            request.NodeId,
            request.Text ?? string.Empty,
            request.LinkId,
            request.LinkText ?? string.Empty);

        return Json(ToStateResponse(snapshot));
    }

    [HttpPost("MoveNode")]
    [ValidateAntiForgeryToken]
    public IActionResult MoveNode([FromBody] MoveNodeRequest? request)
    {
        if (!IsEnabled())
        {
            return NotFound();
        }

        if (request == null)
        {
            return BadRequest(new { error = "Request payload is required." });
        }

        var snapshot = _service.MoveNode(request.NodeId, request.ParentId, request.TargetIndex);
        return Json(ToStateResponse(snapshot));
    }

    [HttpPost("AddSubText")]
    [ValidateAntiForgeryToken]
    public IActionResult AddSubText([FromBody] SubTextRequest? request)
    {
        if (!IsEnabled())
        {
            return NotFound();
        }

        if (request == null)
        {
            return BadRequest(new { error = "Request payload is required." });
        }

        var snapshot = _service.AddSubText(request.NodeId, request.Value ?? string.Empty);
        return Json(ToStateResponse(snapshot));
    }

    [HttpPost("RemoveSubText")]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveSubText([FromBody] SubTextRequest? request)
    {
        if (!IsEnabled())
        {
            return NotFound();
        }

        if (request == null)
        {
            return BadRequest(new { error = "Request payload is required." });
        }

        var snapshot = _service.RemoveSubText(request.NodeId, request.Value ?? string.Empty);
        return Json(ToStateResponse(snapshot));
    }

    [HttpPost("Undo")]
    [ValidateAntiForgeryToken]
    public IActionResult Undo()
    {
        if (!IsEnabled())
        {
            return NotFound();
        }

        return Json(ToStateResponse(_service.Undo()));
    }

    [HttpPost("Redo")]
    [ValidateAntiForgeryToken]
    public IActionResult Redo()
    {
        if (!IsEnabled())
        {
            return NotFound();
        }

        return Json(ToStateResponse(_service.Redo()));
    }

    [HttpPost("Reset")]
    [ValidateAntiForgeryToken]
    public IActionResult Reset()
    {
        if (!IsEnabled())
        {
            return NotFound();
        }

        return Json(ToStateResponse(_service.Reset()));
    }

    [HttpGet("ExportXml")]
    public IActionResult ExportXml()
    {
        if (!IsEnabled())
        {
            return NotFound();
        }

        var xml = _service.ExportXml();
        return Content(xml, "application/xml");
    }

    [HttpPost("SaveXml")]
    [ValidateAntiForgeryToken]
    public IActionResult SaveXml()
    {
        if (!IsEnabled())
        {
            return NotFound();
        }

        var savePath = ResolveSavePath();
        if (string.IsNullOrWhiteSpace(savePath))
        {
            return BadRequest(new { error = "No XML save path is configured." });
        }

        try
        {
            var directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            var xml = _service.ExportXml();
            System.IO.File.WriteAllText(savePath, xml, Encoding.UTF8);

            var snapshot = _service.Load();
            return Json(new
            {
                document = snapshot.Document,
                undoCount = snapshot.UndoHistory.Count,
                redoCount = snapshot.RedoHistory.Count,
                lastUpdatedUtc = snapshot.LastUpdatedUtc,
                savedPath = savePath
            });
        }
        catch (Exception ex) when (
            ex is IOException or
            UnauthorizedAccessException or
            NotSupportedException or
            ArgumentException)
        {
            _logger.LogWarning(ex, "Failed to save protocol XML to path: {Path}", savePath);
            return StatusCode(500, new { error = "Failed to save XML to the configured path." });
        }
    }

    private bool IsEnabled()
    {
        return _featureOptions.ProtocolEditorEnabled;
    }

    private string ResolveSavePath()
    {
        return string.IsNullOrWhiteSpace(_startupOptions.SaveProtocolPath)
            ? _startupOptions.StartupProtocolPath
            : _startupOptions.SaveProtocolPath;
    }

    private static object ToStateResponse(ProtocolEditorSnapshot snapshot)
    {
        return new
        {
            document = snapshot.Document,
            undoCount = snapshot.UndoHistory.Count,
            redoCount = snapshot.RedoHistory.Count,
            lastUpdatedUtc = snapshot.LastUpdatedUtc
        };
    }

    public sealed class AddSectionRequest
    {
        public string? Text { get; init; }
    }

    public sealed class AddChildRequest
    {
        public int ParentId { get; init; }

        public string? Text { get; init; }
    }

    public sealed class RemoveNodeRequest
    {
        public int NodeId { get; init; }
    }

    public sealed class UpdateNodeRequest
    {
        public int NodeId { get; init; }

        public int LinkId { get; init; }

        public string? LinkText { get; init; }

        public string? Text { get; init; }
    }

    public sealed class MoveNodeRequest
    {
        public int NodeId { get; init; }

        public int ParentId { get; init; }

        public int TargetIndex { get; init; }
    }

    public sealed class SubTextRequest
    {
        public int NodeId { get; init; }

        public string? Value { get; init; }
    }
}
