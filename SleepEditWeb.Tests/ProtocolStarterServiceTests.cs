using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SleepEditWeb.Models;
using SleepEditWeb.Services;

namespace SleepEditWeb.Tests;

[TestFixture]
public class ProtocolStarterServiceTests
{
    [Test]
    public void Create_UsesConfiguredStartupProtocolFile_WhenPresent()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xml");
        File.WriteAllText(tempPath, """
<?xml version="1.0"?>
<Protocol>
  <Id>-1</Id>
  <LinkId>-1</LinkId>
  <LinkText></LinkText>
  <text>Configured Protocol</text>
  <Section>
    <Id>1</Id>
    <LinkId>-1</LinkId>
    <LinkText></LinkText>
    <text>Configured Section</text>
  </Section>
</Protocol>
""");

        try
        {
            var service = new ProtocolStarterService(
                new ProtocolXmlService(),
                Options.Create(new ProtocolEditorStartupOptions { StartupProtocolPath = tempPath }),
                NullLogger<ProtocolStarterService>.Instance);

            // Act
            var result = service.Create();

            // Assert
            Assert.That(result.Text, Is.EqualTo("Configured Protocol"));
            Assert.That(result.Sections, Has.Count.EqualTo(1));
            Assert.That(result.Sections[0].Text, Is.EqualTo("Configured Section"));
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Test]
    public void Create_FallsBackToSeededProtocol_WhenConfiguredFileIsMissing()
    {
        // Arrange
        var missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xml");
        var service = new ProtocolStarterService(
            new ProtocolXmlService(),
            Options.Create(new ProtocolEditorStartupOptions { StartupProtocolPath = missingPath }),
            NullLogger<ProtocolStarterService>.Instance);

        // Act
        var result = service.Create();

        // Assert
        Assert.That(result.Text, Is.EqualTo("Saint Luke's Protocol"));
        Assert.That(result.Sections.Any(section => section.Text == "Diagnostic Polysomnogram:"), Is.True);
    }

    [Test]
    public void Create_PrefersDefaultProtocolPath_OverStartupProtocolPath()
    {
        // Arrange
        var defaultPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-default.xml");
        var startupPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-startup.xml");

        File.WriteAllText(defaultPath, """
<?xml version="1.0"?>
<Protocol>
  <Id>-1</Id>
  <LinkId>-1</LinkId>
  <LinkText></LinkText>
  <text>Default Protocol</text>
  <Section>
    <Id>1</Id>
    <LinkId>-1</LinkId>
    <LinkText></LinkText>
    <text>Default Section</text>
  </Section>
</Protocol>
""");

        File.WriteAllText(startupPath, """
<?xml version="1.0"?>
<Protocol>
  <Id>-1</Id>
  <LinkId>-1</LinkId>
  <LinkText></LinkText>
  <text>Startup Protocol</text>
  <Section>
    <Id>1</Id>
    <LinkId>-1</LinkId>
    <LinkText></LinkText>
    <text>Startup Section</text>
  </Section>
</Protocol>
""");

        try
        {
            var service = new ProtocolStarterService(
                new ProtocolXmlService(),
                Options.Create(new ProtocolEditorStartupOptions
                {
                    DefaultProtocolPath = defaultPath,
                    StartupProtocolPath = startupPath
                }),
                NullLogger<ProtocolStarterService>.Instance);

            // Act
            var result = service.Create();

            // Assert
            Assert.That(result.Text, Is.EqualTo("Default Protocol"));
            Assert.That(result.Sections[0].Text, Is.EqualTo("Default Section"));
        }
        finally
        {
            if (File.Exists(defaultPath))
            {
                File.Delete(defaultPath);
            }

            if (File.Exists(startupPath))
            {
                File.Delete(startupPath);
            }
        }
    }
}
