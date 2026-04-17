using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SleepEditWeb.Infrastructure.SleepNote;
using SleepEditWeb.Models;
using SleepEditWeb.Services;

namespace SleepEditWeb.Tests;

[TestFixture]
public sealed class SleepNoteServiceTests
{
    private Mock<ISleepNoteConfigRepository> _mockRepo = null!;
    private SleepNoteService _service = null!;

    [SetUp]
    public void Setup()
    {
        _mockRepo = new Mock<ISleepNoteConfigRepository>();
        _service = new SleepNoteService(
            _mockRepo.Object,
            NullLogger<SleepNoteService>.Instance);
    }

    [Test]
    public void GenerateNote_ReturnsNarrativeWithTimestamp()
    {
        var formData = new SleepNoteFormData
        {
            StudyType = StudyType.Polysomnogram,
            BodyPositions = new HashSet<string> { "Supine" },
            SnoringLevels = new HashSet<string>(),
            Events = new HashSet<string>(),
            Effects = new HashSet<string>(),
            MiscOptions = new HashSet<string>()
        };

        var result = _service.GenerateNote(formData);

        Assert.That(result.NarrativeText, Does.Contain("supine"));
        Assert.That(result.GeneratedUtc, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public void GetConfiguration_DelegatesToRepository()
    {
        var expected = new SleepNoteConfiguration
        {
            MaskTypes = ["Type1"],
            MaskSizes = ["Size1"]
        };
        _mockRepo.Setup(r => r.GetConfiguration()).Returns(expected);

        var result = _service.GetConfiguration();

        Assert.That(result, Is.SameAs(expected));
        _mockRepo.Verify(r => r.GetConfiguration(), Times.Once);
    }

    [Test]
    public void AddMaskType_DelegatesToRepository()
    {
        _service.AddMaskType("NewType");
        _mockRepo.Verify(r => r.AddMaskType("NewType"), Times.Once);
    }

    [Test]
    public void RemoveMaskType_DelegatesToRepository()
    {
        _service.RemoveMaskType("OldType");
        _mockRepo.Verify(r => r.RemoveMaskType("OldType"), Times.Once);
    }

    [Test]
    public void AddMaskSize_DelegatesToRepository()
    {
        _service.AddMaskSize("XL");
        _mockRepo.Verify(r => r.AddMaskSize("XL"), Times.Once);
    }

    [Test]
    public void RemoveMaskSize_DelegatesToRepository()
    {
        _service.RemoveMaskSize("small");
        _mockRepo.Verify(r => r.RemoveMaskSize("small"), Times.Once);
    }

    [Test]
    public void ResetConfigToDefaults_DelegatesToRepository()
    {
        _service.ResetConfigToDefaults();
        _mockRepo.Verify(r => r.ResetToDefaults(), Times.Once);
    }
}
