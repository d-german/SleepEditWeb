using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SleepEditWeb.Infrastructure.ProtocolPersistence;
using SleepEditWeb.Models;
using SleepEditWeb.Services;

namespace SleepEditWeb.Components.ProtocolEditor;

public partial class ProtocolSelector : ComponentBase
{
    [Inject] private IProtocolManagementService ManagementService { get; set; } = null!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] private ILogger<ProtocolSelector> Logger { get; set; } = null!;

    [Parameter] public EventCallback<ProtocolEditorSnapshot> OnProtocolSwitched { get; set; }
    [Parameter] public EventCallback<string> OnError { get; set; }
    [Parameter] public Guid? ActiveProtocolId { get; set; }
    [Parameter] public bool IsLoading { get; set; }

    private IReadOnlyList<SavedProtocolMetadata> _protocols = [];
    private bool _isCreating;
    private string _newProtocolName = string.Empty;
    private Guid? _renamingProtocolId;
    private string _renameText = string.Empty;

    protected override void OnInitialized()
    {
        RefreshProtocolList();
    }

    public void Refresh()
    {
        RefreshProtocolList();
        StateHasChanged();
    }

    private void RefreshProtocolList()
    {
        try
        {
            _protocols = ManagementService.ListProtocols();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load protocol list.");
            _protocols = [];
        }
    }

    private async Task HandleSwitchProtocol(Guid protocolId)
    {
        if (protocolId == ActiveProtocolId) return;
        try
        {
            var snapshot = ManagementService.LoadProtocol(protocolId);
            RefreshProtocolList();
            await OnProtocolSwitched.InvokeAsync(snapshot);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to switch protocol.");
            await OnError.InvokeAsync("Failed to switch protocol.");
        }
    }

    private void HandleStartCreate()
    {
        _isCreating = true;
        _newProtocolName = string.Empty;
    }

    private async Task HandleConfirmCreate()
    {
        if (string.IsNullOrWhiteSpace(_newProtocolName)) return;
        try
        {
            var metadata = ManagementService.CreateProtocol(_newProtocolName.Trim());
            _isCreating = false;
            _newProtocolName = string.Empty;
            RefreshProtocolList();
            // Auto-switch to the new protocol
            var snapshot = ManagementService.LoadProtocol(metadata.ProtocolId);
            await OnProtocolSwitched.InvokeAsync(snapshot);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to create protocol.");
            await OnError.InvokeAsync("Failed to create protocol.");
        }
    }

    private void HandleCancelCreate()
    {
        _isCreating = false;
        _newProtocolName = string.Empty;
    }

    private void HandleStartRename(SavedProtocolMetadata protocol)
    {
        _renamingProtocolId = protocol.ProtocolId;
        _renameText = protocol.Name;
    }

    private void HandleConfirmRename()
    {
        if (_renamingProtocolId is null || string.IsNullOrWhiteSpace(_renameText)) return;
        try
        {
            ManagementService.RenameProtocol(_renamingProtocolId.Value, _renameText.Trim());
            _renamingProtocolId = null;
            _renameText = string.Empty;
            RefreshProtocolList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to rename protocol.");
        }
    }

    private void HandleCancelRename()
    {
        _renamingProtocolId = null;
        _renameText = string.Empty;
    }

    private async Task HandleDelete(SavedProtocolMetadata protocol)
    {
        var confirmed = await JsRuntime.InvokeAsync<bool>("confirm", $"Delete protocol '{protocol.Name}'? This cannot be undone.");
        if (!confirmed) return;
        try
        {
            var deleted = ManagementService.DeleteProtocol(protocol.ProtocolId);
            if (!deleted)
            {
                await OnError.InvokeAsync("Cannot delete the active or default protocol.");
                return;
            }
            RefreshProtocolList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete protocol.");
            await OnError.InvokeAsync("Failed to delete protocol.");
        }
    }

    private async Task HandleSetDefault(SavedProtocolMetadata protocol)
    {
        try
        {
            ManagementService.SetDefaultProtocol(protocol.ProtocolId);
            RefreshProtocolList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to set default protocol.");
            await OnError.InvokeAsync("Failed to set default protocol.");
        }
    }

    private void HandleCreateKeyDown(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
    {
        if (e.Key == "Escape") HandleCancelCreate();
    }

    private void HandleRenameKeyDown(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
    {
        if (e.Key == "Escape") HandleCancelRename();
    }
}
