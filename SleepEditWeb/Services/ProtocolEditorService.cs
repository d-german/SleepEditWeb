using Microsoft.Extensions.Logging;
using SleepEditWeb.Application.Protocol;
using SleepEditWeb.Application.Protocol.Commands;
using SleepEditWeb.Models;
using SleepEditWeb.Protocol.Domain;

namespace SleepEditWeb.Services;

public interface IProtocolEditorService
{
    ProtocolEditorSnapshot Load();

    ProtocolEditorSnapshot AddSection(string text);

    ProtocolEditorSnapshot AddChild(int parentId, string text);

    ProtocolEditorSnapshot RemoveNode(int nodeId);

    ProtocolEditorSnapshot UpdateNode(int nodeId, string text, int linkId, string linkText);

    ProtocolEditorSnapshot MoveNode(int nodeId, int parentId, int targetIndex);

    ProtocolEditorSnapshot AddSubText(int nodeId, string value);

    ProtocolEditorSnapshot RemoveSubText(int nodeId, string value);

    ProtocolEditorSnapshot Undo();

    ProtocolEditorSnapshot Redo();

    ProtocolEditorSnapshot Reset();

    ProtocolEditorSnapshot ImportXml(string xml);

    string ExportXml();
}

public sealed class ProtocolEditorService : IProtocolEditorService
{
    private const int MaxUndoDepth = 100;

    private readonly IProtocolEditorSessionStore _sessionStore;
    private readonly IProtocolXmlService _xmlService;
    private readonly IProtocolCommandHandler<AddSectionCommand> _addSectionHandler;
    private readonly IProtocolCommandHandler<AddChildCommand> _addChildHandler;
    private readonly IProtocolCommandHandler<RemoveNodeCommand> _removeNodeHandler;
    private readonly IProtocolCommandHandler<UpdateNodeCommand> _updateNodeHandler;
    private readonly IProtocolCommandHandler<MoveNodeCommand> _moveNodeHandler;
    private readonly IProtocolCommandHandler<AddSubTextCommand> _addSubTextHandler;
    private readonly IProtocolCommandHandler<RemoveSubTextCommand> _removeSubTextHandler;
    private readonly ILogger<ProtocolEditorService> _logger;

    public ProtocolEditorService(
        IProtocolEditorSessionStore sessionStore,
        IProtocolXmlService xmlService,
        IProtocolCommandHandler<AddSectionCommand> addSectionHandler,
        IProtocolCommandHandler<AddChildCommand> addChildHandler,
        IProtocolCommandHandler<RemoveNodeCommand> removeNodeHandler,
        IProtocolCommandHandler<UpdateNodeCommand> updateNodeHandler,
        IProtocolCommandHandler<MoveNodeCommand> moveNodeHandler,
        IProtocolCommandHandler<AddSubTextCommand> addSubTextHandler,
        IProtocolCommandHandler<RemoveSubTextCommand> removeSubTextHandler,
        ILogger<ProtocolEditorService> logger)
    {
        _sessionStore = sessionStore;
        _xmlService = xmlService;
        _addSectionHandler = addSectionHandler;
        _addChildHandler = addChildHandler;
        _removeNodeHandler = removeNodeHandler;
        _updateNodeHandler = updateNodeHandler;
        _moveNodeHandler = moveNodeHandler;
        _addSubTextHandler = addSubTextHandler;
        _removeSubTextHandler = removeSubTextHandler;
        _logger = logger;
    }

    public ProtocolEditorSnapshot Load()
    {
        _logger.LogDebug("ProtocolEditorService.Load requested.");
        return _sessionStore.Load();
    }

    public ProtocolEditorSnapshot AddSection(string text)
    {
        _logger.LogInformation("ProtocolEditorService.AddSection requested. TextLength: {Length}", text.Length);
        return ApplyCommandMutation(new AddSectionCommand(text), _addSectionHandler, nameof(AddSection));
    }

    public ProtocolEditorSnapshot AddChild(int parentId, string text)
    {
        _logger.LogInformation("ProtocolEditorService.AddChild requested. ParentId: {ParentId}, TextLength: {Length}", parentId, text.Length);
        return ApplyCommandMutation(new AddChildCommand(parentId, text), _addChildHandler, nameof(AddChild));
    }

    public ProtocolEditorSnapshot RemoveNode(int nodeId)
    {
        _logger.LogInformation("ProtocolEditorService.RemoveNode requested. NodeId: {NodeId}", nodeId);
        return ApplyCommandMutation(new RemoveNodeCommand(nodeId), _removeNodeHandler, nameof(RemoveNode));
    }

    public ProtocolEditorSnapshot UpdateNode(int nodeId, string text, int linkId, string linkText)
    {
        _logger.LogInformation("ProtocolEditorService.UpdateNode requested. NodeId: {NodeId}, LinkId: {LinkId}, TextLength: {Length}", nodeId, linkId, text.Length);
        return ApplyCommandMutation(new UpdateNodeCommand(nodeId, text, linkId, linkText), _updateNodeHandler, nameof(UpdateNode));
    }

    public ProtocolEditorSnapshot MoveNode(int nodeId, int parentId, int targetIndex)
    {
        _logger.LogInformation(
            "ProtocolEditorService.MoveNode requested. NodeId: {NodeId}, ParentId: {ParentId}, TargetIndex: {TargetIndex}",
            nodeId,
            parentId,
            targetIndex);

        return ApplyCommandMutation(new MoveNodeCommand(nodeId, parentId, targetIndex), _moveNodeHandler, nameof(MoveNode));
    }

    public ProtocolEditorSnapshot AddSubText(int nodeId, string value)
    {
        _logger.LogInformation("ProtocolEditorService.AddSubText requested. NodeId: {NodeId}, ValueLength: {Length}", nodeId, value.Length);
        return ApplyCommandMutation(new AddSubTextCommand(nodeId, value), _addSubTextHandler, nameof(AddSubText));
    }

    public ProtocolEditorSnapshot RemoveSubText(int nodeId, string value)
    {
        _logger.LogInformation("ProtocolEditorService.RemoveSubText requested. NodeId: {NodeId}, ValueLength: {Length}", nodeId, value.Length);
        return ApplyCommandMutation(new RemoveSubTextCommand(nodeId, value), _removeSubTextHandler, nameof(RemoveSubText));
    }

    public ProtocolEditorSnapshot Undo()
    {
        _logger.LogInformation("ProtocolEditorService.Undo requested.");
        var snapshot = _sessionStore.Load();

        var undoDomainHistory = ResolveDomainHistory(snapshot.UndoDomainHistory, snapshot.UndoHistory);
        if (undoDomainHistory.Count == 0)
        {
            _logger.LogDebug("ProtocolEditorService.Undo skipped because undo history was empty.");
            return snapshot;
        }

        var current = ProtocolTreeMapper.ToDomain(snapshot.Document);
        var restored = undoDomainHistory[^1];
        undoDomainHistory.RemoveAt(undoDomainHistory.Count - 1);

        var redoDomainHistory = ResolveDomainHistory(snapshot.RedoDomainHistory, snapshot.RedoHistory);
        redoDomainHistory.Add(current);

        var next = BuildSnapshot(restored, undoDomainHistory, redoDomainHistory, DateTimeOffset.UtcNow);
        _sessionStore.Save(next);
        _logger.LogInformation("ProtocolEditorService.Undo completed.");
        return next;
    }

    public ProtocolEditorSnapshot Redo()
    {
        _logger.LogInformation("ProtocolEditorService.Redo requested.");
        var snapshot = _sessionStore.Load();

        var redoDomainHistory = ResolveDomainHistory(snapshot.RedoDomainHistory, snapshot.RedoHistory);
        if (redoDomainHistory.Count == 0)
        {
            _logger.LogDebug("ProtocolEditorService.Redo skipped because redo history was empty.");
            return snapshot;
        }

        var current = ProtocolTreeMapper.ToDomain(snapshot.Document);
        var restored = redoDomainHistory[^1];
        redoDomainHistory.RemoveAt(redoDomainHistory.Count - 1);

        var undoDomainHistory = ResolveDomainHistory(snapshot.UndoDomainHistory, snapshot.UndoHistory);
        undoDomainHistory.Add(current);

        var next = BuildSnapshot(restored, undoDomainHistory, redoDomainHistory, DateTimeOffset.UtcNow);
        _sessionStore.Save(next);
        _logger.LogInformation("ProtocolEditorService.Redo completed.");
        return next;
    }

    public ProtocolEditorSnapshot Reset()
    {
        _logger.LogInformation("ProtocolEditorService.Reset requested.");
        _sessionStore.Reset();
        _logger.LogInformation("ProtocolEditorService.Reset completed.");
        return _sessionStore.Load();
    }

    public ProtocolEditorSnapshot ImportXml(string xml)
    {
        _logger.LogInformation("ProtocolEditorService.ImportXml requested. XmlLength: {Length}", xml.Length);
        var document = _xmlService.Deserialize(xml);
        var next = new ProtocolEditorSnapshot
        {
            Document = document,
            UndoHistory = [],
            RedoHistory = [],
            UndoDomainHistory = [],
            RedoDomainHistory = [],
            LastUpdatedUtc = DateTimeOffset.UtcNow
        };

        _sessionStore.Save(next);
        _logger.LogInformation("ProtocolEditorService.ImportXml completed successfully.");
        return next;
    }

    public string ExportXml()
    {
        _logger.LogInformation("ProtocolEditorService.ExportXml requested.");
        var snapshot = _sessionStore.Load();
        var xml = _xmlService.Serialize(snapshot.Document);
        _logger.LogInformation("ProtocolEditorService.ExportXml completed. XmlLength: {Length}", xml.Length);
        return xml;
    }

    private ProtocolEditorSnapshot ApplyCommandMutation<TCommand>(
        TCommand command,
        IProtocolCommandHandler<TCommand> handler,
        string operationName)
    {
        var snapshot = _sessionStore.Load();
        var current = ProtocolTreeMapper.ToDomain(snapshot.Document);

        var updateResult = handler.Handle(current, command);
        if (updateResult.IsFailure)
        {
            var userMessage = ProtocolResultMapping.ToUserSafeMessage(updateResult);
            _logger.LogDebug(
                "ProtocolEditorService.{Operation} skipped. Code: {Code}, Message: {Message}, UserMessage: {UserMessage}",
                operationName,
                updateResult.ErrorCode,
                updateResult.ErrorMessage,
                userMessage);
            return snapshot;
        }

        var undoDomainHistory = ResolveDomainHistory(snapshot.UndoDomainHistory, snapshot.UndoHistory);
        undoDomainHistory.Add(current);

        if (undoDomainHistory.Count > MaxUndoDepth)
        {
            undoDomainHistory.RemoveAt(0);
        }

        var next = BuildSnapshot(updateResult.Value, undoDomainHistory, [], DateTimeOffset.UtcNow);
        _sessionStore.Save(next);

        _logger.LogDebug(
            "ProtocolEditorService mutation saved. Operation: {Operation}, UndoCount: {UndoCount}, RedoCount: {RedoCount}",
            operationName,
            next.UndoHistory.Count,
            next.RedoHistory.Count);

        return next;
    }

    private static ProtocolEditorSnapshot BuildSnapshot(
        ProtocolTreeDocument current,
        List<ProtocolTreeDocument> undoDomainHistory,
        List<ProtocolTreeDocument> redoDomainHistory,
        DateTimeOffset timestamp)
    {
        return new ProtocolEditorSnapshot
        {
            Document = ProtocolTreeMapper.ToMutable(current),
            UndoDomainHistory = undoDomainHistory,
            RedoDomainHistory = redoDomainHistory,
            UndoHistory = CreateLegacyHistoryPlaceholders(undoDomainHistory.Count),
            RedoHistory = CreateLegacyHistoryPlaceholders(redoDomainHistory.Count),
            LastUpdatedUtc = timestamp
        };
    }

    private static List<ProtocolTreeDocument> ResolveDomainHistory(
        IReadOnlyList<ProtocolTreeDocument> domainHistory,
        IReadOnlyList<ProtocolDocument> legacyHistory)
    {
        if (domainHistory.Count > 0)
        {
            return domainHistory.ToList();
        }

        return legacyHistory
            .Select(ProtocolTreeMapper.ToDomain)
            .ToList();
    }

    private static List<ProtocolDocument> CreateLegacyHistoryPlaceholders(int count)
    {
        if (count <= 0)
        {
            return [];
        }

        return Enumerable
            .Range(0, count)
            .Select(_ => new ProtocolDocument())
            .ToList();
    }
}
