using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SleepEditWeb.Models;
using SleepEditWeb.Protocol.Domain;
using SleepEditWeb.Services;

namespace SleepEditWeb.Tests;

[TestFixture]
public class ProtocolTreeFunctionsTests
{
    [Test]
    public void AddChild_AddsNodeWithoutMutatingOriginalDocument()
    {
        // Arrange
        var source = CreateDomainDocument();
        var parentId = source.Sections[0].Id;
        var childText = $"domain-child-{Guid.NewGuid():N}";

        // Act
        var updated = ProtocolTreeFunctions.AddChild(source, parentId, childText);

        // Assert
        var originalParent = ProtocolTreeFunctions.FindNode(source, parentId);
        var updatedParent = ProtocolTreeFunctions.FindNode(updated, parentId);
        Assert.That(originalParent, Is.Not.Null);
        Assert.That(updatedParent, Is.Not.Null);
        Assert.That(originalParent!.Children.Any(node => node.Text == childText), Is.False);
        Assert.That(updatedParent!.Children.Any(node => node.Text == childText), Is.True);
    }

    [Test]
    public void RemoveNode_ClearsInboundLinksToRemovedNode()
    {
        // Arrange
        var source = CreateDomainDocument();
        var sourceId = source.Sections[0].Id;
        var targetId = source.Sections[1].Id;
        var linked = ProtocolTreeFunctions.UpdateNode(
            source,
            sourceId,
            source.Sections[0].Text,
            targetId,
            source.Sections[1].Text);

        // Act
        var removed = ProtocolTreeFunctions.RemoveNode(linked, targetId);
        var updatedSource = ProtocolTreeFunctions.FindNode(removed, sourceId);

        // Assert
        Assert.That(updatedSource, Is.Not.Null);
        Assert.That(updatedSource!.LinkId, Is.EqualTo(-1));
        Assert.That(updatedSource.LinkText, Is.EqualTo(string.Empty));
    }

    [Test]
    public void MoveNode_RejectsMoveIntoOwnDescendant()
    {
        // Arrange
        var source = CreateDomainDocument();
        var sectionId = source.Sections[0].Id;
        var descendantId = source.Sections[0].Children[0].Id;

        // Act
        var result = ProtocolTreeFunctions.MoveNode(source, sectionId, descendantId, 0);

        // Assert
        Assert.That(result, Is.SameAs(source));
    }

    [Test]
    public void AddSubText_ThenRemoveSubText_IsCaseInsensitive()
    {
        // Arrange
        var source = CreateDomainDocument();
        var nodeId = source.Sections[0].Id;
        var subTextValue = $"subtext-{Guid.NewGuid():N}";

        // Act
        var withSubText = ProtocolTreeFunctions.AddSubText(source, nodeId, $"  {subTextValue}  ");
        var withoutSubText = ProtocolTreeFunctions.RemoveSubText(withSubText, nodeId, subTextValue.ToUpperInvariant());
        var nodeWithSubText = ProtocolTreeFunctions.FindNode(withSubText, nodeId);
        var nodeWithoutSubText = ProtocolTreeFunctions.FindNode(withoutSubText, nodeId);

        // Assert
        Assert.That(nodeWithSubText, Is.Not.Null);
        Assert.That(nodeWithoutSubText, Is.Not.Null);
        Assert.That(nodeWithSubText!.SubText, Does.Contain(subTextValue));
        Assert.That(nodeWithoutSubText!.SubText.Any(value => value.Equals(subTextValue, StringComparison.OrdinalIgnoreCase)), Is.False);
    }

    [Test]
    public void Mapper_RoundTrip_PreservesProtocolTreeShape()
    {
        // Arrange
        var mutable = CreateMutableDocument();

        // Act
        var domain = ProtocolTreeMapper.ToDomain(mutable);
        var roundTripped = ProtocolTreeMapper.ToMutable(domain);

        // Assert
        Assert.That(roundTripped.Text, Is.EqualTo(mutable.Text));
        Assert.That(roundTripped.Sections.Count, Is.EqualTo(mutable.Sections.Count));
        Assert.That(roundTripped.Sections[0].Text, Is.EqualTo(mutable.Sections[0].Text));
        Assert.That(roundTripped.Sections[0].Children[0].Text, Is.EqualTo(mutable.Sections[0].Children[0].Text));
    }

    private static ProtocolTreeDocument CreateDomainDocument()
    {
        return ProtocolTreeMapper.ToDomain(CreateMutableDocument());
    }

    private static ProtocolDocument CreateMutableDocument()
    {
        var starter = new ProtocolStarterService(
            new ProtocolXmlService(NullLogger<ProtocolXmlService>.Instance),
            Options.Create(new ProtocolEditorStartupOptions()),
            NullLogger<ProtocolStarterService>.Instance);

        return starter.Create();
    }
}
