using LiteDB;
using Microsoft.Extensions.Logging;
using SleepEditWeb.Models;
using SleepEditWeb.Services;

namespace SleepEditWeb.Infrastructure.ProtocolPersistence;

public sealed class LiteDbProtocolRepository : IProtocolRepository, IDisposable
{
    private const string VersionsCollection = "protocol_versions";

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

        return entity == null ? null : Map(entity);
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
            .Select(Map)
            .ToList();
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

    private ProtocolVersion Map(ProtocolVersionEntity entity)
    {
        return new ProtocolVersion(
            entity.Id,
            entity.SavedUtc,
            entity.Source,
            entity.Note,
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
}
