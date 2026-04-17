using LiteDB;
using SleepEditWeb.Models;
using SleepEditWeb.Services;

namespace SleepEditWeb.Infrastructure.ProtocolPersistence;

public sealed class LiteDbProtocolRepository : IProtocolRepository, IDisposable
{
    private const string VersionsCollection = "protocol_versions";
    private const string CurrentCollection = "current_protocol";
    private const string CurrentProtocolKey = "current";
    private const string SaveCurrentProtocolNote = "SaveCurrentProtocol";
    private const string SavedProtocolsCollection = "saved_protocols";

    private readonly LiteDatabase _database;
    private readonly IProtocolXmlService _xmlService;
    private readonly ILogger<LiteDbProtocolRepository> _logger;
    private readonly object _migrationLock = new();
    private volatile bool _migrationChecked;

    public LiteDbProtocolRepository(
        LiteDatabase database,
        IProtocolXmlService xmlService,
        ILogger<LiteDbProtocolRepository> logger)
    {
        _xmlService = xmlService;
        _logger = logger;
        _database = database;
    }

    public ProtocolVersion SaveVersion(ProtocolDocument document, string source, string note)
    {
        ArgumentNullException.ThrowIfNull(document);

        var entity = CreateVersionEntity(
            _xmlService.Serialize(document),
            source ?? string.Empty,
            note ?? string.Empty,
            DateTime.UtcNow);
        var version = InsertVersionEntity(entity, document);

        _logger.LogInformation(
            "Saved protocol version {VersionId} from source {Source} at {SavedUtc}.",
            entity.Id,
            entity.Source,
            entity.SavedUtc);

        return version;
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
        var normalizedSource = source ?? string.Empty;

        var currentEntity = new CurrentProtocolEntity
        {
            Id = CurrentProtocolKey,
            SavedUtc = now,
            Source = normalizedSource,
            Xml = xml
        };

        var versionEntity = CreateVersionEntity(xml, normalizedSource, SaveCurrentProtocolNote, now);

        _database.BeginTrans();
        try
        {
            var currentCollection = _database.GetCollection<CurrentProtocolEntity>(CurrentCollection);
            currentCollection.Upsert(currentEntity);

            var version = InsertVersionEntity(versionEntity, document);
            _database.Commit();

            _logger.LogInformation(
                "Saved current protocol from source {Source} at {SavedUtc} with version {VersionId}.",
                currentEntity.Source,
                currentEntity.SavedUtc,
                version.VersionId);

            return version;
        }
        catch
        {
            _database.Rollback();
            throw;
        }
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

    public ProtocolVersion SaveProtocol(Guid protocolId, string name, ProtocolDocument document, string source)
    {
        ArgumentNullException.ThrowIfNull(document);

        var xml = _xmlService.Serialize(document);
        var now = DateTime.UtcNow;
        var normalizedSource = source ?? string.Empty;

        _database.BeginTrans();
        try
        {
            var collection = _database.GetCollection<SavedProtocolEntity>(SavedProtocolsCollection);
            var existing = collection.FindById(protocolId);

            if (existing != null)
            {
                existing.Xml = xml;
                existing.LastModifiedUtc = now;
                if (!string.Equals(existing.Name, name, StringComparison.Ordinal))
                {
                    existing.Name = name;
                }

                collection.Update(existing);
            }
            else
            {
                var entity = new SavedProtocolEntity
                {
                    ProtocolId = protocolId,
                    Name = name ?? string.Empty,
                    CreatedUtc = now,
                    LastModifiedUtc = now,
                    IsDefault = false,
                    Xml = xml
                };
                collection.Insert(entity);
            }

            var versionEntity = new ProtocolVersionEntity
            {
                Id = Guid.NewGuid(),
                SavedUtc = now,
                Source = normalizedSource,
                Note = $"SaveProtocol:{name}",
                Xml = xml,
                ProtocolId = protocolId
            };
            var version = InsertVersionEntity(versionEntity, document);

            _database.Commit();

            _logger.LogInformation(
                "Saved protocol {ProtocolId} ({Name}) from source {Source} at {SavedUtc}.",
                protocolId,
                name,
                normalizedSource,
                now);

            return version;
        }
        catch
        {
            _database.Rollback();
            throw;
        }
    }

    public ProtocolVersion? GetProtocol(Guid protocolId)
    {
        var collection = _database.GetCollection<SavedProtocolEntity>(SavedProtocolsCollection);
        var entity = collection.FindById(protocolId);

        if (entity == null)
        {
            return null;
        }

        return new ProtocolVersion(
            protocolId,
            entity.LastModifiedUtc,
            string.Empty,
            string.Empty,
            _xmlService.Deserialize(entity.Xml));
    }

    public IReadOnlyList<SavedProtocolMetadata> ListProtocols()
    {
        EnsureMigration();

        var collection = _database.GetCollection<SavedProtocolEntity>(SavedProtocolsCollection);

        return collection
            .Query()
            .OrderBy(entity => entity.CreatedUtc)
            .ToList()
            .Select(static entity => new SavedProtocolMetadata(
                entity.ProtocolId,
                entity.Name,
                entity.CreatedUtc,
                entity.LastModifiedUtc,
                entity.IsDefault))
            .ToList();
    }

    public bool DeleteProtocol(Guid protocolId)
    {
        var collection = _database.GetCollection<SavedProtocolEntity>(SavedProtocolsCollection);
        var entity = collection.FindById(protocolId);

        if (entity == null || entity.IsDefault)
        {
            return false;
        }

        return collection.Delete(protocolId);
    }

    public void RenameProtocol(Guid protocolId, string newName)
    {
        var collection = _database.GetCollection<SavedProtocolEntity>(SavedProtocolsCollection);
        var entity = collection.FindById(protocolId);

        if (entity == null)
        {
            throw new InvalidOperationException($"Protocol {protocolId} not found.");
        }

        entity.Name = newName;
        entity.LastModifiedUtc = DateTime.UtcNow;
        collection.Update(entity);
    }

    public void SetDefaultProtocol(Guid protocolId)
    {
        _database.BeginTrans();
        try
        {
            var collection = _database.GetCollection<SavedProtocolEntity>(SavedProtocolsCollection);

            var currentDefaults = collection.Find(entity => entity.IsDefault).ToList();
            foreach (var existing in currentDefaults)
            {
                existing.IsDefault = false;
                collection.Update(existing);
            }

            var target = collection.FindById(protocolId);
            if (target == null)
            {
                _database.Rollback();
                throw new InvalidOperationException($"Protocol {protocolId} not found.");
            }

            target.IsDefault = true;
            collection.Update(target);

            _database.Commit();
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch
        {
            _database.Rollback();
            throw;
        }
    }

    public ProtocolVersion? GetDefaultProtocol()
    {
        EnsureMigration();

        var collection = _database.GetCollection<SavedProtocolEntity>(SavedProtocolsCollection);
        var entity = collection.FindOne(e => e.IsDefault);

        if (entity == null)
        {
            return null;
        }

        return new ProtocolVersion(
            entity.ProtocolId,
            entity.LastModifiedUtc,
            string.Empty,
            string.Empty,
            _xmlService.Deserialize(entity.Xml));
    }

    public void Dispose()
    {
        // LiteDatabase lifecycle is managed by DI container
    }

    private void EnsureMigration()
    {
        if (_migrationChecked)
        {
            return;
        }

        lock (_migrationLock)
        {
            if (_migrationChecked)
            {
                return;
            }

            var savedCollection = _database.GetCollection<SavedProtocolEntity>(SavedProtocolsCollection);
            if (savedCollection.Count() > 0)
            {
                _migrationChecked = true;
                return;
            }

            var currentCollection = _database.GetCollection<CurrentProtocolEntity>(CurrentCollection);
            var currentEntity = currentCollection.FindById(CurrentProtocolKey);

            if (currentEntity != null)
            {
                var document = _xmlService.Deserialize(currentEntity.Xml);
                var migrated = new SavedProtocolEntity
                {
                    ProtocolId = Guid.NewGuid(),
                    Name = document.Text,
                    CreatedUtc = currentEntity.SavedUtc,
                    LastModifiedUtc = currentEntity.SavedUtc,
                    IsDefault = true,
                    Xml = currentEntity.Xml
                };
                savedCollection.Insert(migrated);

                _logger.LogInformation(
                    "Migrated current protocol to saved_protocols as {ProtocolId} with name {Name}.",
                    migrated.ProtocolId,
                    migrated.Name);
            }

            _migrationChecked = true;
        }
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

    private static ProtocolVersionEntity CreateVersionEntity(
        string xml,
        string source,
        string note,
        DateTime savedUtc)
    {
        return new ProtocolVersionEntity
        {
            Id = Guid.NewGuid(),
            SavedUtc = savedUtc,
            Source = source,
            Note = note,
            Xml = xml
        };
    }

    private ProtocolVersion InsertVersionEntity(ProtocolVersionEntity entity, ProtocolDocument document)
    {
        var collection = _database.GetCollection<ProtocolVersionEntity>(VersionsCollection);
        collection.Insert(entity);
        collection.EnsureIndex(version => version.SavedUtc);

        return new ProtocolVersion(entity.Id, entity.SavedUtc, entity.Source, entity.Note, document);
    }

    

    

    private sealed class ProtocolVersionEntity
    {
        [BsonId]
        public Guid Id { get; init; }

        public DateTime SavedUtc { get; init; }

        public string Source { get; init; } = string.Empty;

        public string Note { get; init; } = string.Empty;

        public string Xml { get; init; } = string.Empty;

        public Guid? ProtocolId { get; init; }
    }

    private sealed class SavedProtocolEntity
    {
        [BsonId]
        public Guid ProtocolId { get; init; }

        public string Name { get; set; } = string.Empty;

        public DateTime CreatedUtc { get; init; }

        public DateTime LastModifiedUtc { get; set; }

        public bool IsDefault { get; set; }

        public string Xml { get; set; } = string.Empty;
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
