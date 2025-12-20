using System.Reflection;
using LiteDB;
using SleepEditWeb.Models;
using CSharpFunctionalExtensions;

namespace SleepEditWeb.Data;

/// <summary>
/// LiteDB implementation of IMedicationRepository.
/// Singleton service - database remains open for application lifetime.
/// </summary>
public sealed class LiteDbMedicationRepository : IMedicationRepository, IDisposable
{
    private const int CurrentSeedVersion = 1;
    private const string MedicationsCollection = "medications";
    private const string MetadataCollection = "metadata";

    private readonly LiteDatabase _database;
    private readonly string _databasePath;
    private string _loadedFrom = "not initialized";
    private bool _disposed;

    public LiteDbMedicationRepository()
    {
        _databasePath = GetDatabasePath();
        EnsureDirectoryExists(_databasePath);
        
        _database = new LiteDatabase(_databasePath);
        InitializeDatabase();
    }

    /// <summary>
    /// Gets the database file path based on environment.
    /// Linux (Koyeb): /app/Data/medications.db
    /// Windows (local dev): ./Data/medications.db
    /// </summary>
    private static string GetDatabasePath()
    {
        var isLinux = Environment.OSVersion.Platform == PlatformID.Unix;
        var basePath = isLinux 
            ? "/app/Data" 
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        
        return Path.Combine(basePath, "medications.db");
    }

    /// <summary>
    /// Ensures the directory for the database file exists.
    /// </summary>
    private static void EnsureDirectoryExists(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            Console.WriteLine($"[LiteDB] Created directory: {directory}");
        }
    }

    /// <summary>
    /// Initializes database on first run or after seed version update.
    /// </summary>
    private void InitializeDatabase()
    {
        var medications = _database.GetCollection<Medication>(MedicationsCollection);
        var metadata = _database.GetCollection<DatabaseMetadata>(MetadataCollection);

        // Check if database needs seeding
        var dbMetadata = metadata.FindById(1);
        
        if (dbMetadata == null || medications.Count() == 0)
        {
            // First run - seed from embedded resource
            Console.WriteLine("[LiteDB] First run - seeding database from embedded resource");
            SeedDatabase(medications, metadata);
        }
        else if (dbMetadata.SeedVersion < CurrentSeedVersion)
        {
            // Seed version updated - merge new medications
            Console.WriteLine($"[LiteDB] Seed version update detected ({dbMetadata.SeedVersion} → {CurrentSeedVersion})");
            MergeSeedData(medications, metadata);
        }
        else
        {
            Console.WriteLine($"[LiteDB] Database ready - {medications.Count()} medications loaded");
            _loadedFrom = $"LiteDB ({_databasePath})";
        }

        // Note: Cannot create index on Name field due to LiteDB 1023 byte key limit
        // Some medication names exceed this limit. Queries will still work without index.
    }

    /// <summary>
    /// Seeds the database from the embedded medlist.txt resource.
    /// </summary>
    private void SeedDatabase(ILiteCollection<Medication> medications, ILiteCollection<DatabaseMetadata> metadata)
    {
        var seedMedications = LoadMedicationsFromEmbeddedResource();
        
        if (seedMedications.Count == 0)
        {
            Console.WriteLine("[LiteDB] WARNING: No seed data found in embedded resource!");
            _loadedFrom = "empty - no seed data";
            return;
        }

        // Clear existing data and insert seed
        medications.DeleteAll();
        var now = DateTime.UtcNow;
        
        var medicationEntities = seedMedications.Select((name, index) => new Medication
        {
            Id = index + 1,
            Name = name,
            IsSystemMed = true,
            CreatedAt = now
        }).ToList();

        medications.InsertBulk(medicationEntities);
        Console.WriteLine($"[LiteDB] Seeded {medicationEntities.Count} medications");

        // Update metadata
        var newMetadata = new DatabaseMetadata
        {
            Id = 1,
            SeedVersion = CurrentSeedVersion,
            LastSeeded = now
        };
        metadata.Upsert(newMetadata);

        _loadedFrom = "seeded from embedded resource";
    }

    /// <summary>
    /// Merges new seed data while preserving user-added medications.
    /// </summary>
    private void MergeSeedData(ILiteCollection<Medication> medications, ILiteCollection<DatabaseMetadata> metadata)
    {
        var seedMedications = LoadMedicationsFromEmbeddedResource();
        var existingNames = new HashSet<string>(
            medications.FindAll().Select(m => m.Name),
            StringComparer.OrdinalIgnoreCase);

        var now = DateTime.UtcNow;
        var newMedications = seedMedications
            .Where(name => !existingNames.Contains(name))
            .Select(name => new Medication
            {
                Name = name,
                IsSystemMed = true,
                CreatedAt = now
            })
            .ToList();

        if (newMedications.Count > 0)
        {
            medications.InsertBulk(newMedications);
            Console.WriteLine($"[LiteDB] Merged {newMedications.Count} new medications from seed");
        }

        // Update metadata
        var dbMetadata = metadata.FindById(1) ?? new DatabaseMetadata { Id = 1 };
        dbMetadata.SeedVersion = CurrentSeedVersion;
        dbMetadata.LastSeeded = now;
        metadata.Upsert(dbMetadata);

        _loadedFrom = $"merged (added {newMedications.Count} from seed v{CurrentSeedVersion})";
    }

    /// <summary>
    /// Loads medication names from the embedded medlist.txt resource.
    /// </summary>
    private static List<string> LoadMedicationsFromEmbeddedResource()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var resourceStream = assembly.GetManifestResourceStream("medlist.txt");
        
        if (resourceStream == null)
        {
            Console.WriteLine("[LiteDB] ERROR: Could not find embedded resource 'medlist.txt'");
            return [];
        }

        using var reader = new StreamReader(resourceStream);
        var content = reader.ReadToEnd();
        var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        
        Console.WriteLine($"[LiteDB] Loaded {lines.Length} medication names from embedded resource");
        return lines.ToList();
    }

    /// <summary>
    /// Gets the database path for diagnostics.
    /// </summary>
    public string DatabasePath => _databasePath;

    /// <summary>
    /// Gets the load source info for diagnostics.
    /// </summary>
    public string LoadedFrom => _loadedFrom;

    #region IMedicationRepository Implementation - Query Operations

    public IEnumerable<string> GetAllMedicationNames()
    {
        var medications = _database.GetCollection<Medication>(MedicationsCollection);
        return [..medications.FindAll()
            .OrderBy(m => m.Name)
            .Select(m => m.Name)];
    }

    public IEnumerable<string> SearchMedications(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var medications = _database.GetCollection<Medication>(MedicationsCollection);
        var lowerQuery = query.ToLowerInvariant();
        
        return [..medications.Find(m => m.Name.ToLower().StartsWith(lowerQuery))
            .OrderBy(m => m.Name)
            .Select(m => m.Name)];
    }

    public bool MedicationExists(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var medications = _database.GetCollection<Medication>(MedicationsCollection);
        return medications.Exists(m => m.Name.ToLower() == name.ToLower());
    }

    #endregion

    #region IMedicationRepository Implementation - CRUD Operations

    public Result AddUserMedication(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure("Medication name cannot be empty.");

        if (MedicationExists(name))
            return Result.Failure($"Medication '{name}' already exists in the database.");

        var medications = _database.GetCollection<Medication>(MedicationsCollection);
        var medication = new Medication
        {
            Name = name.Trim(),
            IsSystemMed = false,
            CreatedAt = DateTime.UtcNow
        };

        medications.Insert(medication);
        Console.WriteLine($"[LiteDB] Added user medication: {name}");
        return Result.Success();
    }

    public Result RemoveUserMedication(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure("Medication name cannot be empty.");

        var medications = _database.GetCollection<Medication>(MedicationsCollection);
        var medication = medications.FindOne(m => m.Name.ToLower() == name.ToLower());

        if (medication == null)
            return Result.Failure($"Medication '{name}' not found.");

        if (medication.IsSystemMed)
        {
            Console.WriteLine($"[LiteDB] Cannot remove system medication: {name}");
            return Result.Failure($"Medication '{name}' is a system medication and cannot be removed.");
        }

        medications.Delete(medication.Id);
        Console.WriteLine($"[LiteDB] Removed user medication: {name}");
        return Result.Success();
    }

    #endregion

    #region IMedicationRepository Implementation - Admin Operations

    public MedicationBackup ExportAll()
    {
        var medications = _database.GetCollection<Medication>(MedicationsCollection);
        var metadata = _database.GetCollection<DatabaseMetadata>(MetadataCollection);
        var dbMetadata = metadata.FindById(1);

        var allMedications = medications.FindAll().ToList();

        return new MedicationBackup
        {
            ExportDate = DateTime.UtcNow,
            SeedVersion = dbMetadata?.SeedVersion ?? CurrentSeedVersion,
            TotalCount = allMedications.Count,
            Medications = allMedications
        };
    }

    public Result ImportReplace(List<Medication> medications)
    {
        var medsCollection = _database.GetCollection<Medication>(MedicationsCollection);
        
        _database.BeginTrans();
        try
        {
            medsCollection.DeleteAll();
            medsCollection.InsertBulk(medications);
            _database.Commit();
            Console.WriteLine($"[LiteDB] Import replace: {medications.Count} medications");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _database.Rollback();
            return Result.Failure($"Import replace failed: {ex.Message}");
        }
    }

    public Result ImportMerge(List<Medication> medications)
    {
        return Result.Try(() => 
        {
            var medsCollection = _database.GetCollection<Medication>(MedicationsCollection);
            var existingNames = new HashSet<string>(
                medsCollection.FindAll().Select(m => m.Name),
                StringComparer.OrdinalIgnoreCase);

            var newMedications = medications
                .Where(m => !existingNames.Contains(m.Name))
                .ToList();

            if (newMedications.Count > 0)
            {
                medsCollection.InsertBulk(newMedications);
                Console.WriteLine($"[LiteDB] Import merge: added {newMedications.Count} new medications");
            }
            return Result.Success();
        }, ex => $"Import merge failed: {ex.Message}").Bind(r => r);
    }

    public MedicationStats GetStats()
    {
        var medications = _database.GetCollection<Medication>(MedicationsCollection);
        var metadata = _database.GetCollection<DatabaseMetadata>(MetadataCollection);
        var dbMetadata = metadata.FindById(1);

        var allMeds = medications.FindAll().ToList();

        return new MedicationStats
        {
            TotalCount = allMeds.Count,
            SystemMedCount = allMeds.Count(m => m.IsSystemMed),
            UserMedCount = allMeds.Count(m => !m.IsSystemMed),
            SeedVersion = dbMetadata?.SeedVersion ?? 0,
            LastSeeded = dbMetadata?.LastSeeded,
            LoadedFrom = _loadedFrom
        };
    }

    #endregion

    #region IMedicationRepository Implementation - Maintenance Operations

    public Result Reseed()
    {
        return Result.Try(() => 
        {
            Console.WriteLine("[LiteDB] Reseed requested - clearing and reseeding database");
            var medications = _database.GetCollection<Medication>(MedicationsCollection);
            var metadata = _database.GetCollection<DatabaseMetadata>(MetadataCollection);

            SeedDatabase(medications, metadata);
            return Result.Success();
        }, ex => $"Reseed failed: {ex.Message}").Bind(r => r);
    }

    public Result ClearUserMedications()
    {
        return Result.Try(() => 
        {
            var medications = _database.GetCollection<Medication>(MedicationsCollection);
            var deleted = medications.DeleteMany(m => !m.IsSystemMed);
            Console.WriteLine($"[LiteDB] Cleared {deleted} user-added medications");
            return Result.Success();
        }, ex => $"Clear failed: {ex.Message}").Bind(r => r);
    }

    #endregion

    #region IDisposable Implementation

    public void Dispose()
    {
        if (_disposed) return;
        
        _database.Dispose();
        _disposed = true;
        Console.WriteLine("[LiteDB] Database connection closed");
    }

    #endregion
}
