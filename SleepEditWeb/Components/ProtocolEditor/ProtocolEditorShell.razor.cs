using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using SleepEditWeb.Models;
using SleepEditWeb.Services;

namespace SleepEditWeb.Components.ProtocolEditor;

public partial class ProtocolEditorShell : ComponentBase
{
    [Inject] private IProtocolEditorService Service { get; set; } = null!;
    [Inject] private IProtocolManagementService ManagementService { get; set; } = null!;
    [Inject] private IOptions<ProtocolEditorFeatureOptions> FeatureOptions { get; set; } = null!;
    [Inject] private IJSRuntime JsRuntime { get; set; } = null!;
    [Inject] private ILogger<ProtocolEditorShell> Logger { get; set; } = null!;

    private ProtocolEditorSnapshot _snapshot = new();
    private int? _selectedNodeId;
    private string _statusMessage = "Ready";
    private bool _isLoading = true;
    private bool _addSectionPanelOpen;
    private string _newSectionText = string.Empty;
    private bool _allSectionsCollapsed;
    private Guid? _activeProtocolId;
    private string _activeProtocolName = string.Empty;
    private ProtocolSelector? _protocolSelector;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _snapshot = await Task.Run(() => Service.Load());
            _activeProtocolId = _snapshot.ActiveProtocolId ?? ManagementService.GetActiveProtocolId();
            UpdateActiveProtocolName();
            Logger.LogInformation("ProtocolEditorShell loaded document with {SectionCount} sections.", _snapshot.Document.Sections.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "ProtocolEditorShell failed to load.");
            _statusMessage = "Failed to load protocol. Please refresh.";
        }
        finally
        {
            _isLoading = false;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_addSectionPanelOpen)
        {
            await JsRuntime.InvokeVoidAsync("document.getElementById", "newSectionInput");
            try { await JsRuntime.InvokeVoidAsync("eval", "document.getElementById('newSectionInput')?.focus()"); }
            catch { /* focus is best-effort */ }
        }
    }

    private void HandleMutation(ProtocolEditorSnapshot snapshot)
    {
        _snapshot = snapshot;
        _statusMessage = "Saved";
        StateHasChanged();
    }

    private void HandleError(string message)
    {
        _statusMessage = message;
        StateHasChanged();
    }

    private void HandleNodeSelected(int nodeId)
    {
        _selectedNodeId = nodeId;
    }

    private async Task HandleNodeMoved((int NodeId, int NewParentId, int NewIndex) args)
    {
        try
        {
            var snapshot = Service.MoveNode(args.NodeId, args.NewParentId, args.NewIndex);
            HandleMutation(snapshot);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "MoveNode failed.");
            HandleError("Failed to move node.");
        }
        await Task.CompletedTask;
    }

    private void HandleAddSectionRequested()
    {
        _addSectionPanelOpen = true;
        _newSectionText = string.Empty;
    }

    private void HandleAddSectionConfirm()
    {
        if (string.IsNullOrWhiteSpace(_newSectionText)) return;
        try
        {
            var snapshot = Service.AddSection(_newSectionText.Trim());
            _addSectionPanelOpen = false;
            _newSectionText = string.Empty;
            HandleMutation(snapshot);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "AddSection failed.");
            HandleError("Failed to add section.");
        }
    }

    private void HandleAddSectionKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") HandleAddSectionConfirm();
        if (e.Key == "Escape") { _addSectionPanelOpen = false; _newSectionText = string.Empty; }
    }

    private void HandleProtocolSwitched(ProtocolEditorSnapshot snapshot)
    {
        _snapshot = snapshot;
        _selectedNodeId = null;
        _activeProtocolId = snapshot.ActiveProtocolId;
        UpdateActiveProtocolName();
        _statusMessage = $"Switched to protocol: {_activeProtocolName}";
        StateHasChanged();
    }

    private void UpdateActiveProtocolName()
    {
        if (_activeProtocolId is null)
        {
            _activeProtocolName = string.Empty;
            return;
        }

        try
        {
            var protocols = ManagementService.ListProtocols();
            var active = protocols.FirstOrDefault(p => p.ProtocolId == _activeProtocolId);
            _activeProtocolName = active?.Name ?? "Unknown";
        }
        catch
        {
            _activeProtocolName = "Unknown";
        }
    }

    private void HandleDefaultChanged()
    {
        _protocolSelector?.Refresh();
        _statusMessage = "Default protocol updated.";
        StateHasChanged();
    }

    private void HandleToggleAllSections()
    {
        _allSectionsCollapsed = !_allSectionsCollapsed;
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e is { CtrlKey: true, Key: "z", ShiftKey: false })
        {
            try { HandleMutation(Service.Undo()); } catch { HandleError("Undo failed."); }
        }
        else if (e.CtrlKey && (e.Key == "y" || e is { Key: "z", ShiftKey: true }))
        {
            try { HandleMutation(Service.Redo()); } catch { HandleError("Redo failed."); }
        }
        else if (e.Key == "Delete" && _selectedNodeId.HasValue && !_addSectionPanelOpen)
        {
            var confirmed = await JsRuntime.InvokeAsync<bool>("confirm", "Remove this node and all its children?");
            if (confirmed)
            {
                try { HandleMutation(Service.RemoveNode(_selectedNodeId.Value)); }
                catch { HandleError("Failed to remove node."); }
            }
        }
    }

    private static ProtocolNodeModel? GetSelectedNode(ProtocolDocument document, int? selectedNodeId)
    {
        if (selectedNodeId is null) return null;
        return FindNodeById(document.Sections, selectedNodeId.Value);
    }

    private static ProtocolNodeModel? FindNodeById(IEnumerable<ProtocolNodeModel> nodes, int id)
    {
        foreach (var node in nodes)
        {
            if (node.Id == id) return node;
            var found = FindNodeById(node.Children, id);
            if (found is not null) return found;
        }
        return null;
    }
}
