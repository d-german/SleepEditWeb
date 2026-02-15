using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SleepEditWeb.Infrastructure.ProtocolPersistence;
using SleepEditWeb.Models;
using SleepEditWeb.Services;
using SleepEditWeb.Web.ProtocolEditor;

namespace SleepEditWeb.Controllers;

[Route("ProtocolEditor")]
public sealed class ProtocolEditorController : Controller
{
    private const long MaxImportXmlBytes = 2 * 1024 * 1024;

    private readonly IProtocolEditorService _service;
    private readonly ProtocolEditorFeatureOptions _featureOptions;
    private readonly IProtocolEditorPathPolicy _pathPolicy;
    private readonly IProtocolEditorFileStore _fileStore;
    private readonly IProtocolRepository _repository;
    private readonly IProtocolEditorRequestValidator _requestValidator;
    private readonly IProtocolEditorResponseMapper _responseMapper;
    private readonly ILogger<ProtocolEditorController> _logger;

    public ProtocolEditorController(
        IProtocolEditorService service,
        IOptions<ProtocolEditorFeatureOptions> featureOptions,
        IProtocolEditorPathPolicy pathPolicy,
        IProtocolEditorFileStore fileStore,
        IProtocolRepository repository,
        IProtocolEditorRequestValidator requestValidator,
        IProtocolEditorResponseMapper responseMapper,
        ILogger<ProtocolEditorController> logger)
    {
        _service = service;
        _featureOptions = featureOptions.Value;
        _pathPolicy = pathPolicy;
        _fileStore = fileStore;
        _repository = repository;
        _requestValidator = requestValidator;
        _responseMapper = responseMapper;
        _logger = logger;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        if (!TryEnsureEnabled(nameof(Index), out var denied))
        {
            return denied!;
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
        if (!TryEnsureEnabled(nameof(State), out var denied))
        {
            return denied!;
        }

        _logger.LogDebug("ProtocolEditor state requested.");
        return Json(_responseMapper.ToStateResponse(_service.Load()));
    }

    [HttpPost("AddSection")]
    [ValidateAntiForgeryToken]
    public IActionResult AddSection([FromBody] AddSectionRequest? request)
    {
        if (!TryEnsureEnabled(nameof(AddSection), out var denied))
        {
            return denied!;
        }

        _logger.LogInformation("AddSection requested. TextLength: {Length}", request?.Text?.Length ?? 0);
        var snapshot = _service.AddSection(request?.Text ?? "New Section");
        return Json(_responseMapper.ToStateResponse(snapshot));
    }

    [HttpPost("AddChild")]
    [ValidateAntiForgeryToken]
    public IActionResult AddChild([FromBody] AddChildRequest? request)
    {
        if (!TryEnsureEnabled(nameof(AddChild), out var denied))
        {
            return denied!;
        }

        if (_requestValidator.IsPayloadMissing(request))
        {
            _logger.LogWarning("AddChild rejected because payload was null.");
            return BadRequest(new { error = "Request payload is required." });
        }

        _logger.LogInformation("AddChild requested. ParentId: {ParentId}, TextLength: {Length}", request!.ParentId, request.Text?.Length ?? 0);
        var snapshot = _service.AddChild(request.ParentId, request.Text ?? "New Node");
        return Json(_responseMapper.ToStateResponse(snapshot));
    }

    [HttpPost("RemoveNode")]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveNode([FromBody] RemoveNodeRequest? request)
    {
        if (!TryEnsureEnabled(nameof(RemoveNode), out var denied))
        {
            return denied!;
        }

        if (_requestValidator.IsPayloadMissing(request))
        {
            _logger.LogWarning("RemoveNode rejected because payload was null.");
            return BadRequest(new { error = "Request payload is required." });
        }

        _logger.LogInformation("RemoveNode requested. NodeId: {NodeId}", request!.NodeId);
        var snapshot = _service.RemoveNode(request.NodeId);
        return Json(_responseMapper.ToStateResponse(snapshot));
    }

    [HttpPost("UpdateNode")]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateNode([FromBody] UpdateNodeRequest? request)
    {
        if (!TryEnsureEnabled(nameof(UpdateNode), out var denied))
        {
            return denied!;
        }

        if (_requestValidator.IsPayloadMissing(request))
        {
            _logger.LogWarning("UpdateNode rejected because payload was null.");
            return BadRequest(new { error = "Request payload is required." });
        }

        _logger.LogInformation(
            "UpdateNode requested. NodeId: {NodeId}, LinkId: {LinkId}, TextLength: {Length}",
            request!.NodeId,
            request.LinkId,
            request.Text?.Length ?? 0);

        var snapshot = _service.UpdateNode(
            request.NodeId,
            request.Text ?? string.Empty,
            request.LinkId,
            request.LinkText ?? string.Empty);

        return Json(_responseMapper.ToStateResponse(snapshot));
    }

    [HttpPost("MoveNode")]
    [ValidateAntiForgeryToken]
    public IActionResult MoveNode([FromBody] MoveNodeRequest? request)
    {
        if (!TryEnsureEnabled(nameof(MoveNode), out var denied))
        {
            return denied!;
        }

        if (_requestValidator.IsPayloadMissing(request))
        {
            _logger.LogWarning("MoveNode rejected because payload was null.");
            return BadRequest(new { error = "Request payload is required." });
        }

        _logger.LogInformation(
            "MoveNode requested. NodeId: {NodeId}, ParentId: {ParentId}, TargetIndex: {TargetIndex}",
            request!.NodeId,
            request.ParentId,
            request.TargetIndex);

        var snapshot = _service.MoveNode(request.NodeId, request.ParentId, request.TargetIndex);
        return Json(_responseMapper.ToStateResponse(snapshot));
    }

    [HttpPost("AddSubText")]
    [ValidateAntiForgeryToken]
    public IActionResult AddSubText([FromBody] SubTextRequest? request)
    {
        if (!TryEnsureEnabled(nameof(AddSubText), out var denied))
        {
            return denied!;
        }

        if (_requestValidator.IsPayloadMissing(request))
        {
            _logger.LogWarning("AddSubText rejected because payload was null.");
            return BadRequest(new { error = "Request payload is required." });
        }

        _logger.LogInformation("AddSubText requested. NodeId: {NodeId}, ValueLength: {Length}", request!.NodeId, request.Value?.Length ?? 0);
        var snapshot = _service.AddSubText(request.NodeId, request.Value ?? string.Empty);
        return Json(_responseMapper.ToStateResponse(snapshot));
    }

    [HttpPost("RemoveSubText")]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveSubText([FromBody] SubTextRequest? request)
    {
        if (!TryEnsureEnabled(nameof(RemoveSubText), out var denied))
        {
            return denied!;
        }

        if (_requestValidator.IsPayloadMissing(request))
        {
            _logger.LogWarning("RemoveSubText rejected because payload was null.");
            return BadRequest(new { error = "Request payload is required." });
        }

        _logger.LogInformation("RemoveSubText requested. NodeId: {NodeId}, ValueLength: {Length}", request!.NodeId, request.Value?.Length ?? 0);
        var snapshot = _service.RemoveSubText(request.NodeId, request.Value ?? string.Empty);
        return Json(_responseMapper.ToStateResponse(snapshot));
    }

    [HttpPost("Undo")]
    [ValidateAntiForgeryToken]
    public IActionResult Undo()
    {
        if (!TryEnsureEnabled(nameof(Undo), out var denied))
        {
            return denied!;
        }

        _logger.LogInformation("Undo requested.");
        return Json(_responseMapper.ToStateResponse(_service.Undo()));
    }

    [HttpPost("Redo")]
    [ValidateAntiForgeryToken]
    public IActionResult Redo()
    {
        if (!TryEnsureEnabled(nameof(Redo), out var denied))
        {
            return denied!;
        }

        _logger.LogInformation("Redo requested.");
        return Json(_responseMapper.ToStateResponse(_service.Redo()));
    }

    [HttpPost("Reset")]
    [ValidateAntiForgeryToken]
    public IActionResult Reset()
    {
        if (!TryEnsureEnabled(nameof(Reset), out var denied))
        {
            return denied!;
        }

        _logger.LogInformation("Reset requested.");
        return Json(_responseMapper.ToStateResponse(_service.Reset()));
    }

    [HttpGet("ExportXml")]
    public IActionResult ExportXml()
    {
        if (!TryEnsureEnabled(nameof(ExportXml), out var denied))
        {
            return denied!;
        }

        _logger.LogInformation("ExportXml requested.");
        var xml = _service.ExportXml();
        return Content(xml, "application/xml");
    }

    [HttpPost("SaveXml")]
    [ValidateAntiForgeryToken]
    public IActionResult SaveXml()
    {
        if (!TryEnsureEnabled(nameof(SaveXml), out var denied))
        {
            return denied!;
        }

        var savePath = _pathPolicy.ResolveSavePath();
        var pathValidationError = _requestValidator.ValidateResolvedPath(savePath, "No XML save path is configured.");
        if (pathValidationError != null)
        {
            _logger.LogWarning("SaveXml aborted because no save path could be resolved.");
            return BadRequest(new { error = pathValidationError });
        }

        _logger.LogInformation("SaveXml requested. Resolved save path: {Path}", savePath);

        try
        {
            var xml = _service.ExportXml();
            _fileStore.WriteAllText(savePath, xml);

            var snapshot = _service.Load();
            TryPersistVersion(snapshot.Document, "SaveXml", savePath);
            _logger.LogInformation("SaveXml completed successfully at path: {Path}", savePath);
            return Json(_responseMapper.ToSavedPathResponse(snapshot, savePath));
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
        if (!TryEnsureEnabled(nameof(SetDefaultProtocol), out var denied))
        {
            return denied!;
        }

        var defaultPath = _pathPolicy.ResolveDefaultPath();
        var pathValidationError = _requestValidator.ValidateResolvedPath(defaultPath, "No default protocol path is configured.");
        if (pathValidationError != null)
        {
            _logger.LogWarning("SetDefaultProtocol aborted because no default path could be resolved.");
            return BadRequest(new { error = pathValidationError });
        }

        _logger.LogInformation("SetDefaultProtocol requested. Resolved default path: {Path}", defaultPath);

        try
        {
            var xml = _service.ExportXml();
            _fileStore.WriteAllText(defaultPath, xml);

            var snapshot = _service.Load();
            TryPersistVersion(snapshot.Document, "SetDefaultProtocol", defaultPath);
            _logger.LogInformation("SetDefaultProtocol completed successfully at path: {Path}", defaultPath);
            return Json(_responseMapper.ToDefaultPathResponse(snapshot, defaultPath));
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
        if (!TryEnsureEnabled(nameof(ImportXml), out var denied))
        {
            return denied!;
        }

        var importPath = _pathPolicy.ResolveImportPath(request?.Path);
        _logger.LogInformation("ImportXml requested. Resolved import path: {Path}", importPath);

        var pathValidationError = _requestValidator.ValidateResolvedPath(importPath, "No XML import path is configured.");
        if (pathValidationError != null)
        {
            _logger.LogWarning("ImportXml aborted because no import path could be resolved.");
            return BadRequest(new { error = pathValidationError });
        }

        if (!_fileStore.Exists(importPath))
        {
            _logger.LogWarning("ImportXml aborted because file was not found at path: {Path}", importPath);
            return BadRequest(new { error = "Import XML file was not found.", path = importPath });
        }

        try
        {
            var xml = _fileStore.ReadAllText(importPath);
            var snapshot = _service.ImportXml(xml);
            TryPersistVersion(snapshot.Document, "ImportXml", importPath);
            _logger.LogInformation("ImportXml completed successfully from path: {Path}", importPath);
            return Json(_responseMapper.ToLoadedPathResponse(snapshot, importPath));
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
        if (!TryEnsureEnabled(nameof(ImportXmlUpload), out var denied))
        {
            return denied!;
        }

        var uploadValidationError = _requestValidator.ValidateUploadedFile(file, MaxImportXmlBytes);
        if (uploadValidationError != null)
        {
            if (uploadValidationError == "Uploaded XML file is too large.")
            {
                _logger.LogWarning(
                    "ImportXmlUpload rejected file '{FileName}' because size {Bytes} exceeded limit {LimitBytes}.",
                    file?.FileName,
                    file?.Length,
                    MaxImportXmlBytes);
            }
            else
            {
                _logger.LogWarning("ImportXmlUpload aborted because no file content was uploaded.");
            }

            return BadRequest(new { error = uploadValidationError });
        }

        _logger.LogInformation(
            "ImportXmlUpload requested for file '{FileName}' with size {Bytes}.",
            file!.FileName,
            file.Length);

        try
        {
            var xml = await _fileStore.ReadUploadedXmlAsync(file);
            var snapshot = _service.ImportXml(xml);
            var savedPath = _pathPolicy.ResolveUploadSavePath(file.FileName);
            _fileStore.WriteAllText(savedPath, xml);
            TryPersistVersion(snapshot.Document, "ImportXmlUpload", savedPath);
            _logger.LogInformation(
                "ImportXmlUpload completed successfully for file '{FileName}'. Saved path: {Path}",
                file.FileName,
                savedPath);

            return Json(_responseMapper.ToSavedPathResponse(snapshot, savedPath));
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

    private bool TryEnsureEnabled(string actionName, out IActionResult? denied)
    {
        if (IsEnabled())
        {
            denied = null;
            return true;
        }

        _logger.LogWarning("{Action} denied because feature is disabled.", actionName);
        denied = NotFound();
        return false;
    }

    private void TryPersistVersion(ProtocolDocument document, string source, string note)
    {
        try
        {
            _repository.SaveVersion(document, source, note);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Protocol version persistence failed for source {Source}. Continuing with session snapshot behavior.",
                source);
        }
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
