using SleepEditWeb.Models;
using SleepEditWeb.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace SleepEditWeb.Tests;

[TestFixture]
public class ProtocolXmlServiceTests
{
    private ProtocolXmlService _service;

    [SetUp]
    public void SetUp()
    {
        _service = new ProtocolXmlService(NullLogger<ProtocolXmlService>.Instance);
    }

    [Test]
    public void Serialize_WritesExpectedHierarchyAndFieldOrder()
    {
        // Arrange
        var document = new ProtocolDocument
        {
            Id = 0,
            LinkId = -1,
            LinkText = string.Empty,
            Text = "Starter",
            Sections =
            [
                new ProtocolNodeModel
                {
                    Id = 1,
                    LinkId = -1,
                    LinkText = string.Empty,
                    Text = "Diagnostic Polysomnogram:",
                    Kind = ProtocolNodeKind.Section,
                    SubText = [],
                    Children =
                    [
                        new ProtocolNodeModel
                        {
                            Id = 2,
                            LinkId = 1,
                            LinkText = "Diagnostic Polysomnogram:",
                            Text = "Child statement",
                            Kind = ProtocolNodeKind.SubSection,
                            SubText = [ "option a" ],
                            Children = []
                        }
                    ]
                }
            ]
        };

        // Act
        var xml = _service.Serialize(document);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(xml, Does.Contain("<Protocol>"));
            Assert.That(xml, Does.Contain("<Section>"));
            Assert.That(xml, Does.Contain("<SubSection>"));
            Assert.That(xml, Does.Contain("<SubText>option a</SubText>"));
        });

        var sectionStart = xml.IndexOf("<Section>", StringComparison.Ordinal);
        var idIndex = xml.IndexOf("<Id>", sectionStart, StringComparison.Ordinal);
        var linkIdIndex = xml.IndexOf("<LinkId>", sectionStart, StringComparison.Ordinal);
        var linkTextIndex = xml.IndexOf("<LinkText>", sectionStart, StringComparison.Ordinal);
        var textIndex = xml.IndexOf("<text>", sectionStart, StringComparison.Ordinal);

        Assert.That(idIndex, Is.LessThan(linkIdIndex));
        Assert.That(linkIdIndex, Is.LessThan(linkTextIndex));
        Assert.That(linkTextIndex, Is.LessThan(textIndex));
    }

    [Test]
    public void Deserialize_RoundTripsDocumentWithoutLosingStructure()
    {
        // Arrange
        var source = new ProtocolDocument
        {
            Id = 0,
            LinkId = -1,
            LinkText = string.Empty,
            Text = "Saint Luke's Protocol",
            Sections =
            [
                new ProtocolNodeModel
                {
                    Id = 1,
                    LinkId = -1,
                    LinkText = string.Empty,
                    Text = "Diagnostic Polysomnogram:",
                    Kind = ProtocolNodeKind.Section,
                    SubText = [],
                    Children =
                    [
                        new ProtocolNodeModel
                        {
                            Id = 2,
                            LinkId = 5,
                            LinkText = "BiPAP Titration Polysomnogram:",
                            Text = "SpO2 drops below 50%-GOTO BiPAP Titration",
                            Kind = ProtocolNodeKind.SubSection,
                            SubText = [ "edit" ],
                            Children =
                            [
                                new ProtocolNodeModel
                                {
                                    Id = 3,
                                    LinkId = -1,
                                    LinkText = string.Empty,
                                    Text = "PaCO2 > 52",
                                    Kind = ProtocolNodeKind.SubSection,
                                    SubText = [],
                                    Children = []
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        // Act
        var xml = _service.Serialize(source);
        var hydrated = _service.Deserialize(xml);

        // Assert
        Assert.That(hydrated.Text, Is.EqualTo(source.Text));
        Assert.That(hydrated.Sections.Count, Is.EqualTo(1));
        Assert.That(hydrated.Sections[0].Text, Is.EqualTo("Diagnostic Polysomnogram:"));
        Assert.That(hydrated.Sections[0].Children.Count, Is.EqualTo(1));
        Assert.That(hydrated.Sections[0].Children[0].SubText, Has.Count.EqualTo(1));
        Assert.That(hydrated.Sections[0].Children[0].Children[0].Text, Is.EqualTo("PaCO2 > 52"));
    }
}
