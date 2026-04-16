using SleepEditWeb.Infrastructure.ProtocolPersistence;
using SleepEditWeb.Models;

namespace SleepEditWeb.Services;

public interface IProtocolManagementService
{
    SavedProtocolMetadata CreateProtocol(string name);
    IReadOnlyList<SavedProtocolMetadata> ListProtocols();
    ProtocolEditorSnapshot LoadProtocol(Guid protocolId);
    bool DeleteProtocol(Guid protocolId);
    void RenameProtocol(Guid protocolId, string newName);
    void SetDefaultProtocol(Guid protocolId);
    Guid? GetActiveProtocolId();
}

public sealed class ProtocolManagementService(
    IProtocolRepository repository,
    IProtocolEditorSessionStore sessionStore,
    ILogger<ProtocolManagementService> logger)
    : IProtocolManagementService
{
    public SavedProtocolMetadata CreateProtocol(string name)
    {
        var protocolId = Guid.NewGuid();
        var emptyDocument = new ProtocolDocument
        {
            Id = 0,
            LinkId = -1,
            LinkText = string.Empty,
            Text = name,
            Sections = []
        };
        repository.SaveProtocol(protocolId, name, emptyDocument, "CreateProtocol");

        logger.LogInformation("Created protocol {ProtocolId} with name '{Name}'", protocolId, name);

        var protocols = repository.ListProtocols();
        return protocols.First(p => p.ProtocolId == protocolId);
    }

    public IReadOnlyList<SavedProtocolMetadata> ListProtocols() =>
        repository.ListProtocols();

    public ProtocolEditorSnapshot LoadProtocol(Guid protocolId)
    {
        var currentActiveId = sessionStore.GetActiveProtocolId();
        if (currentActiveId.HasValue)
        {
            var currentSnapshot = sessionStore.Load();
            repository.SaveProtocol(
                currentActiveId.Value,
                currentSnapshot.Document.Text,
                currentSnapshot.Document,
                "AutoSaveOnSwitch");
        }

        sessionStore.SetActiveProtocolId(protocolId);

        var version = repository.GetProtocol(protocolId)
            ?? throw new InvalidOperationException($"Protocol {protocolId} not found.");

        var snapshot = new ProtocolEditorSnapshot
        {
            Document = version.Document,
            UndoHistory = [],
            RedoHistory = [],
            UndoDomainHistory = [],
            RedoDomainHistory = [],
            LastUpdatedUtc = DateTimeOffset.UtcNow,
            ActiveProtocolId = protocolId
        };

        sessionStore.Save(snapshot);

        logger.LogInformation("Loaded protocol {ProtocolId} into editor session", protocolId);

        return snapshot;
    }

    public bool DeleteProtocol(Guid protocolId)
    {
        var activeId = sessionStore.GetActiveProtocolId();
        if (activeId.HasValue && activeId.Value == protocolId)
        {
            logger.LogWarning(
                "Cannot delete protocol {ProtocolId} because it is the active protocol", protocolId);
            return false;
        }

        var deleted = repository.DeleteProtocol(protocolId);

        if (deleted)
            logger.LogInformation("Deleted protocol {ProtocolId}", protocolId);
        else
            logger.LogWarning("Failed to delete protocol {ProtocolId} — not found", protocolId);

        return deleted;
    }

    public void RenameProtocol(Guid protocolId, string newName)
    {
        repository.RenameProtocol(protocolId, newName);
        logger.LogInformation("Renamed protocol {ProtocolId} to '{NewName}'", protocolId, newName);
    }

    public void SetDefaultProtocol(Guid protocolId)
    {
        repository.SetDefaultProtocol(protocolId);
        logger.LogInformation("Set protocol {ProtocolId} as default", protocolId);
    }

    public Guid? GetActiveProtocolId() =>
        sessionStore.GetActiveProtocolId();
}
