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
    public void Load_WhenSessionMissing_UsesCurrentProtocolFromRepository()
    {
        // Arrange
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var starter = new Mock<IProtocolStarterService>();
        starter.Setup(x => x.Create()).Returns(CreateDocument("Starter Protocol"));

        var repository = new Mock<IProtocolRepository>();
        repository.Setup(x => x.GetDefaultProtocol()).Returns(new ProtocolVersion(
            VersionId: Guid.NewGuid(),
            SavedUtc: DateTime.UtcNow,
            Source: "test",
            Note: "current",
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
        repository.Setup(x => x.GetCurrentProtocol()).Throws(new IOException("database unavailable"));

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
        repository.Setup(x => x.GetDefaultProtocol()).Returns(new ProtocolVersion(
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
        repository.Verify(x => x.GetCurrentProtocol(), Times.Never);
    }

    [Test]
    public void Save_WhenSessionWriteFails_KeepsSnapshotInMemory()
    {
        // Arrange
        var session = new Mock<ISession>();
        session
            .Setup(x => x.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
            .Throws(new InvalidOperationException("The session cannot be established after the response has started."));

        var httpContext = new Mock<HttpContext>();
        httpContext.SetupGet(x => x.Session).Returns(session.Object);

        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns(httpContext.Object);

        var starter = new Mock<IProtocolStarterService>();
        starter.Setup(x => x.Create()).Returns(CreateDocument("Starter Protocol"));

        var repository = new Mock<IProtocolRepository>();
        repository.Setup(x => x.GetCurrentProtocol()).Returns((ProtocolVersion?)null);

        var store = new ProtocolEditorSessionStore(
            accessor.Object,
            starter.Object,
            repository.Object,
            NullLogger<ProtocolEditorSessionStore>.Instance);

        var snapshot = new ProtocolEditorSnapshot
        {
            Document = CreateDocument("Updated Protocol"),
            UndoHistory = [],
            RedoHistory = [],
            LastUpdatedUtc = DateTimeOffset.UtcNow
        };

        // Act
        Assert.DoesNotThrow(() => store.Save(snapshot));
        var loaded = store.Load();

        // Assert
        Assert.That(loaded.Document.Text, Is.EqualTo("Updated Protocol"));
        session.Verify(x => x.Set(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Once);
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
