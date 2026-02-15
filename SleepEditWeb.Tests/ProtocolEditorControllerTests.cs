using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SleepEditWeb.Controllers;
using SleepEditWeb.Infrastructure.ProtocolPersistence;
using SleepEditWeb.Models;
using SleepEditWeb.Services;
using SleepEditWeb.Web.ProtocolEditor;

namespace SleepEditWeb.Tests;

[TestFixture]
public class ProtocolEditorControllerTests
{
    [Test]
    public void State_ReturnsCanonicalStatePayloadShape()
    {
        // Arrange
        var snapshot = new ProtocolEditorSnapshot
        {
            Document = new ProtocolDocument
            {
                Id = 42,
                LinkId = -1,
                LinkText = string.Empty,
                Text = "Canonical Protocol",
                Sections = []
            },
            UndoHistory = [new ProtocolDocument(), new ProtocolDocument()],
            RedoHistory = [new ProtocolDocument()],
            LastUpdatedUtc = new DateTimeOffset(2026, 2, 15, 12, 30, 0, TimeSpan.Zero)
        };
        var service = new Mock<IProtocolEditorService>();
        service.Setup(x => x.Load()).Returns(snapshot);
        var controller = CreateController(service.Object, new ProtocolEditorStartupOptions());

        // Act
        var result = controller.State() as JsonResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(GetPropertyValue(result!.Value, "document"), Is.SameAs(snapshot.Document));
        Assert.That(GetPropertyValue(result.Value, "undoCount"), Is.EqualTo(2));
        Assert.That(GetPropertyValue(result.Value, "redoCount"), Is.EqualTo(1));
        Assert.That(GetPropertyValue(result.Value, "lastUpdatedUtc"), Is.EqualTo(snapshot.LastUpdatedUtc));
    }

    [Test]
    public void UpdateNode_ForwardsLinkPayloadToService()
    {
        // Arrange
        var service = CreateServiceMock();
        var snapshot = (ProtocolEditorSnapshot)service.Object.Load();
        var request = new ProtocolEditorController.UpdateNodeRequest
        {
            NodeId = 14,
            Text = "Updated text",
            LinkId = 22,
            LinkText = "Linked target"
        };
        service.Setup(x => x.UpdateNode(request.NodeId, request.Text!, request.LinkId, request.LinkText!)).Returns(snapshot);
        var controller = CreateController(service.Object, new ProtocolEditorStartupOptions());

        // Act
        var result = controller.UpdateNode(request) as JsonResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        service.Verify(x => x.UpdateNode(request.NodeId, request.Text!, request.LinkId, request.LinkText!), Times.Once);
        Assert.That(GetPropertyValue(result!.Value, "document"), Is.Not.Null);
    }

    [Test]
    public void AddSubText_ForwardsPayloadToService()
    {
        // Arrange
        var service = CreateServiceMock();
        var snapshot = (ProtocolEditorSnapshot)service.Object.Load();
        var request = new ProtocolEditorController.SubTextRequest { NodeId = 9, Value = "New SubText" };
        service.Setup(x => x.AddSubText(request.NodeId, request.Value!)).Returns(snapshot);
        var controller = CreateController(service.Object, new ProtocolEditorStartupOptions());

        // Act
        var result = controller.AddSubText(request) as JsonResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        service.Verify(x => x.AddSubText(request.NodeId, request.Value!), Times.Once);
        Assert.That(GetPropertyValue(result!.Value, "undoCount"), Is.Not.Null);
    }

    [Test]
    public void RemoveSubText_ForwardsPayloadToService()
    {
        // Arrange
        var service = CreateServiceMock();
        var snapshot = (ProtocolEditorSnapshot)service.Object.Load();
        var request = new ProtocolEditorController.SubTextRequest { NodeId = 9, Value = "Old SubText" };
        service.Setup(x => x.RemoveSubText(request.NodeId, request.Value!)).Returns(snapshot);
        var controller = CreateController(service.Object, new ProtocolEditorStartupOptions());

        // Act
        var result = controller.RemoveSubText(request) as JsonResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        service.Verify(x => x.RemoveSubText(request.NodeId, request.Value!), Times.Once);
        Assert.That(GetPropertyValue(result!.Value, "redoCount"), Is.Not.Null);
    }

    [Test]
    public void ExportXml_ReturnsXmlContentTypeAndBody()
    {
        // Arrange
        var service = CreateServiceMock();
        service.Setup(x => x.ExportXml()).Returns("<Protocol><Id>-1</Id></Protocol>");
        var controller = CreateController(service.Object, new ProtocolEditorStartupOptions());

        // Act
        var result = controller.ExportXml() as ContentResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ContentType, Is.EqualTo("application/xml"));
        Assert.That(result.Content, Is.EqualTo("<Protocol><Id>-1</Id></Protocol>"));
    }

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
    public void SaveXml_PersistsProtocolVersion_WhenRepositoryAvailable()
    {
        // Arrange
        var service = CreateServiceMock();
        var repository = CreateRepositoryMock();
        var savePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-protocol-save.xml");
        var controller = CreateController(
            service.Object,
            new ProtocolEditorStartupOptions { SaveProtocolPath = savePath },
            repository);

        try
        {
            // Act
            var result = controller.SaveXml() as JsonResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            repository.Verify(
                x => x.SaveVersion(It.IsAny<ProtocolDocument>(), "SaveXml", savePath),
                Times.Once);
        }
        finally
        {
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }
        }
    }

    [Test]
    public void SaveXml_WhenRepositorySaveFails_StillReturnsSuccessPayload()
    {
        // Arrange
        var service = CreateServiceMock();
        var repository = CreateRepositoryMock();
        repository
            .Setup(x => x.SaveVersion(It.IsAny<ProtocolDocument>(), It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new IOException("database unavailable"));

        var savePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-protocol-save.xml");
        var controller = CreateController(
            service.Object,
            new ProtocolEditorStartupOptions { SaveProtocolPath = savePath },
            repository);

        try
        {
            // Act
            var result = controller.SaveXml() as JsonResult;

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(GetPropertyValue(result!.Value, "savedPath"), Is.EqualTo(savePath));
        }
        finally
        {
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
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
        ProtocolEditorStartupOptions startupOptions,
        Mock<IProtocolRepository>? repositoryMock = null)
    {
        var repository = repositoryMock ?? CreateRepositoryMock();
        var pathPolicy = new ProtocolEditorPathPolicy(Options.Create(startupOptions));
        var fileStore = new ProtocolEditorFileStore();
        var requestValidator = new ProtocolEditorRequestValidator();
        var responseMapper = new ProtocolEditorResponseMapper();

        return new ProtocolEditorController(
            service,
            Options.Create(new ProtocolEditorFeatureOptions { ProtocolEditorEnabled = true }),
            pathPolicy,
            fileStore,
            repository.Object,
            requestValidator,
            responseMapper,
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
        service.Setup(x => x.AddSection(It.IsAny<string>())).Returns(snapshot);
        service.Setup(x => x.AddChild(It.IsAny<int>(), It.IsAny<string>())).Returns(snapshot);
        service.Setup(x => x.RemoveNode(It.IsAny<int>())).Returns(snapshot);
        service.Setup(x => x.UpdateNode(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>())).Returns(snapshot);
        service.Setup(x => x.MoveNode(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())).Returns(snapshot);
        service.Setup(x => x.AddSubText(It.IsAny<int>(), It.IsAny<string>())).Returns(snapshot);
        service.Setup(x => x.RemoveSubText(It.IsAny<int>(), It.IsAny<string>())).Returns(snapshot);
        service.Setup(x => x.Undo()).Returns(snapshot);
        service.Setup(x => x.Redo()).Returns(snapshot);
        service.Setup(x => x.Reset()).Returns(snapshot);
        service.Setup(x => x.ImportXml(It.IsAny<string>())).Returns(snapshot);
        return service;
    }

    private static Mock<IProtocolRepository> CreateRepositoryMock()
    {
        var repository = new Mock<IProtocolRepository>();
        repository
            .Setup(x => x.SaveVersion(It.IsAny<ProtocolDocument>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns<ProtocolDocument, string, string>((document, source, note) =>
                new ProtocolVersion(Guid.NewGuid(), DateTime.UtcNow, source, note, document));
        repository
            .Setup(x => x.GetLatestVersion())
            .Returns((ProtocolVersion?)null);
        repository
            .Setup(x => x.ListVersions(It.IsAny<int>()))
            .Returns(Array.Empty<ProtocolVersion>());
        return repository;
    }

    private static object? GetPropertyValue(object? obj, string propertyName)
    {
        return obj?.GetType().GetProperty(propertyName)?.GetValue(obj, null);
    }
}
