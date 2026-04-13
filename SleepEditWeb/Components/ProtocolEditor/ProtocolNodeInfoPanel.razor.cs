using Microsoft.AspNetCore.Components;
using SleepEditWeb.Models;
using SleepEditWeb.Services;

namespace SleepEditWeb.Components.ProtocolEditor;

public partial class ProtocolNodeInfoPanel : ComponentBase
{
    [Inject] private IProtocolEditorService Service { get; set; } = null!;
    [Inject] private ILogger<ProtocolNodeInfoPanel> Logger { get; set; } = null!;

    [Parameter] public ProtocolNodeModel? SelectedNode { get; set; }
    [Parameter] public ProtocolDocument Document { get; set; } = new();
    [Parameter] public EventCallback<ProtocolEditorSnapshot> OnMutation { get; set; }
    [Parameter] public EventCallback<string> OnError { get; set; }

    private string _nodeText = string.Empty;
    private string _selectedSubText = string.Empty;
    private string _newSubText = string.Empty;
    private bool _linkPickerOpen;
    private int? _lastSelectedNodeId;

    protected override void OnParametersSet()
    {
        if (SelectedNode?.Id != _lastSelectedNodeId)
        {
            _nodeText = SelectedNode?.Text ?? string.Empty;
            _selectedSubText = string.Empty;
            _newSubText = string.Empty;
            _linkPickerOpen = false;
            _lastSelectedNodeId = SelectedNode?.Id;
        }
    }

    private async Task HandleTextBlur()
    {
        if (SelectedNode is null) return;
        if (_nodeText == SelectedNode.Text) return;
        try
        {
            var snapshot = Service.UpdateNode(SelectedNode.Id, _nodeText, SelectedNode.LinkId, SelectedNode.LinkText);
            await OnMutation.InvokeAsync(snapshot);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UpdateNode text failed.");
            await OnError.InvokeAsync("Failed to update statement text.");
        }
    }

    private async Task HandleAddSubText()
    {
        if (SelectedNode is null || string.IsNullOrWhiteSpace(_newSubText)) return;
        try
        {
            var snapshot = Service.AddSubText(SelectedNode.Id, _newSubText.Trim());
            _newSubText = string.Empty;
            await OnMutation.InvokeAsync(snapshot);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "AddSubText failed.");
            await OnError.InvokeAsync("Failed to add SubText item.");
        }
    }

    private async Task HandleRemoveSubText()
    {
        if (SelectedNode is null || string.IsNullOrWhiteSpace(_selectedSubText)) return;
        try
        {
            var snapshot = Service.RemoveSubText(SelectedNode.Id, _selectedSubText);
            _selectedSubText = string.Empty;
            await OnMutation.InvokeAsync(snapshot);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "RemoveSubText failed.");
            await OnError.InvokeAsync("Failed to remove SubText item.");
        }
    }

    private async Task HandleClearLink()
    {
        if (SelectedNode is null) return;
        try
        {
            var snapshot = Service.UpdateNode(SelectedNode.Id, SelectedNode.Text, 0, string.Empty);
            await OnMutation.InvokeAsync(snapshot);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "ClearLink failed.");
            await OnError.InvokeAsync("Failed to clear link.");
        }
    }

    private async Task HandleLinkSelected((int LinkId, string LinkText) link)
    {
        _linkPickerOpen = false;
        if (SelectedNode is null) return;
        try
        {
            var snapshot = Service.UpdateNode(SelectedNode.Id, SelectedNode.Text, link.LinkId, link.LinkText);
            await OnMutation.InvokeAsync(snapshot);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "SetLink failed.");
            await OnError.InvokeAsync("Failed to set link.");
        }
    }
}
