using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using SleepEditWeb.Controllers;
using SleepEditWeb.Data;
using SleepEditWeb.Models;
using CSharpFunctionalExtensions;

namespace SleepEditWeb.Tests;

[TestFixture]
public class AdminControllerTests
{
    private Mock<IMedicationRepository> _mockRepo;
    private AdminController _controller;
    private ITempDataDictionary _tempData;
    private const string SecretKey = "medAdmin2025xK9!";

    [SetUp]
    public void Setup()
    {
        _mockRepo = new Mock<IMedicationRepository>();
        _controller = new AdminController(_mockRepo.Object);
        
        var httpContext = new DefaultHttpContext();
        _tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        _controller.TempData = _tempData;
    }

    [TearDown]
    public void TearDown()
    {
        _controller?.Dispose();
    }

    [Test]
    public void Index_InvalidKey_ReturnsNotFound()
    {
        var result = _controller.Index("wrong-key");
        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public void Index_ValidKey_ReturnsView()
    {
        var stats = new MedicationStats { TotalCount = 10, LoadedFrom = "Test" };
        _mockRepo.Setup(r => r.GetStats()).Returns(stats);

        var result = _controller.Index(SecretKey) as ViewResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.ViewName, Is.EqualTo("Medications"));
        Assert.That(result.Model, Is.EqualTo(stats));
        Assert.That(_controller.ViewBag.SecretKey, Is.EqualTo(SecretKey));
    }

    [Test]
    public void Export_ValidKey_ReturnsFile()
    {
        var backup = new MedicationBackup { Medications = [] };
        _mockRepo.Setup(r => r.ExportAll()).Returns(backup);

        var result = _controller.Export(SecretKey) as FileContentResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.ContentType, Is.EqualTo("application/json"));
        Assert.That(result.FileDownloadName, Does.StartWith("medications_backup_"));
    }

    [Test]
    public async Task Import_NoFile_SetsError()
    {
        var result = await _controller.Import(SecretKey, null!, "merge") as RedirectToActionResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(_controller.TempData["Error"], Is.EqualTo("Please select a backup file to import."));
    }

    [Test]
    public async Task Import_ValidFileMergeSuccess_SetsSuccess()
    {
        // Arrange
        var content = "{\"medications\": [{\"name\": \"Med1\"}]}";
        var fileName = "test.json";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(_ => _.OpenReadStream()).Returns(stream);
        fileMock.Setup(_ => _.Length).Returns(stream.Length);
        fileMock.Setup(_ => _.FileName).Returns(fileName);

        _mockRepo.Setup(r => r.ImportMerge(It.IsAny<List<Medication>>())).Returns(Result.Success());

        // Act
        var result = await _controller.Import(SecretKey, fileMock.Object, "merge") as RedirectToActionResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(_controller.TempData["Success"], Is.EqualTo("Backup merged successfully. New medications added, existing preserved."));
    }

    [Test]
    public async Task Import_InvalidJson_SetsError()
    {
        // Arrange
        var content = "invalid-json";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(_ => _.OpenReadStream()).Returns(stream);
        fileMock.Setup(_ => _.Length).Returns(stream.Length);

        // Act
        var result = await _controller.Import(SecretKey, fileMock.Object, "merge") as RedirectToActionResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(_controller.TempData["Error"], Is.EqualTo("Invalid JSON format in backup file."));
    }

    [Test]
    public void Reseed_Success_SetsSuccess()
    {
        _mockRepo.Setup(r => r.Reseed()).Returns(Result.Success());

        var result = _controller.Reseed(SecretKey) as RedirectToActionResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(_controller.TempData["Success"], Is.EqualTo("Database reseeded successfully from embedded resource."));
    }

    [Test]
    public void Reseed_Failure_SetsError()
    {
        _mockRepo.Setup(r => r.Reseed()).Returns(Result.Failure("Reseed failed"));

        var result = _controller.Reseed(SecretKey) as RedirectToActionResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(_controller.TempData["Error"], Is.EqualTo("Reseed failed"));
    }

    [Test]
    public async Task Import_ValidFileReplaceSuccess_SetsSuccess()
    {
        // Arrange
        var content = "{\"medications\": [{\"name\": \"Med1\"}]}";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(_ => _.OpenReadStream()).Returns(stream);
        fileMock.Setup(_ => _.Length).Returns(stream.Length);

        _mockRepo.Setup(r => r.ImportReplace(It.IsAny<List<Medication>>())).Returns(Result.Success());

        // Act
        var result = await _controller.Import(SecretKey, fileMock.Object, "replace") as RedirectToActionResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(_controller.TempData["Success"], Does.Contain("Database replaced"));
    }

    [Test]
    public void Export_InvalidKey_ReturnsNotFound()
    {
        var result = _controller.Export("wrong-key");
        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public void Reseed_InvalidKey_ReturnsNotFound()
    {
        var result = _controller.Reseed("wrong-key");
        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public void ClearUserMeds_InvalidKey_ReturnsNotFound()
    {
        var result = _controller.ClearUserMeds("wrong-key");
        Assert.That(result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public void ClearUserMeds_Success_SetsSuccess()
    {
        _mockRepo.Setup(r => r.ClearUserMedications()).Returns(Result.Success());

        var result = _controller.ClearUserMeds(SecretKey) as RedirectToActionResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(_controller.TempData["Success"], Is.EqualTo("All user-added medications have been cleared."));
    }

    [Test]
    public void ClearUserMeds_Failure_SetsError()
    {
        _mockRepo.Setup(r => r.ClearUserMedications()).Returns(Result.Failure("Clear failed"));

        var result = _controller.ClearUserMeds(SecretKey) as RedirectToActionResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(_controller.TempData["Error"], Is.EqualTo("Clear failed"));
    }
}
