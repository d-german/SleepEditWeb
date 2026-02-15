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
}
