using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SleepEditWeb.Models;
using SleepEditWeb.Services;

namespace SleepEditWeb.Controllers;

[Route("ProtocolEditor")]
public sealed class ProtocolEditorController : Controller
{
    private const long MaxImportXmlBytes = 2 * 1024 * 1024;

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
            _logger.LogWarning("ProtocolEditor index denied because feature is disabled.");
            return NotFound();
        }

        _logger.LogInformation("ProtocolEditor index requested.");
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
            _logger.LogWarning("ProtocolEditor state denied because feature is disabled.");
            return NotFound();
        }

        _logger.LogDebug("ProtocolEditor state requested.");
        return Json(ToStateResponse(_service.Load()));
    }

    [HttpPost("AddSection")]
    [ValidateAntiForgeryToken]
    public IActionResult AddSection([FromBody] AddSectionRequest? request)
    {
        if (!IsEnabled())
        {
            _logger.LogWarning("AddSection denied because feature is disabled.");
            return NotFound();
        }

        _logger.LogInformation("AddSection requested. TextLength: {Length}", request?.Text?.Length ?? 0);
        var snapshot = _service.AddSection(request?.Text ?? "New Section");
        return Json(ToStateResponse(snapshot));
    }

    [HttpPost("AddChild")]
    [ValidateAntiForgeryToken]
    public IActionResult AddChild([FromBody] AddChildRequest? request)
    {
        if (!IsEnabled())
        {
            _logger.LogWarning("AddChild denied because feature is disabled.");
            return NotFound();
        }

        if (request == null)
        {
            _logger.LogWarning("AddChild rejected because payload was null.");
            return BadRequest(new { error = "Request payload is required." });
        }

        _logger.LogInformation("AddChild requested. ParentId: {ParentId}, TextLength: {Length}", request.ParentId, request.Text?.Length ?? 0);
        var snapshot = _service.AddChild(request.ParentId, request.Text ?? "New Node");
        return Json(ToStateResponse(snapshot));
    }

    [HttpPost("RemoveNode")]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveNode([FromBody] RemoveNodeRequest? request)
    {
        if (!IsEnabled())
        {
            _logger.LogWarning("RemoveNode denied because feature is disabled.");
            return NotFound();
        }

        if (request == null)
        {
            _logger.LogWarning("RemoveNode rejected because payload was null.");
            return BadRequest(new { error = "Request payload is required." });
        }

        _logger.LogInformation("RemoveNode requested. NodeId: {NodeId}", request.NodeId);
        var snapshot = _service.RemoveNode(request.NodeId);
        return Json(ToStateResponse(snapshot));
    }

    [HttpPost("UpdateNode")]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateNode([FromBody] UpdateNodeRequest? request)
    {
        if (!IsEnabled())
        {
            _logger.LogWarning("UpdateNode denied because feature is disabled.");
            return NotFound();
        }

        if (request == null)
        {
            _logger.LogWarning("UpdateNode rejected because payload was null.");
            return BadRequest(new { error = "Request payload is required." });
        }

        _logger.LogInformation("UpdateNode requested. NodeId: {NodeId}, LinkId: {LinkId}, TextLength: {Length}", request.NodeId, request.LinkId, request.Text?.Length ?? 0);
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
            _logger.LogWarning("MoveNode denied because feature is disabled.");
            return NotFound();
        }

        if (request == null)
        {
            _logger.LogWarning("MoveNode rejected because payload was null.");
            return BadRequest(new { error = "Request payload is required." });
        }

        _logger.LogInformation(
            "MoveNode requested. NodeId: {NodeId}, ParentId: {ParentId}, TargetIndex: {TargetIndex}",
            request.NodeId,
            request.ParentId,
            request.TargetIndex);
        var snapshot = _service.MoveNode(request.NodeId, request.ParentId, request.TargetIndex);
        return Json(ToStateResponse(snapshot));
    }

    [HttpPost("AddSubText")]
    [ValidateAntiForgeryToken]
    public IActionResult AddSubText([FromBody] SubTextRequest? request)
    {
        if (!IsEnabled())
        {
            _logger.LogWarning("AddSubText denied because feature is disabled.");
            return NotFound();
        }

        if (request == null)
        {
            _logger.LogWarning("AddSubText rejected because payload was null.");
            return BadRequest(new { error = "Request payload is required." });
        }

        _logger.LogInformation("AddSubText requested. NodeId: {NodeId}, ValueLength: {Length}", request.NodeId, request.Value?.Length ?? 0);
        var snapshot = _service.AddSubText(request.NodeId, request.Value ?? string.Empty);
        return Json(ToStateResponse(snapshot));
    }

    [HttpPost("RemoveSubText")]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveSubText([FromBody] SubTextRequest? request)
    {
        if (!IsEnabled())
        {
            _logger.LogWarning("RemoveSubText denied because feature is disabled.");
            return NotFound();
        }

        if (request == null)
        {
            _logger.LogWarning("RemoveSubText rejected because payload was null.");
            return BadRequest(new { error = "Request payload is required." });
        }

        _logger.LogInformation("RemoveSubText requested. NodeId: {NodeId}, ValueLength: {Length}", request.NodeId, request.Value?.Length ?? 0);
        var snapshot = _service.RemoveSubText(request.NodeId, request.Value ?? string.Empty);
        return Json(ToStateResponse(snapshot));
    }

    [HttpPost("Undo")]
    [ValidateAntiForgeryToken]
    public IActionResult Undo()
    {
        if (!IsEnabled())
        {
            _logger.LogWarning("Undo denied because feature is disabled.");
            return NotFound();
        }

        _logger.LogInformation("Undo requested.");
        return Json(ToStateResponse(_service.Undo()));
    }

    [HttpPost("Redo")]
    [ValidateAntiForgeryToken]
    public IActionResult Redo()
    {
        if (!IsEnabled())
        {
            _logger.LogWarning("Redo denied because feature is disabled.");
            return NotFound();
        }

        _logger.LogInformation("Redo requested.");
        return Json(ToStateResponse(_service.Redo()));
    }

    [HttpPost("Reset")]
    [ValidateAntiForgeryToken]
    public IActionResult Reset()
    {
        if (!IsEnabled())
        {
            _logger.LogWarning("Reset denied because feature is disabled.");
            return NotFound();
        }

        _logger.LogInformation("Reset requested.");
        return Json(ToStateResponse(_service.Reset()));
    }

    [HttpGet("ExportXml")]
    public IActionResult ExportXml()
    {
        if (!IsEnabled())
        {
            _logger.LogWarning("ExportXml denied because feature is disabled.");
            return NotFound();
        }

        _logger.LogInformation("ExportXml requested.");
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
            _logger.LogWarning("SaveXml aborted because no save path could be resolved.");
            return BadRequest(new { error = "No XML save path is configured." });
        }

        _logger.LogInformation("SaveXml requested. Resolved save path: {Path}", savePath);

        try
        {
            var directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var xml = _service.ExportXml();
            System.IO.File.WriteAllText(savePath, xml, Encoding.UTF8);

            var snapshot = _service.Load();
            _logger.LogInformation("SaveXml completed successfully at path: {Path}", savePath);
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
            _logger.LogWarning(ex, "SaveXml failed for path: {Path}", savePath);
            return StatusCode(500, new { error = "Failed to save XML to the configured path." });
        }
    }

    [HttpPost("SetDefaultProtocol")]
    [ValidateAntiForgeryToken]
    public IActionResult SetDefaultProtocol()
    {
        if (!IsEnabled())
        {
            return NotFound();
        }

        var defaultPath = ResolveDefaultPath();
        if (string.IsNullOrWhiteSpace(defaultPath))
        {
            _logger.LogWarning("SetDefaultProtocol aborted because no default path could be resolved.");
            return BadRequest(new { error = "No default protocol path is configured." });
        }

        _logger.LogInformation("SetDefaultProtocol requested. Resolved default path: {Path}", defaultPath);

        try
        {
            var directory = Path.GetDirectoryName(defaultPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var xml = _service.ExportXml();
            System.IO.File.WriteAllText(defaultPath, xml, Encoding.UTF8);

            var snapshot = _service.Load();
            _logger.LogInformation("SetDefaultProtocol completed successfully at path: {Path}", defaultPath);
            return Json(new
            {
                document = snapshot.Document,
                undoCount = snapshot.UndoHistory.Count,
                redoCount = snapshot.RedoHistory.Count,
                lastUpdatedUtc = snapshot.LastUpdatedUtc,
                defaultPath
            });
        }
        catch (Exception ex) when (
            ex is IOException or
            UnauthorizedAccessException or
            NotSupportedException or
            ArgumentException)
        {
            _logger.LogWarning(ex, "SetDefaultProtocol failed for path: {Path}", defaultPath);
            return StatusCode(500, new { error = "Failed to set default protocol." });
        }
    }

    [HttpPost("ImportXml")]
    [ValidateAntiForgeryToken]
    public IActionResult ImportXml([FromBody] ImportXmlRequest? request)
    {
        if (!IsEnabled())
        {
            return NotFound();
        }

        var importPath = ResolveImportPath(request);
        _logger.LogInformation("ImportXml requested. Resolved import path: {Path}", importPath);
        if (string.IsNullOrWhiteSpace(importPath))
        {
            _logger.LogWarning("ImportXml aborted because no import path could be resolved.");
            return BadRequest(new { error = "No XML import path is configured." });
        }

        if (!System.IO.File.Exists(importPath))
        {
            _logger.LogWarning("ImportXml aborted because file was not found at path: {Path}", importPath);
            return BadRequest(new { error = "Import XML file was not found.", path = importPath });
        }

        try
        {
            var xml = System.IO.File.ReadAllText(importPath, Encoding.UTF8);
            var snapshot = _service.ImportXml(xml);
            _logger.LogInformation("ImportXml completed successfully from path: {Path}", importPath);
            return Json(new
            {
                document = snapshot.Document,
                undoCount = snapshot.UndoHistory.Count,
                redoCount = snapshot.RedoHistory.Count,
                lastUpdatedUtc = snapshot.LastUpdatedUtc,
                loadedPath = importPath
            });
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Invalid protocol XML format at path: {Path}", importPath);
            return BadRequest(new { error = "Invalid XML format for protocol import." });
        }
        catch (Exception ex) when (
            ex is IOException or
            UnauthorizedAccessException or
            NotSupportedException or
            ArgumentException)
        {
            _logger.LogWarning(ex, "Failed to import protocol XML from path: {Path}", importPath);
            return StatusCode(500, new { error = "Failed to import XML from the configured path." });
        }
    }

    [HttpPost("ImportXmlUpload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportXmlUpload(IFormFile? file)
    {
        if (!IsEnabled())
        {
            return NotFound();
        }

        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("ImportXmlUpload aborted because no file content was uploaded.");
            return BadRequest(new { error = "No XML file was uploaded." });
        }

        if (file.Length > MaxImportXmlBytes)
        {
            _logger.LogWarning(
                "ImportXmlUpload rejected file '{FileName}' because size {Bytes} exceeded limit {LimitBytes}.",
                file.FileName,
                file.Length,
                MaxImportXmlBytes);
            return BadRequest(new { error = "Uploaded XML file is too large." });
        }

        _logger.LogInformation(
            "ImportXmlUpload requested for file '{FileName}' with size {Bytes}.",
            file.FileName,
            file.Length);

        try
        {
            var xml = await ReadUploadedXmlAsync(file);
            var snapshot = _service.ImportXml(xml);
            var savedPath = WriteXmlToResolvedPath(xml, file.FileName);
            _logger.LogInformation(
                "ImportXmlUpload completed successfully for file '{FileName}'. Saved path: {Path}",
                file.FileName,
                savedPath);
            return Json(new
            {
                document = snapshot.Document,
                undoCount = snapshot.UndoHistory.Count,
                redoCount = snapshot.RedoHistory.Count,
                lastUpdatedUtc = snapshot.LastUpdatedUtc,
                savedPath
            });
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Invalid uploaded protocol XML.");
            return BadRequest(new { error = "Invalid XML format for protocol import." });
        }
        catch (Exception ex) when (
            ex is IOException or
            UnauthorizedAccessException or
            NotSupportedException or
            ArgumentException)
        {
            _logger.LogWarning(ex, "Failed to import uploaded protocol XML.");
            return StatusCode(500, new { error = "Failed to import uploaded XML." });
        }
    }

    private bool IsEnabled()
    {
        return _featureOptions.ProtocolEditorEnabled;
    }

    private string ResolveSavePath()
    {
        if (!string.IsNullOrWhiteSpace(_startupOptions.SaveProtocolPath))
        {
            return _startupOptions.SaveProtocolPath;
        }

        if (!string.IsNullOrWhiteSpace(_startupOptions.StartupProtocolPath))
        {
            return _startupOptions.StartupProtocolPath;
        }

        if (!string.IsNullOrWhiteSpace(_startupOptions.DefaultProtocolPath))
        {
            return _startupOptions.DefaultProtocolPath;
        }

        return Path.Combine(AppContext.BaseDirectory, "Data", "protocols", "default-protocol.xml");
    }

    private string ResolveDefaultPath()
    {
        if (!string.IsNullOrWhiteSpace(_startupOptions.DefaultProtocolPath))
        {
            return _startupOptions.DefaultProtocolPath;
        }

        if (!string.IsNullOrWhiteSpace(_startupOptions.StartupProtocolPath))
        {
            return _startupOptions.StartupProtocolPath;
        }

        if (!string.IsNullOrWhiteSpace(_startupOptions.SaveProtocolPath))
        {
            return _startupOptions.SaveProtocolPath;
        }

        return Path.Combine(AppContext.BaseDirectory, "Data", "protocols", "default-protocol.xml");
    }

    private static async Task<string> ReadUploadedXmlAsync(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return await reader.ReadToEndAsync();
    }

    private string WriteXmlToResolvedPath(string xml, string? uploadedFileName)
    {
        var savePath = ResolveUploadedFileSavePath(uploadedFileName);
        var directory = Path.GetDirectoryName(savePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        System.IO.File.WriteAllText(savePath, xml, Encoding.UTF8);
        return savePath;
    }

    private string ResolveUploadedFileSavePath(string? uploadedFileName)
    {
        if (!string.IsNullOrWhiteSpace(_startupOptions.SaveProtocolPath))
        {
            return _startupOptions.SaveProtocolPath;
        }

        var safeFileName = string.IsNullOrWhiteSpace(uploadedFileName)
            ? "protocol-upload.xml"
            : Path.GetFileName(uploadedFileName);

        if (!safeFileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
        {
            safeFileName = $"{safeFileName}.xml";
        }

        var fallbackDirectory = Path.Combine(AppContext.BaseDirectory, "Data", "protocols");
        return Path.Combine(fallbackDirectory, safeFileName);
    }

    private string ResolveImportPath(ImportXmlRequest? request)
    {
        if (!string.IsNullOrWhiteSpace(request?.Path))
        {
            return request.Path;
        }

        return ResolveSavePath();
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

    public sealed class ImportXmlRequest
    {
        public string? Path { get; init; }
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
