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
        using var repository = new LiteDbProtocolRepository(
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
        using var repository = new LiteDbProtocolRepository(
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
        using var repository = new LiteDbProtocolRepository(
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
        using var repository = new LiteDbProtocolRepository(
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
        using var repository = new LiteDbProtocolRepository(
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
        using var repository = new LiteDbProtocolRepository(
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
