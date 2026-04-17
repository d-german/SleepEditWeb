using LiteDB;
using SleepEditWeb.Models;

namespace SleepEditWeb.Infrastructure.SleepNote;

public sealed class LiteDbSleepNoteConfigRepository : ISleepNoteConfigRepository, IDisposable
{
    private const string CollectionName = "sleep_note_config";
    private const string ConfigKey = "config";

    private readonly LiteDatabase _database;
    private readonly ILogger<LiteDbSleepNoteConfigRepository> _logger;
    private bool _disposed;

    public LiteDbSleepNoteConfigRepository(ILogger<LiteDbSleepNoteConfigRepository> logger)
    {
        _logger = logger;
        var databasePath = GetDatabasePath();
        EnsureDirectoryExists(databasePath);
        _database = new LiteDatabase(databasePath);
    }

    public SleepNoteConfiguration GetConfiguration()
    {
        var collection = _database.GetCollection<SleepNoteConfigEntity>(CollectionName);
        var entity = collection.FindById(ConfigKey);

        if (entity is null)
        {
            var defaults = CreateDefaults();
            collection.Insert(defaults);
            _logger.LogInformation("Seeded default sleep note configuration.");
            return MapToConfiguration(defaults);
        }

        return MapToConfiguration(entity);
    }

    public void SaveConfiguration(SleepNoteConfiguration config)
    {
        var collection = _database.GetCollection<SleepNoteConfigEntity>(CollectionName);
        var entity = new SleepNoteConfigEntity
        {
            Id = ConfigKey,
            MaskTypes = config.MaskTypes.ToList(),
            MaskSizes = config.MaskSizes.ToList(),
            TechnicianNames = config.TechnicianNames.ToList(),
            PressureValues = config.PressureValues.ToList()
        };
        collection.Upsert(entity);
    }

    public void AddMaskType(string maskType)
    {
        var config = GetConfiguration();
        if (config.MaskTypes.Contains(maskType, StringComparer.OrdinalIgnoreCase))
            return;

        var updated = config with { MaskTypes = [..config.MaskTypes, maskType] };
        SaveConfiguration(updated);
        _logger.LogInformation("Added mask type: {MaskType}", maskType);
    }

    public void RemoveMaskType(string maskType)
    {
        var config = GetConfiguration();
        var updated = config with
        {
            MaskTypes = config.MaskTypes
                .Where(t => !string.Equals(t, maskType, StringComparison.OrdinalIgnoreCase))
                .ToList()
        };
        SaveConfiguration(updated);
        _logger.LogInformation("Removed mask type: {MaskType}", maskType);
    }

    public void AddMaskSize(string maskSize)
    {
        var config = GetConfiguration();
        if (config.MaskSizes.Contains(maskSize, StringComparer.OrdinalIgnoreCase))
            return;

        var updated = config with { MaskSizes = [..config.MaskSizes, maskSize] };
        SaveConfiguration(updated);
        _logger.LogInformation("Added mask size: {MaskSize}", maskSize);
    }

    public void RemoveMaskSize(string maskSize)
    {
        var config = GetConfiguration();
        var updated = config with
        {
            MaskSizes = config.MaskSizes
                .Where(s => !string.Equals(s, maskSize, StringComparison.OrdinalIgnoreCase))
                .ToList()
        };
        SaveConfiguration(updated);
        _logger.LogInformation("Removed mask size: {MaskSize}", maskSize);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _database.Dispose();
            _disposed = true;
        }
    }

    private static SleepNoteConfigEntity CreateDefaults() =>
        new()
        {
            Id = ConfigKey,
            MaskTypes = ["Respironics Comfort Select", "F&P Flexifit HC407"],
            MaskSizes = ["small", "medium", "large"],
            TechnicianNames = [],
            PressureValues = Enumerable.Range(4, 17).ToList()
        };

    private static SleepNoteConfiguration MapToConfiguration(SleepNoteConfigEntity entity) =>
        new()
        {
            MaskTypes = entity.MaskTypes,
            MaskSizes = entity.MaskSizes,
            TechnicianNames = entity.TechnicianNames,
            PressureValues = entity.PressureValues
        };

    private static string GetDatabasePath()
    {
        var basePath = Environment.OSVersion.Platform == PlatformID.Unix
            ? "/app/Data"
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

        return Path.Combine(basePath, "sleepnote-config.db");
    }

    private static void EnsureDirectoryExists(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private sealed class SleepNoteConfigEntity
    {
        [BsonId]
        public string Id { get; set; } = ConfigKey;
        public List<string> MaskTypes { get; set; } = [];
        public List<string> MaskSizes { get; set; } = [];
        public List<string> TechnicianNames { get; set; } = [];
        public List<int> PressureValues { get; set; } = [];
    }
}
