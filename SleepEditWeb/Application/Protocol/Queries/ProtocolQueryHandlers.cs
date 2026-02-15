using SleepEditWeb.Protocol.Domain;

namespace SleepEditWeb.Application.Protocol.Queries;

public interface IProtocolQueryHandler<in TQuery, TResult>
{
    ProtocolResult<TResult> Handle(ProtocolTreeDocument document, TQuery query);
}

public readonly record struct FindNodeByIdQuery(int NodeId);

public sealed class FindNodeByIdQueryHandler : IProtocolQueryHandler<FindNodeByIdQuery, ProtocolTreeNode>
{
    public ProtocolResult<ProtocolTreeNode> Handle(ProtocolTreeDocument document, FindNodeByIdQuery query)
    {
        var node = ProtocolTreeFunctions.FindNode(document, query.NodeId);
        if (node == null)
        {
            return ProtocolResult<ProtocolTreeNode>.Failure("node_not_found", "Requested node could not be found.");
        }

        return ProtocolResult<ProtocolTreeNode>.Success(node);
    }
}
