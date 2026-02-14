using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SleepEditWeb.Controllers;
using SleepEditWeb.Models;
using SleepEditWeb.Services;

namespace SleepEditWeb.Tests;

[TestFixture]
public class ProtocolEditorControllerTests
{
    [Test]
    public void SaveXml_WithEmptyConfiguredPaths_UsesDeterministicFallbackPath()
    {
        // Arrange
        var service = CreateServiceMock();
        var controller = CreateController(service.Object, new ProtocolEditorStartupOptions());
        var expectedFallbackPath = Path.Combine(AppContext.BaseDirectory, "Data", "protocols", "default-protocol.xml");

        try
        {
            // Act
            var result = controller.SaveXml() as JsonResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(GetPropertyValue(result!.Value, "savedPath"), Is.EqualTo(expectedFallbackPath));
            Assert.That(File.Exists(expectedFallbackPath), Is.True);
        }
        finally
        {
            if (File.Exists(expectedFallbackPath))
            {
                File.Delete(expectedFallbackPath);
            }
        }
    }

    [Test]
    public void SetDefaultProtocol_WithEmptyConfiguredPaths_UsesDefaultProtocolFallbackPath()
    {
        // Arrange
        var service = CreateServiceMock();
        var controller = CreateController(service.Object, new ProtocolEditorStartupOptions());
        var expectedFallbackPath = Path.Combine(AppContext.BaseDirectory, "Data", "protocols", "default-protocol.xml");

        try
        {
            // Act
            var result = controller.SetDefaultProtocol() as JsonResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(GetPropertyValue(result!.Value, "defaultPath"), Is.EqualTo(expectedFallbackPath));
            Assert.That(File.Exists(expectedFallbackPath), Is.True);
        }
        finally
        {
            if (File.Exists(expectedFallbackPath))
            {
                File.Delete(expectedFallbackPath);
            }
        }
    }

    [Test]
    public void ImportXml_WhenPathDoesNotExist_ReturnsBadRequestWithPath()
    {
        // Arrange
        var service = CreateServiceMock();
        var controller = CreateController(service.Object, new ProtocolEditorStartupOptions());
        var missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xml");

        // Act
        var result = controller.ImportXml(new ProtocolEditorController.ImportXmlRequest { Path = missingPath }) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(GetPropertyValue(result!.Value, "error"), Is.EqualTo("Import XML file was not found."));
        Assert.That(GetPropertyValue(result.Value, "path"), Is.EqualTo(missingPath));
    }

    [Test]
    public async Task ImportXmlUpload_WithMissingFile_ReturnsBadRequest()
    {
        // Arrange
        var service = CreateServiceMock();
        var controller = CreateController(service.Object, new ProtocolEditorStartupOptions());

        // Act
        var result = await controller.ImportXmlUpload(null) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(GetPropertyValue(result!.Value, "error"), Is.EqualTo("No XML file was uploaded."));
    }


    [Test]
    public void SaveXml_WhenExportFails_ReturnsServerErrorPayload()
    {
        // Arrange
        var service = CreateServiceMock();
        service.Setup(x => x.ExportXml()).Throws(new IOException("disk full"));
        var controller = CreateController(service.Object, new ProtocolEditorStartupOptions());

        // Act
        var result = controller.SaveXml() as ObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(500));
        Assert.That(GetPropertyValue(result.Value, "error"), Is.EqualTo("Failed to save XML to the configured path."));
    }

    [Test]
    public void SetDefaultProtocol_WhenExportFails_ReturnsServerErrorPayload()
    {
        // Arrange
        var service = CreateServiceMock();
        service.Setup(x => x.ExportXml()).Throws(new UnauthorizedAccessException("no write access"));
        var controller = CreateController(service.Object, new ProtocolEditorStartupOptions());

        // Act
        var result = controller.SetDefaultProtocol() as ObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(500));
        Assert.That(GetPropertyValue(result.Value, "error"), Is.EqualTo("Failed to set default protocol."));
    }

    [Test]
    public void ImportXml_WhenServiceThrowsFormatException_ReturnsBadRequestInvalidFormat()
    {
        // Arrange
        var service = CreateServiceMock();
        service.Setup(x => x.ImportXml(It.IsAny<string>())).Throws(new FormatException("invalid xml"));
        var controller = CreateController(service.Object, new ProtocolEditorStartupOptions());
        var importPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xml");

        try
        {
            File.WriteAllText(importPath, "<not-a-valid-protocol />");

            // Act
            var result = controller.ImportXml(new ProtocolEditorController.ImportXmlRequest { Path = importPath }) as BadRequestObjectResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(GetPropertyValue(result!.Value, "error"), Is.EqualTo("Invalid XML format for protocol import."));
        }
        finally
        {
            if (File.Exists(importPath))
            {
                File.Delete(importPath);
            }
        }
    }

    [Test]
    public async Task ImportXmlUpload_WhenFileTooLarge_ReturnsBadRequest()
    {
        // Arrange
        var service = CreateServiceMock();
        var controller = CreateController(service.Object, new ProtocolEditorStartupOptions());
        var file = new Mock<IFormFile>();
        file.SetupGet(x => x.FileName).Returns("large.xml");
        file.SetupGet(x => x.Length).Returns((10L * 1024L * 1024L) + 1L);

        // Act
        var result = await controller.ImportXmlUpload(file.Object) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(GetPropertyValue(result!.Value, "error"), Is.EqualTo("Uploaded XML file is too large."));
    }


    [Test]
    public async Task ImportXmlUpload_WithoutConfiguredSavePath_UsesUploadedFileNameFallbackPath()
    {
        // Arrange
        var service = CreateServiceMock();
        var snapshot = new ProtocolEditorSnapshot
        {
            Document = new ProtocolDocument
            {
                Id = -1,
                LinkId = -1,
                LinkText = string.Empty,
                Text = "Imported Protocol",
                Sections = []
            },
            UndoHistory = [],
            RedoHistory = [],
            LastUpdatedUtc = DateTimeOffset.UtcNow
        };
        service.Setup(x => x.ImportXml(It.IsAny<string>())).Returns(snapshot);

        var controller = CreateController(service.Object, new ProtocolEditorStartupOptions());
        var fileName = $"{Guid.NewGuid():N}-import.xml";
        var xmlContent = System.Text.Encoding.UTF8.GetBytes("<Protocol><Id>-1</Id><LinkId>-1</LinkId><LinkText></LinkText><text>Imported</text></Protocol>");
        await using var stream = new MemoryStream(xmlContent);
        IFormFile file = new FormFile(stream, 0, xmlContent.Length, "file", fileName);

        var expectedPath = Path.Combine(AppContext.BaseDirectory, "Data", "protocols", fileName);
        var defaultPath = Path.Combine(AppContext.BaseDirectory, "Data", "protocols", "default-protocol.xml");

        try
        {
            // Act
            var result = await controller.ImportXmlUpload(file) as JsonResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(GetPropertyValue(result!.Value, "savedPath"), Is.EqualTo(expectedPath));
            Assert.That(expectedPath, Is.Not.EqualTo(defaultPath));
            Assert.That(File.Exists(expectedPath), Is.True);
        }
        finally
        {
            if (File.Exists(expectedPath))
            {
                File.Delete(expectedPath);
            }
        }
    }

    private static ProtocolEditorController CreateController(
        IProtocolEditorService service,
        ProtocolEditorStartupOptions startupOptions)
    {
        return new ProtocolEditorController(
            service,
            Options.Create(new ProtocolEditorFeatureOptions { ProtocolEditorEnabled = true }),
            Options.Create(startupOptions),
            NullLogger<ProtocolEditorController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private static Mock<IProtocolEditorService> CreateServiceMock()
    {
        var snapshot = new ProtocolEditorSnapshot
        {
            Document = new ProtocolDocument
            {
                Id = -1,
                LinkId = -1,
                LinkText = string.Empty,
                Text = "Test Protocol",
                Sections = []
            },
            UndoHistory = [],
            RedoHistory = [],
            LastUpdatedUtc = DateTimeOffset.UtcNow
        };

        var service = new Mock<IProtocolEditorService>();
        service.Setup(x => x.ExportXml()).Returns("<Protocol><Id>-1</Id><LinkId>-1</LinkId><LinkText></LinkText><text>Test Protocol</text></Protocol>");
        service.Setup(x => x.Load()).Returns(snapshot);
        return service;
    }

    private static object? GetPropertyValue(object? obj, string propertyName)
    {
        return obj?.GetType().GetProperty(propertyName)?.GetValue(obj, null);
    }
}
