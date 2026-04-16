using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SleepEditWeb.Infrastructure.ProtocolPersistence;
using SleepEditWeb.Models;
using SleepEditWeb.Services;

namespace SleepEditWeb.Tests;

[TestFixture]
public class ProtocolStarterServiceTests
{
    [Test]
    public void Create_UsesCurrentProtocolFromRepository_WhenAvailable()
    {
        // Arrange
        var repository = new Mock<IProtocolRepository>();
        repository.Setup(r => r.GetDefaultProtocol()).Returns(new ProtocolVersion(
            VersionId: Guid.NewGuid(),
            SavedUtc: DateTime.UtcNow,
            Source: "test",
            Note: "db protocol",
            Document: CreateDocument("DB Protocol")));

        var service = new ProtocolStarterService(
            repository.Object,
            NullLogger<ProtocolStarterService>.Instance);

        // Act
        var result = service.Create();

        // Assert
        Assert.That(result.Text, Is.EqualTo("DB Protocol"));
        repository.Verify(r => r.GetDefaultProtocol(), Times.Once);
    }

    [Test]
    public void Create_FallsBackToSeed_WhenRepositoryReturnsNull()
    {
        // Arrange
        var repository = new Mock<IProtocolRepository>();
        repository.Setup(r => r.GetCurrentProtocol()).Returns((ProtocolVersion?)null);

        var service = new ProtocolStarterService(
            repository.Object,
            NullLogger<ProtocolStarterService>.Instance);

        // Act
        var result = service.Create();

        // Assert
        Assert.That(result.Text, Is.EqualTo("Saint Luke's Protocol"));
        Assert.That(result.Sections.Any(s => s.Text == "Diagnostic Polysomnogram:"), Is.True);
    }

    [Test]
    public void Create_FallsBackToSeed_WhenRepositoryThrows()
    {
        // Arrange
        var repository = new Mock<IProtocolRepository>();
        repository.Setup(r => r.GetCurrentProtocol()).Throws(new IOException("database unavailable"));

        var service = new ProtocolStarterService(
            repository.Object,
            NullLogger<ProtocolStarterService>.Instance);

        // Act
        var result = service.Create();

        // Assert
        Assert.That(result.Text, Is.EqualTo("Saint Luke's Protocol"));
        Assert.That(result.Sections.Count, Is.GreaterThan(0));
    }

    [Test]
    public void Create_SeedProtocol_HasExpectedSections()
    {
        // Arrange
        var repository = new Mock<IProtocolRepository>();
        repository.Setup(r => r.GetCurrentProtocol()).Returns((ProtocolVersion?)null);

        var service = new ProtocolStarterService(
            repository.Object,
            NullLogger<ProtocolStarterService>.Instance);

        // Act
        var result = service.Create();

        // Assert
        Assert.That(result.Sections, Has.Count.EqualTo(12));
        Assert.That(result.Sections[0].Text, Is.EqualTo("Diagnostic Polysomnogram:"));
        Assert.That(result.Sections.Any(s => s.Text == "CPAP Titration Polysomnogram:"), Is.True);
        Assert.That(result.Sections.Any(s => s.Text == "End of Study:"), Is.True);
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
