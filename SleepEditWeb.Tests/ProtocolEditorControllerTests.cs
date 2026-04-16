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
        var controller = CreateController(service.Object);

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
        var controller = CreateController(service.Object);

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
        var controller = CreateController(service.Object);

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
        var controller = CreateController(service.Object);

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
        var controller = CreateController(service.Object);

        // Act
        var result = controller.ExportXml() as ContentResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ContentType, Is.EqualTo("application/xml"));
        Assert.That(result.Content, Is.EqualTo("<Protocol><Id>-1</Id></Protocol>"));
    }

    [Test]
    public void SaveXml_PersistsToRepository()
    {
        // Arrange
        var service = CreateServiceMock();
        var repository = CreateRepositoryMock();
        var controller = CreateController(service.Object, repository);

        // Act
        var result = controller.SaveXml() as JsonResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        repository.Verify(
            x => x.SaveCurrentProtocol(It.IsAny<ProtocolDocument>(), "SaveXml"),
            Times.Once);
        Assert.That(GetPropertyValue(result!.Value, "document"), Is.Not.Null);
    }

    [Test]
    public void SaveXml_WhenRepositoryPersistenceFails_ReturnsInternalServerError()
    {
        // Arrange
        var service = CreateServiceMock();
        var repository = CreateRepositoryMock();
        repository
            .Setup(x => x.SaveCurrentProtocol(It.IsAny<ProtocolDocument>(), "SaveXml"))
            .Throws(new IOException("disk failure"));
        var controller = CreateController(service.Object, repository);

        // Act
        var result = controller.SaveXml() as ObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(500));
        Assert.That(GetPropertyValue(result.Value, "error"), Is.EqualTo("Failed to save protocol."));
    }

    [Test]
    public void SetDefaultProtocol_PersistsToRepository()
    {
        // Arrange
        var service = CreateServiceMock();
        var repository = CreateRepositoryMock();
        var controller = CreateController(service.Object, repository);

        // Act
        var result = controller.SetDefaultProtocol() as JsonResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        repository.Verify(
            x => x.SaveCurrentProtocol(It.IsAny<ProtocolDocument>(), "SetDefaultProtocol"),
            Times.Once);
        Assert.That(GetPropertyValue(result!.Value, "document"), Is.Not.Null);
    }

    [Test]
    public void SetDefaultProtocol_WhenRepositoryPersistenceFails_ReturnsInternalServerError()
    {
        // Arrange
        var service = CreateServiceMock();
        var repository = CreateRepositoryMock();
        repository
            .Setup(x => x.SaveCurrentProtocol(It.IsAny<ProtocolDocument>(), "SetDefaultProtocol"))
            .Throws(new IOException("disk failure"));
        var controller = CreateController(service.Object, repository);

        // Act
        var result = controller.SetDefaultProtocol() as ObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(500));
        Assert.That(GetPropertyValue(result.Value, "error"), Is.EqualTo("Failed to set default protocol."));
    }

    [Test]
    public async Task ImportXmlUpload_WithMissingFile_ReturnsBadRequest()
    {
        // Arrange
        var service = CreateServiceMock();
        var controller = CreateController(service.Object);

        // Act
        var result = await controller.ImportXmlUpload(null) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(GetPropertyValue(result!.Value, "error"), Is.EqualTo("No XML file was uploaded."));
    }

    [Test]
    public async Task ImportXmlUpload_WhenFileTooLarge_ReturnsBadRequest()
    {
        // Arrange
        var service = CreateServiceMock();
        var controller = CreateController(service.Object);
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
    public async Task ImportXmlUpload_PersistsImportedProtocolToRepository()
    {
        // Arrange
        var service = CreateServiceMock();
        var repository = CreateRepositoryMock();
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

        var controller = CreateController(service.Object, repository);
        var xmlContent = System.Text.Encoding.UTF8.GetBytes("<Protocol><Id>-1</Id><LinkId>-1</LinkId><LinkText></LinkText><text>Imported</text></Protocol>");
        await using var stream = new MemoryStream(xmlContent);
        IFormFile file = new FormFile(stream, 0, xmlContent.Length, "file", "import.xml");

        // Act
        var result = await controller.ImportXmlUpload(file) as JsonResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        repository.Verify(
            x => x.SaveCurrentProtocol(It.IsAny<ProtocolDocument>(), "ImportXmlUpload"),
            Times.Once);
        Assert.That(GetPropertyValue(result!.Value, "document"), Is.Not.Null);
    }

    [Test]
    public async Task ImportXmlUpload_WhenUploadReadFails_ReturnsInternalServerError()
    {
        // Arrange
        var service = CreateServiceMock();
        var controller = CreateController(service.Object);
        var file = new Mock<IFormFile>();
        file.SetupGet(x => x.FileName).Returns("broken.xml");
        file.SetupGet(x => x.Length).Returns(32);
        file.Setup(x => x.OpenReadStream()).Throws(new IOException("stream failure"));

        // Act
        var result = await controller.ImportXmlUpload(file.Object) as ObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(500));
        Assert.That(GetPropertyValue(result.Value, "error"), Is.EqualTo("Failed to import uploaded XML."));
    }

    [Test]
    public async Task ImportXmlUpload_WhenRepositoryPersistenceFails_ReturnsInternalServerError()
    {
        // Arrange
        var service = CreateServiceMock();
        var repository = CreateRepositoryMock();
        repository
            .Setup(x => x.SaveCurrentProtocol(It.IsAny<ProtocolDocument>(), "ImportXmlUpload"))
            .Throws(new IOException("disk failure"));
        var controller = CreateController(service.Object, repository);

        var xmlContent = System.Text.Encoding.UTF8.GetBytes("<Protocol><Id>-1</Id><LinkId>-1</LinkId><LinkText></LinkText><text>Imported</text></Protocol>");
        await using var stream = new MemoryStream(xmlContent);
        IFormFile file = new FormFile(stream, 0, xmlContent.Length, "file", "import.xml");

        // Act
        var result = await controller.ImportXmlUpload(file) as ObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(500));
        Assert.That(GetPropertyValue(result.Value, "error"), Is.EqualTo("Failed to import uploaded XML."));
    }

    [Test]
    public async Task ImportXmlUpload_WhenFormatException_ReturnsBadRequest()
    {
        // Arrange
        var service = CreateServiceMock();
        service.Setup(x => x.ImportXml(It.IsAny<string>())).Throws(new FormatException("invalid xml"));
        var controller = CreateController(service.Object);

        var xmlContent = System.Text.Encoding.UTF8.GetBytes("<not-valid />");
        await using var stream = new MemoryStream(xmlContent);
        IFormFile file = new FormFile(stream, 0, xmlContent.Length, "file", "bad.xml");

        // Act
        var result = await controller.ImportXmlUpload(file) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(GetPropertyValue(result!.Value, "error"), Is.EqualTo("Invalid XML format for protocol import."));
    }

    [Test]
    public void CreateProtocol_ReturnsMetadata_WhenNameProvided()
    {
        var service = CreateServiceMock();
        var managementService = new Mock<IProtocolManagementService>();
        var protocolId = Guid.NewGuid();
        var metadata = new SavedProtocolMetadata(protocolId, "New Protocol", DateTime.UtcNow, DateTime.UtcNow, false);
        managementService.Setup(x => x.CreateProtocol("New Protocol")).Returns(metadata);

        var controller = CreateController(service.Object, managementServiceMock: managementService);
        var request = new ProtocolEditorController.CreateProtocolRequest { Name = "New Protocol" };

        var result = controller.CreateProtocol(request) as JsonResult;

        Assert.That(result, Is.Not.Null);
        managementService.Verify(x => x.CreateProtocol("New Protocol"), Times.Once);
    }

    [Test]
    public void CreateProtocol_ReturnsBadRequest_WhenNameMissing()
    {
        var service = CreateServiceMock();
        var controller = CreateController(service.Object);
        var request = new ProtocolEditorController.CreateProtocolRequest { Name = "  " };

        var result = controller.CreateProtocol(request) as BadRequestObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(GetPropertyValue(result!.Value, "error"), Is.EqualTo("Protocol name is required."));
    }

    [Test]
    public void ListProtocols_ReturnsProtocolList()
    {
        var service = CreateServiceMock();
        var managementService = new Mock<IProtocolManagementService>();
        var list = new List<SavedProtocolMetadata>
        {
            new(Guid.NewGuid(), "Protocol A", DateTime.UtcNow, DateTime.UtcNow, true),
            new(Guid.NewGuid(), "Protocol B", DateTime.UtcNow, DateTime.UtcNow, false)
        };
        managementService.Setup(x => x.ListProtocols()).Returns(list);

        var controller = CreateController(service.Object, managementServiceMock: managementService);

        var result = controller.ListProtocols() as JsonResult;

        Assert.That(result, Is.Not.Null);
        managementService.Verify(x => x.ListProtocols(), Times.Once);
    }

    [Test]
    public void LoadProtocol_ReturnsSnapshot_WhenFound()
    {
        var service = CreateServiceMock();
        var managementService = new Mock<IProtocolManagementService>();
        var protocolId = Guid.NewGuid();
        var snapshot = new ProtocolEditorSnapshot
        {
            Document = new ProtocolDocument { Id = -1, LinkId = -1, LinkText = string.Empty, Text = "Loaded", Sections = [] },
            UndoHistory = [], RedoHistory = [], LastUpdatedUtc = DateTimeOffset.UtcNow,
            ActiveProtocolId = protocolId
        };
        managementService.Setup(x => x.LoadProtocol(protocolId)).Returns(snapshot);

        var controller = CreateController(service.Object, managementServiceMock: managementService);

        var result = controller.LoadProtocol(protocolId) as JsonResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(GetPropertyValue(result!.Value, "activeProtocolId"), Is.EqualTo(protocolId));
    }

    [Test]
    public void LoadProtocol_ReturnsNotFound_WhenProtocolMissing()
    {
        var service = CreateServiceMock();
        var managementService = new Mock<IProtocolManagementService>();
        var protocolId = Guid.NewGuid();
        managementService.Setup(x => x.LoadProtocol(protocolId))
            .Throws(new InvalidOperationException("Not found"));

        var controller = CreateController(service.Object, managementServiceMock: managementService);

        var result = controller.LoadProtocol(protocolId) as NotFoundObjectResult;

        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void DeleteProtocol_ReturnsSuccess_WhenDeleted()
    {
        var service = CreateServiceMock();
        var managementService = new Mock<IProtocolManagementService>();
        var protocolId = Guid.NewGuid();
        managementService.Setup(x => x.DeleteProtocol(protocolId)).Returns(true);

        var controller = CreateController(service.Object, managementServiceMock: managementService);

        var result = controller.DeleteProtocol(protocolId) as JsonResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(GetPropertyValue(result!.Value, "success"), Is.True);
    }

    [Test]
    public void DeleteProtocol_ReturnsBadRequest_WhenCannotDelete()
    {
        var service = CreateServiceMock();
        var managementService = new Mock<IProtocolManagementService>();
        var protocolId = Guid.NewGuid();
        managementService.Setup(x => x.DeleteProtocol(protocolId)).Returns(false);

        var controller = CreateController(service.Object, managementServiceMock: managementService);

        var result = controller.DeleteProtocol(protocolId) as BadRequestObjectResult;

        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void RenameProtocol_ReturnsSuccess_WhenRenamed()
    {
        var service = CreateServiceMock();
        var managementService = new Mock<IProtocolManagementService>();
        var protocolId = Guid.NewGuid();

        var controller = CreateController(service.Object, managementServiceMock: managementService);
        var request = new ProtocolEditorController.RenameProtocolRequest { Name = "Renamed" };

        var result = controller.RenameProtocol(protocolId, request) as JsonResult;

        Assert.That(result, Is.Not.Null);
        managementService.Verify(x => x.RenameProtocol(protocolId, "Renamed"), Times.Once);
    }

    [Test]
    public void RenameProtocol_ReturnsBadRequest_WhenNameMissing()
    {
        var service = CreateServiceMock();
        var controller = CreateController(service.Object);
        var request = new ProtocolEditorController.RenameProtocolRequest { Name = null };

        var result = controller.RenameProtocol(Guid.NewGuid(), request) as BadRequestObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(GetPropertyValue(result!.Value, "error"), Is.EqualTo("Protocol name is required."));
    }

    [Test]
    public void SaveXml_SavesToActiveProtocol_WhenActiveIdExists()
    {
        var service = CreateServiceMock();
        var repository = CreateRepositoryMock();
        var managementService = new Mock<IProtocolManagementService>();
        var activeId = Guid.NewGuid();
        managementService.Setup(x => x.GetActiveProtocolId()).Returns(activeId);

        var controller = CreateController(service.Object, repository, managementService);

        var result = controller.SaveXml() as JsonResult;

        Assert.That(result, Is.Not.Null);
        repository.Verify(x => x.SaveProtocol(activeId, It.IsAny<string>(), It.IsAny<ProtocolDocument>(), "SaveXml"), Times.Once);
    }

    private static ProtocolEditorController CreateController(
        IProtocolEditorService service,
        Mock<IProtocolRepository>? repositoryMock = null,
        Mock<IProtocolManagementService>? managementServiceMock = null)
    {
        var repository = repositoryMock ?? CreateRepositoryMock();
        var managementService = managementServiceMock ?? new Mock<IProtocolManagementService>();
        var requestValidator = new ProtocolEditorRequestValidator();
        var responseMapper = new ProtocolEditorResponseMapper();

        return new ProtocolEditorController(
            service,
            Options.Create(new ProtocolEditorFeatureOptions { ProtocolEditorEnabled = true }),
            repository.Object,
            requestValidator,
            responseMapper,
            managementService.Object,
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
            .Setup(x => x.SaveCurrentProtocol(It.IsAny<ProtocolDocument>(), It.IsAny<string>()))
            .Returns<ProtocolDocument, string>((document, source) =>
                new ProtocolVersion(Guid.NewGuid(), DateTime.UtcNow, source, "SaveCurrentProtocol", document));
        repository
            .Setup(x => x.SaveVersion(It.IsAny<ProtocolDocument>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns<ProtocolDocument, string, string>((document, source, note) =>
                new ProtocolVersion(Guid.NewGuid(), DateTime.UtcNow, source, note, document));
        repository
            .Setup(x => x.GetCurrentProtocol())
            .Returns((ProtocolVersion?)null);
        return repository;
    }

    private static object? GetPropertyValue(object? obj, string propertyName)
    {
        return obj?.GetType().GetProperty(propertyName)?.GetValue(obj, null);
    }
}
