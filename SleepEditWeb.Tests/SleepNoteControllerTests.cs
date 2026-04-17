using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SleepEditWeb.Controllers;
using SleepEditWeb.Models;
using SleepEditWeb.Services;

namespace SleepEditWeb.Tests;

[TestFixture]
public sealed class SleepNoteControllerTests : IDisposable
{
    private Mock<ISleepNoteService> _mockService = null!;
    private SleepNoteController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _mockService = new Mock<ISleepNoteService>();
        _controller = new SleepNoteController(
            _mockService.Object,
            NullLogger<SleepNoteController>.Instance);
    }

    [TearDown]
    public void TearDown() => Dispose();

    public void Dispose() => _controller?.Dispose();

    // ── Index ──────────────────────────────────────────────────────────

    [Test]
    public void Index_ReturnsView()
    {
        var result = _controller.Index();
        Assert.That(result, Is.InstanceOf<ViewResult>());
    }

    // ── GenerateNote ───────────────────────────────────────────────────

    [Test]
    public void GenerateNote_ValidFormData_ReturnsOkWithNarrative()
    {
        var formData = new SleepNoteFormData
        {
            BodyPositions = new HashSet<string> { "Supine" },
            SnoringLevels = new HashSet<string>(),
            Events = new HashSet<string>(),
            Effects = new HashSet<string>(),
            MiscOptions = new HashSet<string>()
        };

        var expected = new SleepNoteGeneratedResult("Test narrative", DateTime.UtcNow);
        _mockService.Setup(s => s.GenerateNote(formData)).Returns(expected);

        var result = _controller.GenerateNote(formData) as OkObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value, Is.EqualTo(expected));
    }

    [Test]
    public void GenerateNote_NullFormData_ReturnsBadRequest()
    {
        var result = _controller.GenerateNote(null);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    // ── GetConfiguration ───────────────────────────────────────────────

    [Test]
    public void GetConfiguration_ReturnsOkWithConfig()
    {
        var config = new SleepNoteConfiguration
        {
            MaskTypes = ["Type1"],
            MaskSizes = ["Size1"]
        };
        _mockService.Setup(s => s.GetConfiguration()).Returns(config);

        var result = _controller.GetConfiguration() as OkObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Value, Is.EqualTo(config));
    }

    // ── AddMaskType ────────────────────────────────────────────────────

    [Test]
    public void AddMaskType_ValidValue_ReturnsCreated()
    {
        var request = new SleepNoteController.ValueRequest { Value = "NewMask" };

        var result = _controller.AddMaskType(request);

        Assert.That(result, Is.InstanceOf<CreatedResult>());
        _mockService.Verify(s => s.AddMaskType("NewMask"), Times.Once);
    }

    [Test]
    public void AddMaskType_NullRequest_ReturnsBadRequest()
    {
        var result = _controller.AddMaskType(null);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public void AddMaskType_EmptyValue_ReturnsBadRequest()
    {
        var request = new SleepNoteController.ValueRequest { Value = "" };
        var result = _controller.AddMaskType(request);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    // ── RemoveMaskType ─────────────────────────────────────────────────

    [Test]
    public void RemoveMaskType_ValidValue_ReturnsNoContent()
    {
        var result = _controller.RemoveMaskType("OldMask");

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        _mockService.Verify(s => s.RemoveMaskType("OldMask"), Times.Once);
    }

    [Test]
    public void RemoveMaskType_EmptyValue_ReturnsBadRequest()
    {
        var result = _controller.RemoveMaskType("");
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    // ── AddMaskSize ────────────────────────────────────────────────────

    [Test]
    public void AddMaskSize_ValidValue_ReturnsCreated()
    {
        var request = new SleepNoteController.ValueRequest { Value = "XL" };

        var result = _controller.AddMaskSize(request);

        Assert.That(result, Is.InstanceOf<CreatedResult>());
        _mockService.Verify(s => s.AddMaskSize("XL"), Times.Once);
    }

    [Test]
    public void AddMaskSize_NullRequest_ReturnsBadRequest()
    {
        var result = _controller.AddMaskSize(null);
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    // ── RemoveMaskSize ─────────────────────────────────────────────────

    [Test]
    public void RemoveMaskSize_ValidValue_ReturnsNoContent()
    {
        var result = _controller.RemoveMaskSize("small");

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        _mockService.Verify(s => s.RemoveMaskSize("small"), Times.Once);
    }

    [Test]
    public void RemoveMaskSize_EmptyValue_ReturnsBadRequest()
    {
        var result = _controller.RemoveMaskSize("");
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public void ResetConfig_ReturnsOkWithConfiguration()
    {
        var defaultConfig = new SleepNoteConfiguration
        {
            MaskTypes = ["Respironics Comfort Select", "F&P Flexifit HC407"],
            MaskSizes = ["small", "medium", "large"],
            TechnicianNames = [],
            PressureValues = Enumerable.Range(4, 17).ToList()
        };
        _mockService.Setup(s => s.GetConfiguration()).Returns(defaultConfig);

        var result = _controller.ResetConfig();

        _mockService.Verify(s => s.ResetConfigToDefaults(), Times.Once);
        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }
}
