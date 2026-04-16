using LiteDB;
using Microsoft.AspNetCore.Mvc;
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
    private readonly IProtocolRepository _repository;
    private readonly IProtocolEditorRequestValidator _requestValidator;
    private readonly IProtocolEditorResponseMapper _responseMapper;
    private readonly IProtocolManagementService _managementService;
    private readonly ILogger<ProtocolEditorController> _logger;

    public ProtocolEditorController(
        IProtocolEditorService service,
        IOptions<ProtocolEditorFeatureOptions> featureOptions,
        IProtocolRepository repository,
        IProtocolEditorRequestValidator requestValidator,
        IProtocolEditorResponseMapper responseMapper,
        IProtocolManagementService managementService,
        ILogger<ProtocolEditorController> logger)
    {
        _service = service;
        _featureOptions = featureOptions.Value;
        _repository = repository;
        _requestValidator = requestValidator;
        _responseMapper = responseMapper;
        _managementService = managementService;
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

        _logger.LogInformation("SaveXml requested.");

        var snapshot = _service.Load();
        try
        {
            _repository.SaveCurrentProtocol(snapshot.Document, "SaveXml");

            var activeProtocolId = _managementService.GetActiveProtocolId();
            if (activeProtocolId.HasValue)
            {
                _repository.SaveProtocol(activeProtocolId.Value, snapshot.Document.Text, snapshot.Document, "SaveXml");
            }
        }
        catch (Exception ex) when (
            ex is IOException or
            UnauthorizedAccessException or
            LiteException)
        {
            _logger.LogWarning(ex, "SaveXml failed while persisting the current protocol.");
            return StatusCode(500, new { error = "Failed to save protocol." });
        }

        _logger.LogInformation("SaveXml completed successfully.");
        return Json(_responseMapper.ToStateResponse(snapshot));
    }

    [HttpPost("SetDefaultProtocol")]
    [ValidateAntiForgeryToken]
    public IActionResult SetDefaultProtocol()
    {
        if (!TryEnsureEnabled(nameof(SetDefaultProtocol), out var denied))
        {
            return denied!;
        }

        _logger.LogInformation("SetDefaultProtocol requested.");

        var snapshot = _service.Load();
        try
        {
            var activeProtocolId = _managementService.GetActiveProtocolId();
            if (activeProtocolId.HasValue)
            {
                _managementService.SetDefaultProtocol(activeProtocolId.Value);
            }
            else
            {
                // Backward compat: save as current protocol if no active ID
                _repository.SaveCurrentProtocol(snapshot.Document, "SetDefaultProtocol");
            }
        }
        catch (Exception ex) when (
            ex is IOException or
            UnauthorizedAccessException or
            LiteException)
        {
            _logger.LogWarning(ex, "SetDefaultProtocol failed while persisting the current protocol.");
            return StatusCode(500, new { error = "Failed to set default protocol." });
        }

        _logger.LogInformation("SetDefaultProtocol completed successfully.");
        return Json(_responseMapper.ToStateResponse(snapshot));
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
            using var reader = new StreamReader(file.OpenReadStream());
            var xml = await reader.ReadToEndAsync();
            var snapshot = _service.ImportXml(xml);
            _repository.SaveCurrentProtocol(snapshot.Document, "ImportXmlUpload");
            _logger.LogInformation(
                "ImportXmlUpload completed successfully for file '{FileName}'.",
                file.FileName);

            return Json(_responseMapper.ToStateResponse(snapshot));
        }
        catch (FormatException ex)
        {
            _logger.LogWarning(ex, "Invalid uploaded protocol XML.");
            return BadRequest(new { error = "Invalid XML format for protocol import." });
        }
        catch (Exception ex) when (
            ex is IOException or
            UnauthorizedAccessException or
            ObjectDisposedException or
            LiteException)
        {
            _logger.LogWarning(ex, "Failed to import uploaded protocol XML.");
            return StatusCode(500, new { error = "Failed to import uploaded XML." });
        }
    }


    [HttpPost("CreateProtocol")]
    [ValidateAntiForgeryToken]
    public IActionResult CreateProtocol([FromBody] CreateProtocolRequest? request)
    {
        if (!TryEnsureEnabled(nameof(CreateProtocol), out var denied)) return denied!;

        var name = request?.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { error = "Protocol name is required." });
        }

        _logger.LogInformation("CreateProtocol requested. Name: {Name}", name);
        var metadata = _managementService.CreateProtocol(name);
        return Json(metadata);
    }

    [HttpGet("ListProtocols")]
    public IActionResult ListProtocols()
    {
        if (!TryEnsureEnabled(nameof(ListProtocols), out var denied)) return denied!;

        _logger.LogDebug("ListProtocols requested.");
        var protocols = _managementService.ListProtocols();
        return Json(protocols);
    }

    [HttpPost("LoadProtocol/{id:guid}")]
    [ValidateAntiForgeryToken]
    public IActionResult LoadProtocol(Guid id)
    {
        if (!TryEnsureEnabled(nameof(LoadProtocol), out var denied)) return denied!;

        _logger.LogInformation("LoadProtocol requested. ProtocolId: {ProtocolId}", id);
        try
        {
            var snapshot = _managementService.LoadProtocol(id);
            return Json(_responseMapper.ToStateResponse(snapshot));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "LoadProtocol failed for {ProtocolId}.", id);
            return NotFound(new { error = $"Protocol {id} not found." });
        }
    }

    [HttpPost("DeleteProtocol/{id:guid}")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteProtocol(Guid id)
    {
        if (!TryEnsureEnabled(nameof(DeleteProtocol), out var denied)) return denied!;

        _logger.LogInformation("DeleteProtocol requested. ProtocolId: {ProtocolId}", id);
        var deleted = _managementService.DeleteProtocol(id);
        if (!deleted)
        {
            return BadRequest(new { error = "Cannot delete the active or default protocol." });
        }
        return Json(new { success = true });
    }

    [HttpPost("RenameProtocol/{id:guid}")]
    [ValidateAntiForgeryToken]
    public IActionResult RenameProtocol(Guid id, [FromBody] RenameProtocolRequest? request)
    {
        if (!TryEnsureEnabled(nameof(RenameProtocol), out var denied)) return denied!;

        var name = request?.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { error = "Protocol name is required." });
        }

        _logger.LogInformation("RenameProtocol requested. ProtocolId: {ProtocolId}, NewName: {Name}", id, name);
        try
        {
            _managementService.RenameProtocol(id, name);
            return Json(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "RenameProtocol failed for {ProtocolId}.", id);
            return NotFound(new { error = $"Protocol {id} not found." });
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

    public sealed class CreateProtocolRequest
    {
        public string? Name { get; init; }
    }

    public sealed class RenameProtocolRequest
    {
        public string? Name { get; init; }
    }
}
