using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SleepEditWeb.Models;

namespace SleepEditWeb.Components.ProtocolEditor;

public partial class ProtocolTree : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime _jsRuntime { get; set; } = default!;

    [Parameter] public ProtocolDocument Document { get; set; } = new();
    [Parameter] public int? SelectedNodeId { get; set; }
    [Parameter] public EventCallback<int> OnNodeSelected { get; set; }
    [Parameter] public EventCallback<(int NodeId, int NewParentId, int NewIndex)> OnNodeMoved { get; set; }

    private HashSet<int> _collapsedSectionIds = new();
    private DotNetObjectReference<ProtocolTree>? _dotNetRef;

    private const string CollapseStorageKey = "protocolEditor.collapsedSections";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var stored = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", CollapseStorageKey);
            if (!string.IsNullOrWhiteSpace(stored))
            {
                try
                {
                    var ids = JsonSerializer.Deserialize<List<int>>(stored);
                    if (ids != null)
                    {
                        _collapsedSectionIds = new HashSet<int>(ids);
                        StateHasChanged();
                        return;
                    }
                }
                catch { /* ignore malformed stored data */ }
            }
        }

        await InitSortable();
    }

    private async Task InitSortable()
    {
        var visibleListIds = Document.Sections
            .Where(s => !_collapsedSectionIds.Contains(s.Id))
            .Select(s => GetSectionListId(s.Id))
            .ToList();

        if (visibleListIds.Count == 0) return;

        _dotNetRef ??= DotNetObjectReference.Create(this);
        await _jsRuntime.InvokeVoidAsync("protocolDnd.init", _dotNetRef, visibleListIds);
    }

    private async Task HandleSectionToggle(int sectionId)
    {
        if (!_collapsedSectionIds.Add(sectionId))
            _collapsedSectionIds.Remove(sectionId);

        var json = JsonSerializer.Serialize(_collapsedSectionIds.ToList());
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", CollapseStorageKey, json);
    }

    private async Task HandleSectionSelected(int sectionId)
    {
        await OnNodeSelected.InvokeAsync(sectionId);
    }

    [JSInvokable]
    public async Task OnDrop(int nodeId, int newParentId, int newIndex)
    {
        await OnNodeMoved.InvokeAsync((nodeId, newParentId, newIndex));
    }

    private static string GetSectionListId(int sectionId) => $"section-list-{sectionId}";

    private static int CountDescendants(ProtocolNodeModel node)
    {
        var count = node.Children.Count;
        foreach (var child in node.Children)
            count += CountDescendants(child);
        return count;
    }

    public async ValueTask DisposeAsync()
    {
        var allListIds = Document.Sections.Select(s => GetSectionListId(s.Id)).ToList();
        if (allListIds.Count > 0)
        {
            try { await _jsRuntime.InvokeVoidAsync("protocolDnd.destroy", allListIds); }
            catch { /* ignore disposal errors */ }
        }
        _dotNetRef?.Dispose();
    }
}
