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
}
