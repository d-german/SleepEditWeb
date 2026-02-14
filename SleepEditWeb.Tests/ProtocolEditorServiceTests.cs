using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SleepEditWeb.Models;
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
            new ProtocolXmlService(),
            Options.Create(new ProtocolEditorStartupOptions()),
            NullLogger<ProtocolStarterService>.Instance));
        _service = new ProtocolEditorService(_sessionStore, new ProtocolXmlService());
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
    public void ExportXml_ContainsProtocolRootAndSections()
    {
        // Act
        var xml = _service.ExportXml();

        // Assert
        Assert.That(xml, Does.Contain("<Protocol>"));
        Assert.That(xml, Does.Contain("<Section>"));
        Assert.That(xml, Does.Contain("Diagnostic Polysomnogram:"));
    }

    private static bool ContainsNode(IEnumerable<ProtocolNodeModel> nodes, int nodeId)
    {
        return nodes.Any(node => node.Id == nodeId);
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
