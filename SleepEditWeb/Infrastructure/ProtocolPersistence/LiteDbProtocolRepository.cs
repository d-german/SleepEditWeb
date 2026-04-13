using LiteDB;
using SleepEditWeb.Models;
using SleepEditWeb.Services;

namespace SleepEditWeb.Infrastructure.ProtocolPersistence;

public sealed class LiteDbProtocolRepository : IProtocolRepository, IDisposable
{
    private const string VersionsCollection = "protocol_versions";
    private const string CurrentCollection = "current_protocol";
    private const string CurrentProtocolKey = "current";

    private readonly LiteDatabase _database;
    private readonly IProtocolXmlService _xmlService;
    private readonly ILogger<LiteDbProtocolRepository> _logger;
    private bool _disposed;

    public LiteDbProtocolRepository(
        IProtocolXmlService xmlService,
        ILogger<LiteDbProtocolRepository> logger)
    {
        _xmlService = xmlService;
        _logger = logger;

        var databasePath = GetDatabasePath();
        EnsureDirectoryExists(databasePath);
        _database = new LiteDatabase(databasePath);
    }

    public ProtocolVersion SaveVersion(ProtocolDocument document, string source, string note)
    {
        ArgumentNullException.ThrowIfNull(document);

        var entity = new ProtocolVersionEntity
        {
            Id = Guid.NewGuid(),
            SavedUtc = DateTime.UtcNow,
            Source = source ?? string.Empty,
            Note = note ?? string.Empty,
            Xml = _xmlService.Serialize(document)
        };

        var collection = _database.GetCollection<ProtocolVersionEntity>(VersionsCollection);
        collection.Insert(entity);
        collection.EnsureIndex(version => version.SavedUtc);

        _logger.LogInformation(
            "Saved protocol version {VersionId} from source {Source} at {SavedUtc}.",
            entity.Id,
            entity.Source,
            entity.SavedUtc);

        return new ProtocolVersion(entity.Id, entity.SavedUtc, entity.Source, entity.Note, document);
    }

    public ProtocolVersion? GetLatestVersion()
    {
        var collection = _database.GetCollection<ProtocolVersionEntity>(VersionsCollection);
        var entity = collection
            .Query()
            .OrderByDescending(version => version.SavedUtc)
            .Limit(1)
            .FirstOrDefault();

        return entity == null ? null : MapVersion(entity);
    }

    public IReadOnlyList<ProtocolVersion> ListVersions(int maxCount = 20)
    {
        var count = Math.Clamp(maxCount, 1, 200);
        var collection = _database.GetCollection<ProtocolVersionEntity>(VersionsCollection);

        return collection
            .Query()
            .OrderByDescending(version => version.SavedUtc)
            .Limit(count)
            .ToList()
            .Select(MapVersion)
            .ToList();
    }

    public ProtocolVersion SaveCurrentProtocol(ProtocolDocument document, string source)
    {
        ArgumentNullException.ThrowIfNull(document);

        var xml = _xmlService.Serialize(document);
        var now = DateTime.UtcNow;

        var currentEntity = new CurrentProtocolEntity
        {
            Id = CurrentProtocolKey,
            SavedUtc = now,
            Source = source ?? string.Empty,
            Xml = xml
        };

        var collection = _database.GetCollection<CurrentProtocolEntity>(CurrentCollection);
        collection.Upsert(currentEntity);

        _logger.LogInformation(
            "Saved current protocol from source {Source} at {SavedUtc}.",
            currentEntity.Source,
            currentEntity.SavedUtc);

        var version = SaveVersion(document, source ?? string.Empty, "SaveCurrentProtocol");

        return new ProtocolVersion(version.VersionId, now, source ?? string.Empty, "SaveCurrentProtocol", document);
    }

    public ProtocolVersion? GetCurrentProtocol()
    {
        var collection = _database.GetCollection<CurrentProtocolEntity>(CurrentCollection);
        var entity = collection.FindById(CurrentProtocolKey);

        if (entity != null)
        {
            return MapCurrent(entity);
        }

        _logger.LogInformation("No current protocol found. Falling back to latest version.");
        return GetLatestVersion();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _database.Dispose();
        _disposed = true;
    }

    private ProtocolVersion MapVersion(ProtocolVersionEntity entity)
    {
        return new ProtocolVersion(
            entity.Id,
            entity.SavedUtc,
            entity.Source,
            entity.Note,
            _xmlService.Deserialize(entity.Xml));
    }

    private ProtocolVersion MapCurrent(CurrentProtocolEntity entity)
    {
        return new ProtocolVersion(
            Guid.Empty,
            entity.SavedUtc,
            entity.Source,
            string.Empty,
            _xmlService.Deserialize(entity.Xml));
    }

    private static string GetDatabasePath()
    {
        var basePath = Environment.OSVersion.Platform == PlatformID.Unix
            ? "/app/Data"
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

        return Path.Combine(basePath, "protocol-versions.db");
    }

    private static void EnsureDirectoryExists(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private sealed class ProtocolVersionEntity
    {
        [BsonId]
        public Guid Id { get; init; }

        public DateTime SavedUtc { get; init; }

        public string Source { get; init; } = string.Empty;

        public string Note { get; init; } = string.Empty;

        public string Xml { get; init; } = string.Empty;
    }

    private sealed class CurrentProtocolEntity
    {
        [BsonId]
        public string Id { get; init; } = CurrentProtocolKey;

        public DateTime SavedUtc { get; init; }

        public string Source { get; init; } = string.Empty;

        public string Xml { get; init; } = string.Empty;
    }
}
