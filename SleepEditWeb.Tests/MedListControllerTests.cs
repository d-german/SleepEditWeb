using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SleepEditWeb.Controllers;
using SleepEditWeb.Data;
using SleepEditWeb.Models;
using SleepEditWeb.Services;
using CSharpFunctionalExtensions;

namespace SleepEditWeb.Tests;

[TestFixture]
public class MedListControllerTests
{
    private Mock<IMedicationRepository> _mockRepo;
    private Mock<IDrugInfoService> _mockDrugService;
    private Mock<ISession> _mockSession;
    private MedListController _controller;

    [SetUp]
    public void Setup()
    {
        _mockRepo = new Mock<IMedicationRepository>();
        _mockDrugService = new Mock<IDrugInfoService>();
        _mockSession = new Mock<ISession>();

        var httpContext = new DefaultHttpContext();
        httpContext.Session = _mockSession.Object;

        _controller = new MedListController(_mockRepo.Object, _mockDrugService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };
    }

    [TearDown]
    public void TearDown()
    {
        _controller?.Dispose();
    }

    [Test]
    public void Index_SessionEmpty_ReturnsViewWithMedList()
    {
        // Arrange
        byte[]? sessionValue = null;
        _mockSession.Setup(s => s.TryGetValue("SelectedMeds", out sessionValue)).Returns(false);
        
        var medNames = new List<string> { "Med1", "Med2" };
        _mockRepo.Setup(r => r.GetAllMedicationNames()).Returns(medNames);

        // Act
        var result = _controller.Index() as ViewResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Model, Is.EqualTo(medNames));
        Assert.That(_controller.ViewBag.SelectedMeds, Is.Empty);
    }

    [Test]
    public void Index_SessionHasMeds_ReturnsViewWithSelectedMeds()
    {
        // Arrange
        var selectedMedsStr = "Med1,Med2";
        var sessionValue = Encoding.UTF8.GetBytes(selectedMedsStr);
        _mockSession.Setup(s => s.TryGetValue("SelectedMeds", out sessionValue)).Returns(true);
        
        var medNames = new List<string> { "Med1", "Med2", "Med3" };
        _mockRepo.Setup(r => r.GetAllMedicationNames()).Returns(medNames);

        // Act
        var result = _controller.Index() as ViewResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Model, Is.EqualTo(medNames));
        var selectedMeds = _controller.ViewBag.SelectedMeds as List<string>;
        Assert.That(selectedMeds, Is.Not.Null);
        Assert.That(selectedMeds.Count, Is.EqualTo(2));
        Assert.That(selectedMeds, Contains.Item("Med1"));
        Assert.That(selectedMeds, Contains.Item("Med2"));
    }

    private object? GetPropertyValue(object? obj, string propertyName)
    {
        return obj?.GetType().GetProperty(propertyName)?.GetValue(obj, null);
    }

    [Test]
    public void AddMedication_RequestNull_ReturnsError()
    {
        // Act
        var result = _controller.AddMedication(null!) as JsonResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(GetPropertyValue(result.Value, "success"), Is.False);
        Assert.That(GetPropertyValue(result.Value, "message"), Is.EqualTo("Invalid input. Please try again."));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void AddMedication_InvalidInput_ReturnsError(string? input)
    {
        // Arrange
        var request = new MedListController.MedRequest { SelectedMed = input };

        // Act
        var result = _controller.AddMedication(request) as JsonResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(GetPropertyValue(result.Value, "success"), Is.False);
        Assert.That(GetPropertyValue(result.Value, "message"), Is.EqualTo("Invalid input. Please try again."));
    }

    [Test]
    public void AddMedication_MasterListAdditionSuccess_ReturnsSuccess()
    {
        // Arrange
        var request = new MedListController.MedRequest { SelectedMed = "+NewMed" };
        _mockRepo.Setup(r => r.AddUserMedication("NewMed")).Returns(Result.Success());

        // Act
        var result = _controller.AddMedication(request) as JsonResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(GetPropertyValue(result.Value, "success"), Is.True);
        Assert.That(GetPropertyValue(result.Value, "message"), Is.EqualTo("Medication 'NewMed' has been added to the master list."));
    }

    [Test]
    public void AddMedication_MasterListAdditionFailure_ReturnsError()
    {
        // Arrange
        var request = new MedListController.MedRequest { SelectedMed = "+ExistingMed" };
        _mockRepo.Setup(r => r.AddUserMedication("ExistingMed")).Returns(Result.Failure("Already exists"));

        // Act
        var result = _controller.AddMedication(request) as JsonResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(GetPropertyValue(result.Value, "success"), Is.False);
        Assert.That(GetPropertyValue(result.Value, "message"), Is.EqualTo("Already exists"));
    }

    [Test]
    public void AddMedication_MasterListRemovalSuccess_ReturnsSuccess()
    {
        // Arrange
        var request = new MedListController.MedRequest { SelectedMed = "-UserMed" };
        _mockRepo.Setup(r => r.RemoveUserMedication("UserMed")).Returns(Result.Success());

        // Act
        var result = _controller.AddMedication(request) as JsonResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(GetPropertyValue(result.Value, "success"), Is.True);
        Assert.That(GetPropertyValue(result.Value, "message"), Is.EqualTo("Medication 'UserMed' has been removed from the master list."));
    }

    [Test]
    public void AddMedication_MasterListRemovalFailure_ReturnsError()
    {
        // Arrange
        var request = new MedListController.MedRequest { SelectedMed = "-SystemMed" };
        _mockRepo.Setup(r => r.RemoveUserMedication("SystemMed")).Returns(Result.Failure("Cannot remove system medication"));

        // Act
        var result = _controller.AddMedication(request) as JsonResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(GetPropertyValue(result.Value, "success"), Is.False);
        Assert.That(GetPropertyValue(result.Value, "message"), Is.EqualTo("Cannot remove system medication"));
    }

    [Test]
    public void AddMedication_ClearSession_ReturnsCleared()
    {
        // Arrange
        var request = new MedListController.MedRequest { SelectedMed = "cls" };

        // Act
        var result = _controller.AddMedication(request) as JsonResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(GetPropertyValue(result.Value, "success"), Is.True);
        Assert.That(GetPropertyValue(result.Value, "message"), Is.EqualTo("Selected medications cleared."));
        _mockSession.Verify(s => s.Remove("SelectedMeds"), Times.Once);
    }

    [Test]
    public void AddMedication_NormalMed_AddsToSession()
    {
        // Arrange
        var request = new MedListController.MedRequest { SelectedMed = "MedX" };
        byte[]? sessionValue = null;
        _mockSession.Setup(s => s.TryGetValue("SelectedMeds", out sessionValue)).Returns(false);

        // Act
        var result = _controller.AddMedication(request) as JsonResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(GetPropertyValue(result.Value, "success"), Is.True);
        Assert.That(GetPropertyValue(result.Value, "message"), Is.EqualTo("Added: MedX"));
        _mockSession.Verify(s => s.Set("SelectedMeds", It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == "MedX")), Times.Once);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public async Task DrugInfo_InvalidInput_ReturnsBadRequest(string? name)
    {
        // Act
        var result = await _controller.DrugInfo(name!) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(GetPropertyValue(result.Value, "error"), Is.EqualTo("Drug name is required"));
    }

    [Test]
    public async Task DrugInfo_ValidInput_ReturnsDrugInfo()
    {
        // Arrange
        var drugName = "Aspirin";
        var drugInfo = new DrugInfo { Name = drugName };
        _mockDrugService.Setup(s => s.GetDrugInfoAsync(drugName)).ReturnsAsync(Result.Success(drugInfo));

        // Act
        var result = await _controller.DrugInfo(drugName) as JsonResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(GetPropertyValue(result.Value, "found"), Is.True);
        Assert.That(GetPropertyValue(result.Value, "name"), Is.EqualTo(drugName));
    }

    [Test]
    public async Task DrugInfo_ServiceFailure_ReturnsError()
    {
        // Arrange
        var drugName = "Unknown";
        _mockDrugService.Setup(s => s.GetDrugInfoAsync(drugName)).ReturnsAsync(Result.Failure<DrugInfo>("Not found"));

        // Act
        var result = await _controller.DrugInfo(drugName) as JsonResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(GetPropertyValue(result.Value, "found"), Is.False);
        Assert.That(GetPropertyValue(result.Value, "errorMessage"), Is.EqualTo("Not found"));
    }

    [Test]
    public void DiagnosticInfo_ReturnsStats()
    {
        // Arrange
        var stats = new MedicationStats { TotalCount = 10, SystemMedCount = 5, UserMedCount = 5, LoadedFrom = "Test" };
        _mockRepo.Setup(r => r.GetStats()).Returns(stats);
        _mockRepo.Setup(r => r.DatabasePath).Returns("test/path");
        _mockRepo.Setup(r => r.GetAllMedicationNames()).Returns(new List<string> { "M1", "M2" });

        // Act
        var result = _controller.DiagnosticInfo() as JsonResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(GetPropertyValue(result.Value, "TotalMedicationCount"), Is.EqualTo(10));
        Assert.That(GetPropertyValue(result.Value, "DatabasePath"), Is.EqualTo("test/path"));
    }
}
