using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SleepEditWeb.Infrastructure.ProtocolPersistence;
using SleepEditWeb.Models;
using SleepEditWeb.Services;

namespace SleepEditWeb.Tests;

[TestFixture]
public class ProtocolManagementServiceTests
{
    private Mock<IProtocolRepository> _repository = null!;
    private Mock<IProtocolEditorSessionStore> _sessionStore = null!;
    private ProtocolManagementService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<IProtocolRepository>();
        _sessionStore = new Mock<IProtocolEditorSessionStore>();
        _sut = new ProtocolManagementService(
            _repository.Object,
            _sessionStore.Object,
            NullLogger<ProtocolManagementService>.Instance);
    }

    [Test]
    public void SaveActiveProtocol_WhenActiveProtocolExists_CallsSaveProtocol()
    {
        var activeId = Guid.NewGuid();
        var document = new ProtocolDocument
        {
            Id = 0, LinkId = -1, LinkText = string.Empty, Text = "My Protocol", Sections = []
        };

        _sessionStore.Setup(s => s.GetActiveProtocolId()).Returns(activeId);
        _repository
            .Setup(r => r.SaveProtocol(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<ProtocolDocument>(), It.IsAny<string>()))
            .Returns(new ProtocolVersion(activeId, DateTime.UtcNow, string.Empty, string.Empty, document));

        _sut.SaveActiveProtocol(document, "TestSource");

        _repository.Verify(
            r => r.SaveProtocol(activeId, document.Text, document, "TestSource"),
            Times.Once);

        _repository.Verify(
            r => r.SaveCurrentProtocol(It.IsAny<ProtocolDocument>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public void SaveActiveProtocol_WhenNoActiveProtocol_CallsSaveCurrentProtocol()
    {
        var document = new ProtocolDocument
        {
            Id = 0, LinkId = -1, LinkText = string.Empty, Text = "My Protocol", Sections = []
        };

        _sessionStore.Setup(s => s.GetActiveProtocolId()).Returns((Guid?)null);
        _repository
            .Setup(r => r.SaveCurrentProtocol(It.IsAny<ProtocolDocument>(), It.IsAny<string>()))
            .Returns(new ProtocolVersion(Guid.Empty, DateTime.UtcNow, string.Empty, string.Empty, document));

        _sut.SaveActiveProtocol(document, "TestSource");

        _repository.Verify(
            r => r.SaveCurrentProtocol(document, "TestSource"),
            Times.Once);

        _repository.Verify(
            r => r.SaveProtocol(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<ProtocolDocument>(), It.IsAny<string>()),
            Times.Never);
    }
}
