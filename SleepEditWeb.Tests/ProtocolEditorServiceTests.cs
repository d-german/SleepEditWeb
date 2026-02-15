using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SleepEditWeb.Application.Protocol;
using SleepEditWeb.Application.Protocol.Commands;
using SleepEditWeb.Models;
using SleepEditWeb.Protocol.Domain;
using SleepEditWeb.Services;

namespace SleepEditWeb.Tests;

[TestFixture]
public class ProtocolEditorServiceTests
{
    private InMemoryProtocolEditorSessionStore _sessionStore;
    private ProtocolEditorService _service;

    [SetUp]
    public void SetUp()
    {
        _sessionStore = new InMemoryProtocolEditorSessionStore(new ProtocolStarterService(
            new ProtocolXmlService(NullLogger<ProtocolXmlService>.Instance),
            Options.Create(new ProtocolEditorStartupOptions()),
            NullLogger<ProtocolStarterService>.Instance));
        _service = new ProtocolEditorService(
            _sessionStore,
            new ProtocolXmlService(NullLogger<ProtocolXmlService>.Instance),
            new AddSectionCommandHandler(),
            new AddChildCommandHandler(),
            new RemoveNodeCommandHandler(),
            new UpdateNodeCommandHandler(),
            new MoveNodeCommandHandler(),
            new AddSubTextCommandHandler(),
            new RemoveSubTextCommandHandler(),
            NullLogger<ProtocolEditorService>.Instance);
    }

    [Test]
    public void AddSection_DelegatesToHandler_AndPersistsSnapshot()
    {
        // Arrange
        var snapshot = CreateSnapshot();
        var sessionStore = new Mock<IProtocolEditorSessionStore>();
        sessionStore.Setup(x => x.Load()).Returns(snapshot);
        var xmlService = new Mock<IProtocolXmlService>();
        var addSectionHandler = new Mock<IProtocolCommandHandler<AddSectionCommand>>();
        addSectionHandler
            .Setup(x => x.Handle(It.IsAny<ProtocolTreeDocument>(), It.IsAny<AddSectionCommand>()))
            .Returns<ProtocolTreeDocument, AddSectionCommand>((document, command) =>
                ProtocolResult<ProtocolTreeDocument>.Success(ProtocolTreeFunctions.AddSection(document, command.Text)));

        var service = CreateService(
            sessionStore.Object,
            xmlService.Object,
            addSectionHandler.Object,
            CreateNoOpHandler<AddChildCommand>(),
            CreateNoOpHandler<RemoveNodeCommand>(),
            CreateNoOpHandler<UpdateNodeCommand>(),
            CreateNoOpHandler<MoveNodeCommand>(),
            CreateNoOpHandler<AddSubTextCommand>(),
            CreateNoOpHandler<RemoveSubTextCommand>());

        // Act
        var updated = service.AddSection("Delegated Section");

        // Assert
        addSectionHandler.Verify(
            x => x.Handle(It.IsAny<ProtocolTreeDocument>(), It.Is<AddSectionCommand>(command => command.Text == "Delegated Section")),
            Times.Once);
        sessionStore.Verify(x => x.Save(It.IsAny<ProtocolEditorSnapshot>()), Times.Once);
        Assert.That(updated.UndoHistory, Has.Count.EqualTo(1));
        Assert.That(updated.RedoHistory, Is.Empty);
        Assert.That(updated.Document.Sections.Any(section => section.Text == "Delegated Section"), Is.True);
    }

    [Test]
    public void AddChild_WhenHandlerFails_DoesNotPersistMutation()
    {
        // Arrange
        var snapshot = CreateSnapshot();
        var sessionStore = new Mock<IProtocolEditorSessionStore>();
        sessionStore.Setup(x => x.Load()).Returns(snapshot);
        var xmlService = new Mock<IProtocolXmlService>();
        var addChildHandler = new Mock<IProtocolCommandHandler<AddChildCommand>>();
        addChildHandler
            .Setup(x => x.Handle(It.IsAny<ProtocolTreeDocument>(), It.IsAny<AddChildCommand>()))
            .Returns(ProtocolResult<ProtocolTreeDocument>.Failure("parent_not_found", "Requested parent node could not be resolved."));

        var service = CreateService(
            sessionStore.Object,
            xmlService.Object,
            CreateNoOpHandler<AddSectionCommand>(),
            addChildHandler.Object,
            CreateNoOpHandler<RemoveNodeCommand>(),
            CreateNoOpHandler<UpdateNodeCommand>(),
            CreateNoOpHandler<MoveNodeCommand>(),
            CreateNoOpHandler<AddSubTextCommand>(),
            CreateNoOpHandler<RemoveSubTextCommand>());

        // Act
        var result = service.AddChild(999999, "Should fail");

        // Assert
        addChildHandler.Verify(
            x => x.Handle(It.IsAny<ProtocolTreeDocument>(), It.Is<AddChildCommand>(command => command.ParentId == 999999)),
            Times.Once);
        sessionStore.Verify(x => x.Save(It.IsAny<ProtocolEditorSnapshot>()), Times.Never);
        Assert.That(result, Is.SameAs(snapshot));
    }

    [Test]
    public void MoveNode_UndoAndRedo_AreDeterministic()
    {
        // Arrange
        var before = _service.Load();
        var sectionOneId = before.Document.Sections[0].Id;
        var sectionTwoId = before.Document.Sections[1].Id;
        _service.AddChild(sectionOneId, "Move Me");
        var childId = _service.Load().Document.Sections[0].Children[^1].Id;

        // Act
        _service.MoveNode(childId, sectionTwoId, 0);
        var moved = _service.Load();
        _service.Undo();
        var undone = _service.Load();
        _service.Redo();
        var redone = _service.Load();

        // Assert
        Assert.That(ContainsNode(moved.Document.Sections[1].Children, childId), Is.True);
        Assert.That(ContainsNode(undone.Document.Sections[0].Children, childId), Is.True);
        Assert.That(ContainsNode(redone.Document.Sections[1].Children, childId), Is.True);
    }

    [Test]
    public void RemoveNode_ClearsInboundLinksPointingToRemovedNode()
    {
        // Arrange
        var snapshot = _service.Load();
        var sourceId = snapshot.Document.Sections[0].Id;
        var targetId = snapshot.Document.Sections[1].Id;
        _service.UpdateNode(sourceId, "Source", targetId, "Target");

        // Act
        _service.RemoveNode(targetId);
        var updated = _service.Load();
        var source = updated.Document.Sections[0];

        // Assert
        Assert.That(source.LinkId, Is.EqualTo(-1));
        Assert.That(source.LinkText, Is.EqualTo(string.Empty));
    }

    [Test]
    public void MoveNode_SectionToNonRootParent_IsRejected()
    {
        // Arrange
        var initial = _service.Load();
        var sectionId = initial.Document.Sections[0].Id;
        var subsectionParentId = initial.Document.Sections[0].Children[0].Id;

        // Act
        _service.MoveNode(sectionId, subsectionParentId, 0);
        var after = _service.Load();

        // Assert
        Assert.That(after.Document.Sections[0].Id, Is.EqualTo(sectionId));
        Assert.That(after.Document.Sections, Has.Count.EqualTo(initial.Document.Sections.Count));
    }

    [Test]
    public void UpdateNode_AssignsLinkIdAndLinkText()
    {
        // Arrange
        var snapshot = _service.Load();
        var source = snapshot.Document.Sections[0];
        var target = snapshot.Document.Sections[1];

        // Act
        _service.UpdateNode(source.Id, source.Text, target.Id, target.Text);
        var updated = _service.Load().Document.Sections[0];

        // Assert
        Assert.That(updated.LinkId, Is.EqualTo(target.Id));
        Assert.That(updated.LinkText, Is.EqualTo(target.Text));
    }

    [Test]
    public void UpdateNode_ClearLink_ResetsLinkFields()
    {
        // Arrange
        var snapshot = _service.Load();
        var source = snapshot.Document.Sections[0];
        var target = snapshot.Document.Sections[1];
        _service.UpdateNode(source.Id, source.Text, target.Id, target.Text);

        // Act
        _service.UpdateNode(source.Id, source.Text, -1, string.Empty);
        var updated = _service.Load().Document.Sections[0];

        // Assert
        Assert.That(updated.LinkId, Is.EqualTo(-1));
        Assert.That(updated.LinkText, Is.EqualTo(string.Empty));
    }

    [Test]
    public void AddSubText_TrimsInput_AndIgnoresWhitespaceOnlyValues()
    {
        // Arrange
        var snapshot = _service.Load();
        var nodeId = snapshot.Document.Sections[0].Id;
        var uniqueValue = $"trim-target-{Guid.NewGuid():N}";

        // Act
        _service.AddSubText(nodeId, $"  {uniqueValue}  ");
        _service.AddSubText(nodeId, "   ");
        var updated = _service.Load().Document.Sections[0];

        // Assert
        Assert.That(updated.SubText, Does.Contain(uniqueValue));
        Assert.That(updated.SubText.Any(string.IsNullOrWhiteSpace), Is.False);
    }

    [Test]
    public void RemoveSubText_RemovesEntriesCaseInsensitively()
    {
        // Arrange
        var snapshot = _service.Load();
        var nodeId = snapshot.Document.Sections[0].Id;
        var uniqueValue = $"case-target-{Guid.NewGuid():N}";
        _service.AddSubText(nodeId, uniqueValue);

        // Act
        _service.RemoveSubText(nodeId, uniqueValue.ToUpperInvariant());
        var updated = _service.Load().Document.Sections[0];

        // Assert
        Assert.That(updated.SubText.Any(item => item.Equals(uniqueValue, StringComparison.OrdinalIgnoreCase)), Is.False);
    }

    [Test]
    public void AddSubText_UndoAndRedo_AreDeterministic()
    {
        // Arrange
        var snapshot = _service.Load();
        var nodeId = snapshot.Document.Sections[0].Id;
        var uniqueValue = $"undo-redo-subtext-{Guid.NewGuid():N}";

        // Act
        _service.AddSubText(nodeId, uniqueValue);
        var afterAdd = _service.Load();
        _service.Undo();
        var afterUndo = _service.Load();
        _service.Redo();
        var afterRedo = _service.Load();

        // Assert
        Assert.That(afterAdd.Document.Sections[0].SubText, Does.Contain(uniqueValue));
        Assert.That(afterUndo.Document.Sections[0].SubText, Does.Not.Contain(uniqueValue));
        Assert.That(afterRedo.Document.Sections[0].SubText, Does.Contain(uniqueValue));
    }

    [Test]
    public void AddSection_UsesDomainHistoryUndoEngineByDefault()
    {
        // Act
        var updated = _service.AddSection("Undo Engine Section");

        // Assert
        Assert.That(updated.UndoDomainHistory, Has.Count.EqualTo(1));
        Assert.That(updated.RedoDomainHistory, Is.Empty);
        Assert.That(updated.UndoHistory, Has.Count.EqualTo(1));
        Assert.That(updated.UndoHistory.All(historyDocument => historyDocument.Sections.Count == 0), Is.True);
    }

    [Test]
    public void Undo_RestoresStateFromDomainHistory_WhenLegacyHistoryUsesPlaceholders()
    {
        // Arrange
        var sectionText = $"undo-domain-{Guid.NewGuid():N}";
        _service.AddSection(sectionText);

        // Act
        _service.Undo();
        var afterUndo = _service.Load();

        // Assert
        Assert.That(afterUndo.Document.Sections.Any(section => section.Text == sectionText), Is.False);
        Assert.That(afterUndo.RedoDomainHistory, Has.Count.EqualTo(1));
    }

    [Test]
    public void ExportXml_ContainsProtocolRootAndSections()
    {
        // Act
        var xml = _service.ExportXml();

        // Assert
        Assert.That(xml, Does.Contain("<Protocol>"));
        Assert.That(xml, Does.Contain("<Section>"));
        Assert.That(xml, Does.Contain("Diagnostic Polysomnogram:"));
    }

    [Test]
    public void ImportXml_ReplacesDocument_AndClearsHistory()
    {
        // Arrange
        _service.AddSection("Temp");
        var xml = """
                  <?xml version="1.0"?>
                  <Protocol>
                    <Id>-1</Id>
                    <LinkId>-1</LinkId>
                    <LinkText></LinkText>
                    <text>Imported Protocol</text>
                    <Section>
                      <Id>1</Id>
                      <LinkId>-1</LinkId>
                      <LinkText></LinkText>
                      <text>Imported Section</text>
                    </Section>
                  </Protocol>
                  """;

        // Act
        var snapshot = _service.ImportXml(xml);

        // Assert
        Assert.That(snapshot.Document.Text, Is.EqualTo("Imported Protocol"));
        Assert.That(snapshot.Document.Sections, Has.Count.EqualTo(1));
        Assert.That(snapshot.Document.Sections[0].Text, Is.EqualTo("Imported Section"));
        Assert.That(snapshot.UndoHistory, Is.Empty);
        Assert.That(snapshot.RedoHistory, Is.Empty);
    }

    private static bool ContainsNode(IEnumerable<ProtocolNodeModel> nodes, int nodeId)
    {
        return nodes.Any(node => node.Id == nodeId);
    }

    private static ProtocolEditorService CreateService(
        IProtocolEditorSessionStore sessionStore,
        IProtocolXmlService xmlService,
        IProtocolCommandHandler<AddSectionCommand> addSectionHandler,
        IProtocolCommandHandler<AddChildCommand> addChildHandler,
        IProtocolCommandHandler<RemoveNodeCommand> removeNodeHandler,
        IProtocolCommandHandler<UpdateNodeCommand> updateNodeHandler,
        IProtocolCommandHandler<MoveNodeCommand> moveNodeHandler,
        IProtocolCommandHandler<AddSubTextCommand> addSubTextHandler,
        IProtocolCommandHandler<RemoveSubTextCommand> removeSubTextHandler)
    {
        return new ProtocolEditorService(
            sessionStore,
            xmlService,
            addSectionHandler,
            addChildHandler,
            removeNodeHandler,
            updateNodeHandler,
            moveNodeHandler,
            addSubTextHandler,
            removeSubTextHandler,
            NullLogger<ProtocolEditorService>.Instance);
    }

    private static ProtocolEditorSnapshot CreateSnapshot()
    {
        var starter = new ProtocolStarterService(
            new ProtocolXmlService(NullLogger<ProtocolXmlService>.Instance),
            Options.Create(new ProtocolEditorStartupOptions()),
            NullLogger<ProtocolStarterService>.Instance);

        return new ProtocolEditorSnapshot
        {
            Document = starter.Create(),
            UndoHistory = [],
            RedoHistory = [],
            LastUpdatedUtc = DateTimeOffset.UtcNow
        };
    }

    private static IProtocolCommandHandler<TCommand> CreateNoOpHandler<TCommand>()
    {
        var handler = new Mock<IProtocolCommandHandler<TCommand>>();
        handler
            .Setup(x => x.Handle(It.IsAny<ProtocolTreeDocument>(), It.IsAny<TCommand>()))
            .Returns<ProtocolTreeDocument, TCommand>((document, _) => ProtocolResult<ProtocolTreeDocument>.Success(document));
        return handler.Object;
    }

    private sealed class InMemoryProtocolEditorSessionStore : IProtocolEditorSessionStore
    {
        private readonly IProtocolStarterService _starterService;
        private ProtocolEditorSnapshot _snapshot;

        public InMemoryProtocolEditorSessionStore(IProtocolStarterService starterService)
        {
            _starterService = starterService;
            _snapshot = CreateDefaultSnapshot();
        }

        public ProtocolEditorSnapshot Load()
        {
            return _snapshot;
        }

        public void Save(ProtocolEditorSnapshot snapshot)
        {
            _snapshot = snapshot;
        }

        public void Reset()
        {
            _snapshot = CreateDefaultSnapshot();
        }

        private ProtocolEditorSnapshot CreateDefaultSnapshot()
        {
            return new ProtocolEditorSnapshot
            {
                Document = _starterService.Create(),
                UndoHistory = [],
                RedoHistory = [],
                LastUpdatedUtc = DateTimeOffset.UtcNow
            };
        }
    }
}
