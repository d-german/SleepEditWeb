using SleepEditWeb.Protocol.Domain;

namespace SleepEditWeb.Application.Protocol.Commands;

public interface IProtocolCommandHandler<in TCommand>
{
    ProtocolResult<ProtocolTreeDocument> Handle(ProtocolTreeDocument document, TCommand command);
}

public sealed record AddSectionCommand(string Text);

public sealed class AddSectionCommandHandler : IProtocolCommandHandler<AddSectionCommand>
{
    public ProtocolResult<ProtocolTreeDocument> Handle(ProtocolTreeDocument document, AddSectionCommand command)
    {
        var updated = ProtocolTreeFunctions.AddSection(document, command.Text);
        return ProtocolResult<ProtocolTreeDocument>.Success(updated);
    }
}

public sealed record AddChildCommand(int ParentId, string Text);

public sealed class AddChildCommandHandler : IProtocolCommandHandler<AddChildCommand>
{
    public ProtocolResult<ProtocolTreeDocument> Handle(ProtocolTreeDocument document, AddChildCommand command)
    {
        var updated = ProtocolTreeFunctions.AddChild(document, command.ParentId, command.Text);
        if (ReferenceEquals(updated, document))
        {
            return ProtocolResult<ProtocolTreeDocument>.Failure("parent_not_found", "Requested parent node could not be resolved.");
        }

        return ProtocolResult<ProtocolTreeDocument>.Success(updated);
    }
}

public sealed record RemoveNodeCommand(int NodeId);

public sealed class RemoveNodeCommandHandler : IProtocolCommandHandler<RemoveNodeCommand>
{
    public ProtocolResult<ProtocolTreeDocument> Handle(ProtocolTreeDocument document, RemoveNodeCommand command)
    {
        var updated = ProtocolTreeFunctions.RemoveNode(document, command.NodeId);
        if (ReferenceEquals(updated, document))
        {
            return ProtocolResult<ProtocolTreeDocument>.Failure("node_not_found", "Requested node could not be removed.");
        }

        return ProtocolResult<ProtocolTreeDocument>.Success(updated);
    }
}

public sealed record UpdateNodeCommand(int NodeId, string Text, int LinkId, string LinkText);

public sealed class UpdateNodeCommandHandler : IProtocolCommandHandler<UpdateNodeCommand>
{
    public ProtocolResult<ProtocolTreeDocument> Handle(ProtocolTreeDocument document, UpdateNodeCommand command)
    {
        var updated = ProtocolTreeFunctions.UpdateNode(
            document,
            command.NodeId,
            command.Text,
            command.LinkId,
            command.LinkText);

        if (ReferenceEquals(updated, document))
        {
            return ProtocolResult<ProtocolTreeDocument>.Failure("node_not_found", "Requested node could not be updated.");
        }

        return ProtocolResult<ProtocolTreeDocument>.Success(updated);
    }
}

public sealed record MoveNodeCommand(int NodeId, int ParentId, int TargetIndex);

public sealed class MoveNodeCommandHandler : IProtocolCommandHandler<MoveNodeCommand>
{
    public ProtocolResult<ProtocolTreeDocument> Handle(ProtocolTreeDocument document, MoveNodeCommand command)
    {
        var updated = ProtocolTreeFunctions.MoveNode(document, command.NodeId, command.ParentId, command.TargetIndex);
        if (ReferenceEquals(updated, document))
        {
            return ProtocolResult<ProtocolTreeDocument>.Failure("invalid_move", "Requested move could not be applied.");
        }

        return ProtocolResult<ProtocolTreeDocument>.Success(updated);
    }
}

public sealed record AddSubTextCommand(int NodeId, string Value);

public sealed class AddSubTextCommandHandler : IProtocolCommandHandler<AddSubTextCommand>
{
    public ProtocolResult<ProtocolTreeDocument> Handle(ProtocolTreeDocument document, AddSubTextCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Value))
        {
            return ProtocolResult<ProtocolTreeDocument>.Failure("invalid_subtext", "SubText value must not be empty.");
        }

        var updated = ProtocolTreeFunctions.AddSubText(document, command.NodeId, command.Value);
        if (ReferenceEquals(updated, document))
        {
            return ProtocolResult<ProtocolTreeDocument>.Failure("node_not_found", "Requested node could not be updated.");
        }

        return ProtocolResult<ProtocolTreeDocument>.Success(updated);
    }
}

public sealed record RemoveSubTextCommand(int NodeId, string Value);

public sealed class RemoveSubTextCommandHandler : IProtocolCommandHandler<RemoveSubTextCommand>
{
    public ProtocolResult<ProtocolTreeDocument> Handle(ProtocolTreeDocument document, RemoveSubTextCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Value))
        {
            return ProtocolResult<ProtocolTreeDocument>.Failure("invalid_subtext", "SubText value must not be empty.");
        }

        var updated = ProtocolTreeFunctions.RemoveSubText(document, command.NodeId, command.Value);
        if (ReferenceEquals(updated, document))
        {
            return ProtocolResult<ProtocolTreeDocument>.Failure("node_not_found", "Requested node could not be updated.");
        }

        return ProtocolResult<ProtocolTreeDocument>.Success(updated);
    }
}
