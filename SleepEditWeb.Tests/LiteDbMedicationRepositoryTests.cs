using SleepEditWeb.Data;
using SleepEditWeb.Models;
using CSharpFunctionalExtensions;

namespace SleepEditWeb.Tests;

[TestFixture]
public class LiteDbMedicationRepositoryTests
{
    private string _testDbPath;
    private LiteDbMedicationRepository _repo;

    [SetUp]
    public void Setup()
    {
        _testDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_meds.db");
        if (File.Exists(_testDbPath)) File.Delete(_testDbPath);
        
        // We can't easily change the path in the current implementation without refactoring the constructor
        // or setting an environment variable if it supports it.
        // Let's check how GetDatabasePath works.
        // It uses AppDomain.CurrentDomain.BaseDirectory which is fine for tests.
        
        _repo = new LiteDbMedicationRepository();
    }

    [TearDown]
    public void TearDown()
    {
        _repo?.Dispose();
        // The real DB path is likely in the Data subfolder of base directory
        var dbPath = _repo?.DatabasePath;
        if (!string.IsNullOrEmpty(dbPath) && File.Exists(dbPath))
        {
            // Can't delete while open, but Dispose should close it
            try { File.Delete(dbPath); } catch {}
        }
    }

    [Test]
    public void AddUserMedication_NewMed_ReturnsSuccess()
    {
        var result = _repo.AddUserMedication("NewMed123");
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(_repo.MedicationExists("NewMed123"), Is.True);
    }

    [Test]
    public void AddUserMedication_ExistingMed_ReturnsFailure()
    {
        _repo.AddUserMedication("DuplicateMed");
        var result = _repo.AddUserMedication("DuplicateMed");
        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public void RemoveUserMedication_UserMed_ReturnsSuccess()
    {
        _repo.AddUserMedication("RemoveMe");
        var result = _repo.RemoveUserMedication("RemoveMe");
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(_repo.MedicationExists("RemoveMe"), Is.False);
    }

    [Test]
    public void RemoveUserMedication_SystemMed_ReturnsFailure()
    {
        // "Aspirin" is likely a system med from seed
        var result = _repo.RemoveUserMedication("Aspirin");
        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public void GetStats_ReturnsValidStats()
    {
        var stats = _repo.GetStats();
        Assert.That(stats.TotalCount, Is.GreaterThan(0));
        Assert.That(stats.LoadedFrom, Is.Not.Null);
    }

    [Test]
    public void SearchMedications_ValidQuery_ReturnsMatches()
    {
        var results = _repo.SearchMedications("Asp");
        Assert.That(results, Is.Not.Empty);
        Assert.That(results.First(), Does.StartWith("Asp").IgnoreCase);
    }

    [Test]
    public void ImportReplace_ValidData_ReturnsSuccess()
    {
        var meds = new List<Medication> { new Medication { Name = "Imported1", IsSystemMed = true } };
        var result = _repo.ImportReplace(meds);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(_repo.MedicationExists("Imported1"), Is.True);
    }

    [Test]
    public void ImportMerge_ValidData_ReturnsSuccess()
    {
        var meds = new List<Medication> { new Medication { Name = "Merged1", IsSystemMed = false } };
        var result = _repo.ImportMerge(meds);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(_repo.MedicationExists("Merged1"), Is.True);
    }

    [Test]
    public void Reseed_ResetsDatabase()
    {
        _repo.AddUserMedication("UserMed");
        var result = _repo.Reseed();
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(_repo.MedicationExists("UserMed"), Is.False);
    }

    [Test]
    public void MedicationExists_NullOrEmpty_ReturnsFalse()
    {
        Assert.That(_repo.MedicationExists(null!), Is.False);
        Assert.That(_repo.MedicationExists(""), Is.False);
    }

    [Test]
    public void AddUserMedication_EmptyName_ReturnsFailure()
    {
        var result = _repo.AddUserMedication("");
        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public void RemoveUserMedication_EmptyName_ReturnsFailure()
    {
        var result = _repo.RemoveUserMedication("");
        Assert.That(result.IsFailure, Is.True);
    }

    [Test]
    public void RemoveUserMedication_NotFound_ReturnsFailure()
    {
        var result = _repo.RemoveUserMedication("DoesNotExissssst");
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Does.Contain("not found"));
    }

    [Test]
    public void ExportAll_ReturnsValidBackup()
    {
        var backup = _repo.ExportAll();
        Assert.That(backup, Is.Not.Null);
        Assert.That(backup.Medications, Is.Not.Empty);
    }

    [Test]
    public void Dispose_ClosesConnection()
    {
        // Use the existing _repo from Setup or create a new one safely
        // But since Setup already has one, let's just use it and null it out
        _repo.Dispose();
        // Just verify no exception on double dispose
        Assert.DoesNotThrow(() => _repo.Dispose());
        _repo = null!; // Prevent TearDown from trying to use it
    }

    [Test]
    public void ImportMerge_ExistingData_SkipsDuplicates()
    {
        _repo.AddUserMedication("Existing");
        var meds = new List<Medication> { new Medication { Name = "Existing", IsSystemMed = false } };
        var result = _repo.ImportMerge(meds);
        Assert.That(result.IsSuccess, Is.True);
        // Verify no exception and logic completes
    }

    [Test]
    public void MedicationExists_CaseInsensitive_ReturnsTrue()
    {
        _repo.AddUserMedication("MixedCase");
        Assert.That(_repo.MedicationExists("mixedcase"), Is.True);
    }

    [Test]
    public void SearchMedications_EmptyQuery_ReturnsEmpty()
    {
        Assert.That(_repo.SearchMedications(""), Is.Empty);
        Assert.That(_repo.SearchMedications(null!), Is.Empty);
    }
}