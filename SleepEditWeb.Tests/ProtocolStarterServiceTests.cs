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

    [Test]
    public void CreateWithGuid_LoadsSpecificProtocol_WhenFound()
    {
        var protocolId = Guid.NewGuid();
        var repository = new Mock<IProtocolRepository>();
        repository.Setup(r => r.GetProtocol(protocolId)).Returns(new ProtocolVersion(
            VersionId: Guid.NewGuid(),
            SavedUtc: DateTime.UtcNow,
            Source: "test",
            Note: "specific protocol",
            Document: CreateDocument("Specific Protocol")));

        var service = new ProtocolStarterService(repository.Object, NullLogger<ProtocolStarterService>.Instance);

        var result = service.Create(protocolId);

        Assert.That(result.Text, Is.EqualTo("Specific Protocol"));
        repository.Verify(r => r.GetProtocol(protocolId), Times.Once);
    }

    [Test]
    public void CreateWithGuid_FallsBackToSeed_WhenProtocolNotFound()
    {
        var repository = new Mock<IProtocolRepository>();
        repository.Setup(r => r.GetProtocol(It.IsAny<Guid>())).Returns((ProtocolVersion?)null);

        var service = new ProtocolStarterService(repository.Object, NullLogger<ProtocolStarterService>.Instance);

        var result = service.Create(Guid.NewGuid());

        Assert.That(result.Text, Is.EqualTo("Saint Luke's Protocol"));
    }

    [Test]
    public void CreateSeedDocument_ReturnsDefaultSeedProtocol()
    {
        var repository = new Mock<IProtocolRepository>();
        var service = new ProtocolStarterService(repository.Object, NullLogger<ProtocolStarterService>.Instance);

        var result = service.CreateSeedDocument();

        Assert.That(result.Text, Is.EqualTo("Saint Luke's Protocol"));
        Assert.That(result.Sections.Count, Is.EqualTo(12));
        repository.Verify(r => r.GetCurrentProtocol(), Times.Never);
        repository.Verify(r => r.GetDefaultProtocol(), Times.Never);
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
