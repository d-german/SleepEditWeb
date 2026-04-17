using Microsoft.Extensions.Logging.Abstractions;
using SleepEditWeb.Infrastructure.ProtocolPersistence;
using SleepEditWeb.Models;
using SleepEditWeb.Services;

namespace SleepEditWeb.Tests;

[TestFixture]
public class LiteDbProtocolRepositoryTests
{
    [Test]
    public void SaveVersion_ThenGetLatest_ReturnsSavedVersionWithMetadata()
    {
        // Arrange
        var xmlService = new ProtocolXmlService(NullLogger<ProtocolXmlService>.Instance);
        var db = new LiteDB.LiteDatabase(":memory:");
        using var repository = new LiteDbProtocolRepository(
            db,
            xmlService,
            NullLogger<LiteDbProtocolRepository>.Instance);

        var document = new ProtocolDocument
        {
            Id = -1,
            LinkId = -1,
            LinkText = string.Empty,
            Text = $"Versioned-{Guid.NewGuid():N}",
            Sections = []
        };

        // Act
        var saved = repository.SaveVersion(document, "unit-test", "save metadata coverage");
        var latest = repository.GetLatestVersion();

        // Assert
        Assert.That(latest, Is.Not.Null);
        Assert.That(latest!.VersionId, Is.EqualTo(saved.VersionId));
        Assert.That(latest.Source, Is.EqualTo("unit-test"));
        Assert.That(latest.Note, Is.EqualTo("save metadata coverage"));
        Assert.That(latest.Document.Text, Is.EqualTo(document.Text));
    }

    [Test]
    public void SaveCurrentProtocol_ThenGetCurrent_ReturnsSavedDocument()
    {
        // Arrange
        var xmlService = new ProtocolXmlService(NullLogger<ProtocolXmlService>.Instance);
        var db = new LiteDB.LiteDatabase(":memory:");
        using var repository = new LiteDbProtocolRepository(
            db,
            xmlService,
            NullLogger<LiteDbProtocolRepository>.Instance);

        var document = CreateDocument($"Current-{Guid.NewGuid():N}");

        // Act
        var saved = repository.SaveCurrentProtocol(document, "unit-test");
        var current = repository.GetCurrentProtocol();

        // Assert
        Assert.That(current, Is.Not.Null);
        Assert.That(current!.Source, Is.EqualTo("unit-test"));
        Assert.That(current.Document.Text, Is.EqualTo(document.Text));
    }

    [Test]
    public void SaveCurrentProtocol_AlsoSavesToVersionHistory()
    {
        // Arrange
        var xmlService = new ProtocolXmlService(NullLogger<ProtocolXmlService>.Instance);
        var db = new LiteDB.LiteDatabase(":memory:");
        using var repository = new LiteDbProtocolRepository(
            db,
            xmlService,
            NullLogger<LiteDbProtocolRepository>.Instance);

        var document = CreateDocument($"Versioned-{Guid.NewGuid():N}");

        // Act
        repository.SaveCurrentProtocol(document, "unit-test");
        var latest = repository.GetLatestVersion();

        // Assert
        Assert.That(latest, Is.Not.Null);
        Assert.That(latest!.Document.Text, Is.EqualTo(document.Text));
    }

    [Test]
    public void SaveCurrentProtocol_Upserts_OverwritesPreviousCurrent()
    {
        // Arrange
        var xmlService = new ProtocolXmlService(NullLogger<ProtocolXmlService>.Instance);
        var db = new LiteDB.LiteDatabase(":memory:");
        using var repository = new LiteDbProtocolRepository(
            db,
            xmlService,
            NullLogger<LiteDbProtocolRepository>.Instance);

        var first = CreateDocument("First Protocol");
        var second = CreateDocument("Second Protocol");

        // Act
        repository.SaveCurrentProtocol(first, "first-save");
        repository.SaveCurrentProtocol(second, "second-save");
        var current = repository.GetCurrentProtocol();

        // Assert
        Assert.That(current, Is.Not.Null);
        Assert.That(current!.Document.Text, Is.EqualTo("Second Protocol"));
        Assert.That(current.Source, Is.EqualTo("second-save"));
    }

    [Test]
    public void GetCurrentProtocol_ReturnsNonNull_WhenVersionHistoryExists()
    {
        // Arrange
        var xmlService = new ProtocolXmlService(NullLogger<ProtocolXmlService>.Instance);
        var db = new LiteDB.LiteDatabase(":memory:");
        using var repository = new LiteDbProtocolRepository(
            db,
            xmlService,
            NullLogger<LiteDbProtocolRepository>.Instance);

        var document = CreateDocument($"Legacy-{Guid.NewGuid():N}");
        repository.SaveVersion(document, "legacy-save", "pre-migration");

        // Act
        var current = repository.GetCurrentProtocol();

        // Assert — GetCurrentProtocol should return a result (either current or fallback to latest)
        Assert.That(current, Is.Not.Null);
    }

    [Test]
    public void GetCurrentProtocol_WhenCurrentSaved_PrefersCurrentOverLatestVersion()
    {
        // Arrange
        var xmlService = new ProtocolXmlService(NullLogger<ProtocolXmlService>.Instance);
        var db = new LiteDB.LiteDatabase(":memory:");
        using var repository = new LiteDbProtocolRepository(
            db,
            xmlService,
            NullLogger<LiteDbProtocolRepository>.Instance);

        var versionDoc = CreateDocument($"VersionOnly-{Guid.NewGuid():N}");
        var currentDoc = CreateDocument($"Current-{Guid.NewGuid():N}");

        repository.SaveVersion(versionDoc, "version-save", "older version");
        repository.SaveCurrentProtocol(currentDoc, "current-save");

        // Act
        var current = repository.GetCurrentProtocol();

        // Assert — should return current protocol, not the version-only entry
        Assert.That(current, Is.Not.Null);
        Assert.That(current!.Document.Text, Is.EqualTo(currentDoc.Text));
        Assert.That(current.Source, Is.EqualTo("current-save"));
    }

    [Test]
    public void SaveProtocol_ThenGetProtocol_ReturnsSavedVersion()
    {
        var xmlService = new ProtocolXmlService(NullLogger<ProtocolXmlService>.Instance);
        var db2 = new LiteDB.LiteDatabase(":memory:");
        using var repository = new LiteDbProtocolRepository(db2, xmlService, NullLogger<LiteDbProtocolRepository>.Instance);
        var protocolId = Guid.NewGuid();
        var document = CreateDocument("Multi-Protocol Test");

        var saved = repository.SaveProtocol(protocolId, "Test Protocol", document, "unit-test");
        var retrieved = repository.GetProtocol(protocolId);

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Document.Text, Is.EqualTo("Multi-Protocol Test"));
        Assert.That(retrieved.Source, Is.Empty);
    }

    [Test]
    public void SaveProtocol_UpsertsSameProtocolId()
    {
        var xmlService = new ProtocolXmlService(NullLogger<ProtocolXmlService>.Instance);
        var db2 = new LiteDB.LiteDatabase(":memory:");
        using var repository = new LiteDbProtocolRepository(db2, xmlService, NullLogger<LiteDbProtocolRepository>.Instance);
        var protocolId = Guid.NewGuid();

        repository.SaveProtocol(protocolId, "First", CreateDocument("First"), "save-1");
        repository.SaveProtocol(protocolId, "Updated", CreateDocument("Updated"), "save-2");
        var retrieved = repository.GetProtocol(protocolId);

        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Document.Text, Is.EqualTo("Updated"));
    }

    [Test]
    public void GetProtocol_ReturnsNull_ForUnknownId()
    {
        var xmlService = new ProtocolXmlService(NullLogger<ProtocolXmlService>.Instance);
        var db2 = new LiteDB.LiteDatabase(":memory:");
        using var repository = new LiteDbProtocolRepository(db2, xmlService, NullLogger<LiteDbProtocolRepository>.Instance);

        var result = repository.GetProtocol(Guid.NewGuid());

        Assert.That(result, Is.Null);
    }

    [Test]
    public void ListProtocols_ReturnsAllSavedProtocols()
    {
        var xmlService = new ProtocolXmlService(NullLogger<ProtocolXmlService>.Instance);
        var db2 = new LiteDB.LiteDatabase(":memory:");
        using var repository = new LiteDbProtocolRepository(db2, xmlService, NullLogger<LiteDbProtocolRepository>.Instance);

        repository.SaveProtocol(Guid.NewGuid(), "Protocol A", CreateDocument("A"), "test");
        repository.SaveProtocol(Guid.NewGuid(), "Protocol B", CreateDocument("B"), "test");

        var list = repository.ListProtocols();

        Assert.That(list.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(list.Any(p => p.Name == "Protocol A"), Is.True);
        Assert.That(list.Any(p => p.Name == "Protocol B"), Is.True);
    }

    [Test]
    public void DeleteProtocol_RemovesProtocol()
    {
        var xmlService = new ProtocolXmlService(NullLogger<ProtocolXmlService>.Instance);
        var db2 = new LiteDB.LiteDatabase(":memory:");
        using var repository = new LiteDbProtocolRepository(db2, xmlService, NullLogger<LiteDbProtocolRepository>.Instance);
        var protocolId = Guid.NewGuid();

        repository.SaveProtocol(protocolId, "To Delete", CreateDocument("Delete Me"), "test");
        var deleted = repository.DeleteProtocol(protocolId);
        var retrieved = repository.GetProtocol(protocolId);

        Assert.That(deleted, Is.True);
        Assert.That(retrieved, Is.Null);
    }

    [Test]
    public void DeleteProtocol_ReturnsFalse_ForUnknownId()
    {
        var xmlService = new ProtocolXmlService(NullLogger<ProtocolXmlService>.Instance);
        var db2 = new LiteDB.LiteDatabase(":memory:");
        using var repository = new LiteDbProtocolRepository(db2, xmlService, NullLogger<LiteDbProtocolRepository>.Instance);

        var result = repository.DeleteProtocol(Guid.NewGuid());

        Assert.That(result, Is.False);
    }

    [Test]
    public void RenameProtocol_UpdatesName()
    {
        var xmlService = new ProtocolXmlService(NullLogger<ProtocolXmlService>.Instance);
        var db2 = new LiteDB.LiteDatabase(":memory:");
        using var repository = new LiteDbProtocolRepository(db2, xmlService, NullLogger<LiteDbProtocolRepository>.Instance);
        var protocolId = Guid.NewGuid();

        repository.SaveProtocol(protocolId, "Original Name", CreateDocument("Test"), "test");
        repository.RenameProtocol(protocolId, "New Name");
        var list = repository.ListProtocols();

        Assert.That(list.Any(p => p.ProtocolId == protocolId && p.Name == "New Name"), Is.True);
    }

    [Test]
    public void SetDefaultProtocol_ChangesDefaultFlag()
    {
        var xmlService = new ProtocolXmlService(NullLogger<ProtocolXmlService>.Instance);
        var db2 = new LiteDB.LiteDatabase(":memory:");
        using var repository = new LiteDbProtocolRepository(db2, xmlService, NullLogger<LiteDbProtocolRepository>.Instance);
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        repository.SaveProtocol(id1, "Proto 1", CreateDocument("P1"), "test");
        repository.SaveProtocol(id2, "Proto 2", CreateDocument("P2"), "test");
        repository.SetDefaultProtocol(id2);
        var defaultProtocol = repository.GetDefaultProtocol();

        Assert.That(defaultProtocol, Is.Not.Null);
        Assert.That(defaultProtocol!.Document.Text, Is.EqualTo("P2"));
    }

    [Test]
    public void GetDefaultProtocol_DoesNotThrow_WhenCalledOnSharedDatabase()
    {
        var xmlService = new ProtocolXmlService(NullLogger<ProtocolXmlService>.Instance);
        var db2 = new LiteDB.LiteDatabase(":memory:");
        using var repository = new LiteDbProtocolRepository(db2, xmlService, NullLogger<LiteDbProtocolRepository>.Instance);

        // Should not throw regardless of database state (may return null or a migrated protocol)
        Assert.DoesNotThrow(() => repository.GetDefaultProtocol());
    }

    private static ProtocolDocument CreateDocument(string text)
    {
        return new ProtocolDocument
        {
            Id = -1,
            LinkId = -1,
            LinkText = string.Empty,
            Text = text,
            Sections = []
        };
    }
}
