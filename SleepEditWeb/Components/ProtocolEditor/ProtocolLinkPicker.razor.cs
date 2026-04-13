using Microsoft.AspNetCore.Components;
using SleepEditWeb.Models;

namespace SleepEditWeb.Components.ProtocolEditor;

public partial class ProtocolLinkPicker : ComponentBase
{
    [Parameter] public ProtocolDocument Document { get; set; } = new();
    [Parameter] public ProtocolNodeModel SourceNode { get; set; } = new();
    [Parameter] public EventCallback<(int LinkId, string LinkText)> OnLinkSelected { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }

    private string _searchQuery = string.Empty;
    private int? _pickedNodeId;

    protected override void OnParametersSet()
    {
        _pickedNodeId = SourceNode.LinkId > 0 ? SourceNode.LinkId : null;
    }

    private async Task HandleSelect()
    {
        if (_pickedNodeId is null) return;
        var pickedNode = FindNodeById(Document.Sections, _pickedNodeId.Value);
        await OnLinkSelected.InvokeAsync((_pickedNodeId.Value, pickedNode?.Text ?? string.Empty));
    }

    private async Task HandleCancel() =>
        await OnCancelled.InvokeAsync();

    private static IEnumerable<(ProtocolNodeModel Section, IEnumerable<(ProtocolNodeModel Node, int Depth)> Nodes)> GetFilteredSections(
        ProtocolDocument doc, string query)
    {
        var q = query.Trim();
        foreach (var sec in doc.Sections)
        {
            var matchingNodes = FlattenNodes(sec.Children, 0)
                .Where(entry => string.IsNullOrEmpty(q)
                    || entry.Node.Text.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || entry.Node.Id.ToString() == q).ToList();

            if (matchingNodes.Count != 0)
                yield return (sec, matchingNodes);
        }
    }

    private static IEnumerable<(ProtocolNodeModel Node, int Depth)> FlattenNodes(IEnumerable<ProtocolNodeModel> nodes, int depth)
    {
        foreach (var node in nodes)
        {
            yield return (node, depth);
            foreach (var child in FlattenNodes(node.Children, depth + 1))
                yield return child;
        }
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
