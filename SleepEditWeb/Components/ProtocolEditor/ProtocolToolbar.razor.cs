using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SleepEditWeb.Infrastructure.ProtocolPersistence;
using SleepEditWeb.Models;
using SleepEditWeb.Services;
using SleepEditWeb.Web.ProtocolEditor;

namespace SleepEditWeb.Components.ProtocolEditor;

public partial class ProtocolToolbar : ComponentBase
{
    [Inject] private IProtocolEditorService _service { get; set; } = default!;
    [Inject] private IProtocolEditorPathPolicy _pathPolicy { get; set; } = default!;
    [Inject] private IProtocolEditorFileStore _fileStore { get; set; } = default!;
    [Inject] private IProtocolRepository _repository { get; set; } = default!;
    [Inject] private IJSRuntime _jsRuntime { get; set; } = default!;
    [Inject] private ILogger<ProtocolToolbar> _logger { get; set; } = default!;

    [Parameter] public ProtocolEditorSnapshot Snapshot { get; set; } = new();
    [Parameter] public EventCallback<ProtocolEditorSnapshot> OnMutation { get; set; }
    [Parameter] public EventCallback<string> OnError { get; set; }
    [Parameter] public EventCallback OnAddSection { get; set; }
    [Parameter] public EventCallback OnToggleAllSections { get; set; }
    [Parameter] public int? SelectedNodeId { get; set; }
    [Parameter] public bool IsLoading { get; set; }

    private bool CanUndo => Snapshot.UndoHistory.Count > 0 || Snapshot.UndoDomainHistory.Count > 0;
    private bool CanRedo => Snapshot.RedoHistory.Count > 0 || Snapshot.RedoDomainHistory.Count > 0;

    private async Task HandleAddSection() =>
        await OnAddSection.InvokeAsync();

    private async Task HandleAddChild()
    {
        if (SelectedNodeId is null) return;
        try
        {
            var snapshot = _service.AddChild(SelectedNodeId.Value, "New Statement");
            await OnMutation.InvokeAsync(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AddChild failed.");
            await OnError.InvokeAsync("Failed to add child node.");
        }
    }

    private async Task HandleRemoveNode()
    {
        if (SelectedNodeId is null) return;
        var confirmed = await _jsRuntime.InvokeAsync<bool>("confirm", "Remove this node and all its children?");
        if (!confirmed) return;
        try
        {
            var snapshot = _service.RemoveNode(SelectedNodeId.Value);
            await OnMutation.InvokeAsync(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RemoveNode failed.");
            await OnError.InvokeAsync("Failed to remove node.");
        }
    }

    private async Task HandleUndo()
    {
        try
        {
            var snapshot = _service.Undo();
            await OnMutation.InvokeAsync(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Undo failed.");
            await OnError.InvokeAsync("Undo failed.");
        }
    }

    private async Task HandleRedo()
    {
        try
        {
            var snapshot = _service.Redo();
            await OnMutation.InvokeAsync(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redo failed.");
            await OnError.InvokeAsync("Redo failed.");
        }
    }

    private async Task HandleReset()
    {
        var confirmed = await _jsRuntime.InvokeAsync<bool>("confirm", "Reset the protocol to its default state? All unsaved changes will be lost.");
        if (!confirmed) return;
        try
        {
            var snapshot = _service.Reset();
            await OnMutation.InvokeAsync(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reset failed.");
            await OnError.InvokeAsync("Reset failed.");
        }
    }

    private async Task HandleImport(InputFileChangeEventArgs e)
    {
        try
        {
            await using var stream = e.File.OpenReadStream(maxAllowedSize: 2 * 1024 * 1024);
            using var reader = new StreamReader(stream);
            var xml = await reader.ReadToEndAsync();
            var snapshot = _service.ImportXml(xml);
            await OnMutation.InvokeAsync(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import failed.");
            await OnError.InvokeAsync("Failed to import protocol XML.");
        }
    }

    private async Task HandleSave()
    {
        try
        {
            var savePath = _pathPolicy.ResolveSavePath();
            if (string.IsNullOrWhiteSpace(savePath))
            {
                await OnError.InvokeAsync("No save path is configured.");
                return;
            }
            var xml = _service.ExportXml();
            _fileStore.WriteAllText(savePath, xml);
            var snapshot = _service.Load();
            TryPersistVersion(snapshot.Document, "SaveXml", savePath);
            await OnMutation.InvokeAsync(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Save failed.");
            await OnError.InvokeAsync("Failed to save protocol.");
        }
    }

    private async Task HandleSetDefault()
    {
        var confirmed = await _jsRuntime.InvokeAsync<bool>("confirm", "Set this protocol as the default? This will overwrite the default protocol file.");
        if (!confirmed) return;
        try
        {
            var defaultPath = _pathPolicy.ResolveDefaultPath();
            if (string.IsNullOrWhiteSpace(defaultPath))
            {
                await OnError.InvokeAsync("No default protocol path is configured.");
                return;
            }
            var xml = _service.ExportXml();
            _fileStore.WriteAllText(defaultPath, xml);
            var snapshot = _service.Load();
            TryPersistVersion(snapshot.Document, "SetDefaultProtocol", defaultPath);
            await OnMutation.InvokeAsync(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SetDefault failed.");
            await OnError.InvokeAsync("Failed to set default protocol.");
        }
    }

    private async Task HandleExport()
    {
        try
        {
            var xml = _service.ExportXml();
            var script = BuildDownloadScript(xml, "protocol.xml");
            await _jsRuntime.InvokeVoidAsync("eval", script);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export failed.");
            await OnError.InvokeAsync("Failed to export protocol.");
        }
    }

    private static string BuildDownloadScript(string content, string fileName)
    {
        var escaped = content
            .Replace("\\", "\\\\")
            .Replace("`", "\\`")
            .Replace("${", "\\${");
        return $"(function(){{var b=new Blob([`{escaped}`],{{type:'application/xml'}});var a=document.createElement('a');a.href=URL.createObjectURL(b);a.download='{fileName}';document.body.appendChild(a);a.click();document.body.removeChild(a);URL.revokeObjectURL(a.href);}})();";
    }

    private void TryPersistVersion(ProtocolDocument document, string source, string note)
    {
        try
        {
            _repository.SaveVersion(document, source, note);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist protocol version. Source: {Source}", source);
        }
    }
}
