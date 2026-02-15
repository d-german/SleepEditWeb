using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SleepEditWeb.Application.Protocol;
using SleepEditWeb.Application.Protocol.Commands;
using SleepEditWeb.Application.Protocol.Queries;
using SleepEditWeb.Models;
using SleepEditWeb.Protocol.Domain;
using SleepEditWeb.Services;

namespace SleepEditWeb.Tests;

[TestFixture]
public class ProtocolApplicationHandlersTests
{
    [Test]
    public void AddChildHandler_ReturnsFailure_WhenParentMissing()
    {
        // Arrange
        var handler = new AddChildCommandHandler();
        var document = CreateDomainDocument();

        // Act
        var result = handler.Handle(document, new AddChildCommand(999999, "Unreachable child"));

        // Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo("parent_not_found"));
    }

    [Test]
    public void UpdateNodeHandler_UpdatesLinkAndText()
    {
        // Arrange
        var handler = new UpdateNodeCommandHandler();
        var document = CreateDomainDocument();
        var source = document.Sections[0];
        var target = document.Sections[1];

        // Act
        var result = handler.Handle(
            document,
            new UpdateNodeCommand(source.Id, "Updated Section Text", target.Id, target.Text));

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        var updatedNode = ProtocolTreeFunctions.FindNode(result.Value, source.Id);
        Assert.That(updatedNode, Is.Not.Null);
        Assert.That(updatedNode!.Text, Is.EqualTo("Updated Section Text"));
        Assert.That(updatedNode.LinkId, Is.EqualTo(target.Id));
        Assert.That(updatedNode.LinkText, Is.EqualTo(target.Text));
    }

    [Test]
    public void MoveNodeHandler_ReturnsFailure_ForInvalidMove()
    {
        // Arrange
        var handler = new MoveNodeCommandHandler();
        var document = CreateDomainDocument();
        var sectionId = document.Sections[0].Id;
        var childId = document.Sections[0].Children[0].Id;

        // Act
        var result = handler.Handle(document, new MoveNodeCommand(sectionId, childId, 0));

        // Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo("invalid_move"));
    }

    [Test]
    public void FindNodeByIdQueryHandler_ReturnsFailure_ForUnknownNode()
    {
        // Arrange
        var handler = new FindNodeByIdQueryHandler();
        var document = CreateDomainDocument();

        // Act
        var result = handler.Handle(document, new FindNodeByIdQuery(-12345));

        // Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo("node_not_found"));
    }

    [Test]
    public void ResultPipeline_MapBindTap_ComposesSuccessfulFlow()
    {
        // Arrange
        var tapped = 0;
        var initial = ProtocolResult<int>.Success(5);

        // Act
        var result = initial
            .Map(value => value + 2)
            .Tap(value => tapped = value)
            .Bind(value => ProtocolResult<int>.Success(value * 3));

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.EqualTo(21));
        Assert.That(tapped, Is.EqualTo(7));
    }

    [Test]
    public void ResultPipeline_MapBindTap_SkipsWhenResultIsFailure()
    {
        // Arrange
        var tapped = 0;
        var initial = ProtocolResult<int>.Failure("boom", "failed");

        // Act
        var result = initial
            .Map(value => value + 2)
            .Tap(value => tapped = value)
            .Bind(value => ProtocolResult<int>.Success(value * 3));

        // Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo("boom"));
        Assert.That(tapped, Is.EqualTo(0));
    }

    private static ProtocolTreeDocument CreateDomainDocument()
    {
        var starter = new ProtocolStarterService(
            new ProtocolXmlService(NullLogger<ProtocolXmlService>.Instance),
            Options.Create(new ProtocolEditorStartupOptions()),
            NullLogger<ProtocolStarterService>.Instance);

        return ProtocolTreeMapper.ToDomain(starter.Create());
    }
}
