using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SleepEditWeb.Infrastructure.ProtocolPersistence;
using SleepEditWeb.Models;
using SleepEditWeb.Services;

namespace SleepEditWeb.Tests;

[TestFixture]
public class ProtocolEditorSessionStoreTests
{
    [Test]
    public void Load_WhenSessionMissing_UsesLatestRepositoryVersion()
    {
        // Arrange
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var starter = new Mock<IProtocolStarterService>();
        starter.Setup(x => x.Create()).Returns(CreateDocument("Starter Protocol"));

        var repository = new Mock<IProtocolRepository>();
        repository.Setup(x => x.GetLatestVersion()).Returns(new ProtocolVersion(
            VersionId: Guid.NewGuid(),
            SavedUtc: DateTime.UtcNow,
            Source: "test",
            Note: "latest",
            Document: CreateDocument("Repository Protocol")));

        var store = new ProtocolEditorSessionStore(
            accessor.Object,
            starter.Object,
            repository.Object,
            NullLogger<ProtocolEditorSessionStore>.Instance);

        // Act
        var snapshot = store.Load();

        // Assert
        Assert.That(snapshot.Document.Text, Is.EqualTo("Repository Protocol"));
        starter.Verify(x => x.Create(), Times.Never);
    }

    [Test]
    public void Load_WhenRepositoryUnavailable_FallsBackToStarterSnapshot()
    {
        // Arrange
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var starter = new Mock<IProtocolStarterService>();
        starter.Setup(x => x.Create()).Returns(CreateDocument("Starter Protocol"));

        var repository = new Mock<IProtocolRepository>();
        repository.Setup(x => x.GetLatestVersion()).Throws(new IOException("database unavailable"));

        var store = new ProtocolEditorSessionStore(
            accessor.Object,
            starter.Object,
            repository.Object,
            NullLogger<ProtocolEditorSessionStore>.Instance);

        // Act
        var snapshot = store.Load();

        // Assert
        Assert.That(snapshot.Document.Text, Is.EqualTo("Starter Protocol"));
        starter.Verify(x => x.Create(), Times.Once);
    }


    [Test]
    public void Reset_WhenRepositoryHasVersion_UsesStarterContentNotRepository()
    {
        // Arrange
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var starter = new Mock<IProtocolStarterService>();
        starter.Setup(x => x.Create()).Returns(CreateDocument("Starter Protocol"));

        var repository = new Mock<IProtocolRepository>();
        repository.Setup(x => x.GetLatestVersion()).Returns(new ProtocolVersion(
            VersionId: Guid.NewGuid(),
            SavedUtc: DateTime.UtcNow,
            Source: "test",
            Note: "latest",
            Document: CreateDocument("Repository Protocol")));

        var store = new ProtocolEditorSessionStore(
            accessor.Object,
            starter.Object,
            repository.Object,
            NullLogger<ProtocolEditorSessionStore>.Instance);

        // Act
        store.Reset();

        // Assert — Reset must use starter service, not the repository
        starter.Verify(x => x.Create(), Times.Once);
        repository.Verify(x => x.GetLatestVersion(), Times.Never);
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
