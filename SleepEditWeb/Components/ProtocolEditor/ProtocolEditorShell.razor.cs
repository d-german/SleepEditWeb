using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using SleepEditWeb.Models;
using SleepEditWeb.Services;

namespace SleepEditWeb.Components.ProtocolEditor;

public partial class ProtocolEditorShell : ComponentBase
{
    [Inject] private IProtocolEditorService _service { get; set; } = default!;
    [Inject] private IOptions<ProtocolEditorFeatureOptions> _featureOptions { get; set; } = default!;
    [Inject] private IJSRuntime _jsRuntime { get; set; } = default!;
    [Inject] private ILogger<ProtocolEditorShell> _logger { get; set; } = default!;

    private ProtocolEditorSnapshot _snapshot = new();
    private int? _selectedNodeId;
    private string _statusMessage = "Ready";
    private bool _isLoading = true;
    private bool _addSectionPanelOpen;
    private string _newSectionText = string.Empty;
    private bool _allSectionsCollapsed;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _snapshot = await Task.Run(() => _service.Load());
            _logger.LogInformation("ProtocolEditorShell loaded document with {SectionCount} sections.", _snapshot.Document.Sections.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ProtocolEditorShell failed to load.");
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
            await _jsRuntime.InvokeVoidAsync("document.getElementById", "newSectionInput");
            try { await _jsRuntime.InvokeVoidAsync("eval", "document.getElementById('newSectionInput')?.focus()"); }
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
            var snapshot = _service.MoveNode(args.NodeId, args.NewParentId, args.NewIndex);
            HandleMutation(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MoveNode failed.");
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
            var snapshot = _service.AddSection(_newSectionText.Trim());
            _addSectionPanelOpen = false;
            _newSectionText = string.Empty;
            HandleMutation(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AddSection failed.");
            HandleError("Failed to add section.");
        }
    }

    private void HandleAddSectionKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") HandleAddSectionConfirm();
        if (e.Key == "Escape") { _addSectionPanelOpen = false; _newSectionText = string.Empty; }
    }

    private void HandleToggleAllSections()
    {
        _allSectionsCollapsed = !_allSectionsCollapsed;
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.CtrlKey && e.Key == "z" && !e.ShiftKey)
        {
            try { HandleMutation(_service.Undo()); } catch { HandleError("Undo failed."); }
        }
        else if (e.CtrlKey && (e.Key == "y" || (e.Key == "z" && e.ShiftKey)))
        {
            try { HandleMutation(_service.Redo()); } catch { HandleError("Redo failed."); }
        }
        else if (e.Key == "Delete" && _selectedNodeId.HasValue && !_addSectionPanelOpen)
        {
            var confirmed = await _jsRuntime.InvokeAsync<bool>("confirm", "Remove this node and all its children?");
            if (confirmed)
            {
                try { HandleMutation(_service.RemoveNode(_selectedNodeId.Value)); }
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
