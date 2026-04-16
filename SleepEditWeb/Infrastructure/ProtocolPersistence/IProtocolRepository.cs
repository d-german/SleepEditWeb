using SleepEditWeb.Models;

namespace SleepEditWeb.Infrastructure.ProtocolPersistence;

public sealed record ProtocolVersion(
    Guid VersionId,
    DateTime SavedUtc,
    string Source,
    string Note,
    ProtocolDocument Document);

public interface IProtocolRepository
{
    ProtocolVersion SaveVersion(ProtocolDocument document, string source, string note);

    ProtocolVersion? GetLatestVersion();

    IReadOnlyList<ProtocolVersion> ListVersions(int maxCount = 20);

    ProtocolVersion SaveCurrentProtocol(ProtocolDocument document, string source);

    ProtocolVersion? GetCurrentProtocol();

    ProtocolVersion SaveProtocol(Guid protocolId, string name, ProtocolDocument document, string source);

    ProtocolVersion? GetProtocol(Guid protocolId);

    IReadOnlyList<SavedProtocolMetadata> ListProtocols();

    bool DeleteProtocol(Guid protocolId);

    void RenameProtocol(Guid protocolId, string newName);

    void SetDefaultProtocol(Guid protocolId);

    ProtocolVersion? GetDefaultProtocol();
}
