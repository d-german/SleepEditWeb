namespace SleepEditWeb.Infrastructure.ProtocolPersistence;

public sealed record SavedProtocolMetadata(
    Guid ProtocolId,
    string Name,
    DateTime CreatedUtc,
    DateTime LastModifiedUtc,
    bool IsDefault);
