using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using SleepEditWeb.Models;
using SleepEditWeb.Services;

namespace SleepEditWeb.Components.ProtocolEditor;

public partial class ProtocolToolbar : ComponentBase
{
    [Inject] private IProtocolEditorService Service { get; set; } = null!;
    [Inject] private IProtocolManagementService ManagementService { get; set; } = null!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] private ILogger<ProtocolToolbar> Logger { get; set; } = null!;

    [Parameter] public ProtocolEditorSnapshot Snapshot { get; set; } = new();
    [Parameter] public EventCallback<ProtocolEditorSnapshot> OnMutation { get; set; }
    [Parameter] public EventCallback OnSetDefault { get; set; }
    [Parameter] public EventCallback<string> OnError { get; set; }
    [Parameter] public EventCallback OnAddSection { get; set; }
    [Parameter] public EventCallback OnToggleAllSections { get; set; }
    [Parameter] public int? SelectedNodeId { get; set; }
    [Parameter] public bool IsLoading { get; set; }
    [Parameter] public Guid? ActiveProtocolId { get; set; }

    private bool CanUndo => Snapshot.UndoHistory.Count > 0 || Snapshot.UndoDomainHistory.Count > 0;
    private bool CanRedo => Snapshot.RedoHistory.Count > 0 || Snapshot.RedoDomainHistory.Count > 0;

    private async Task HandleAddSection() =>
        await OnAddSection.InvokeAsync();

    private async Task HandleAddChild()
    {
        if (SelectedNodeId is null) return;
        try
        {
            var snapshot = Service.AddChild(SelectedNodeId.Value, "New Statement");
            await OnMutation.InvokeAsync(snapshot);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "AddChild failed.");
            await OnError.InvokeAsync("Failed to add child node.");
        }
    }

    private async Task HandleRemoveNode()
    {
        if (SelectedNodeId is null) return;
        var confirmed = await JsRuntime.InvokeAsync<bool>("confirm", "Remove this node and all its children?");
        if (!confirmed) return;
        try
        {
            var snapshot = Service.RemoveNode(SelectedNodeId.Value);
            await OnMutation.InvokeAsync(snapshot);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "RemoveNode failed.");
            await OnError.InvokeAsync("Failed to remove node.");
        }
    }

    private async Task HandleUndo()
    {
        try
        {
            var snapshot = Service.Undo();
            await OnMutation.InvokeAsync(snapshot);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Undo failed.");
            await OnError.InvokeAsync("Undo failed.");
        }
    }

    private async Task HandleRedo()
    {
        try
        {
            var snapshot = Service.Redo();
            await OnMutation.InvokeAsync(snapshot);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Redo failed.");
            await OnError.InvokeAsync("Redo failed.");
        }
    }

    private async Task HandleReset()
    {
        var confirmed = await JsRuntime.InvokeAsync<bool>("confirm", "Reset the protocol to its default state? All unsaved changes will be lost.");
        if (!confirmed) return;
        try
        {
            var snapshot = Service.Reset();
            await OnMutation.InvokeAsync(snapshot);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Reset failed.");
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
            var snapshot = Service.ImportXml(xml);
            ManagementService.SaveActiveProtocol(snapshot.Document, "ImportXml");
            await OnMutation.InvokeAsync(snapshot);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Import failed.");
            await OnError.InvokeAsync("Failed to import protocol XML.");
        }
    }

    private async Task HandleSave()
    {
        try
        {
            var snapshot = Service.Load();
            ManagementService.SaveActiveProtocol(snapshot.Document, "SaveXml");
            await OnMutation.InvokeAsync(snapshot);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Save failed.");
            await OnError.InvokeAsync("Failed to save protocol.");
        }
    }

    private async Task HandleSetDefault()
    {
        if (!ActiveProtocolId.HasValue) return;
        try
        {
            ManagementService.SetDefaultProtocol(ActiveProtocolId.Value);
            await OnSetDefault.InvokeAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "SetDefault failed.");
            await OnError.InvokeAsync("Failed to set default protocol.");
        }
    }

    private async Task HandleExport()
    {
        try
        {
            var xml = Service.ExportXml();
            var script = BuildDownloadScript(xml, "protocol.xml");
            await JsRuntime.InvokeVoidAsync("eval", script);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Export failed.");
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
}
