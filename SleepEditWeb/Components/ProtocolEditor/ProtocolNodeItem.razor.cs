using Microsoft.AspNetCore.Components;
using SleepEditWeb.Models;

namespace SleepEditWeb.Components.ProtocolEditor;

public partial class ProtocolNodeItem : ComponentBase
{
    [Parameter] public ProtocolNodeModel Node { get; set; } = new();
    [Parameter] public int? SelectedNodeId { get; set; }
    [Parameter] public EventCallback<int> OnNodeSelected { get; set; }
    [Parameter] public int Depth { get; set; }

    private async Task HandleClick() =>
        await OnNodeSelected.InvokeAsync(Node.Id);
}
