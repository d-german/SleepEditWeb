using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SleepEditWeb.Controllers;
using SleepEditWeb.Models;

namespace SleepEditWeb.Tests;

[TestFixture]
public class HomeControllerTests
{
    private Mock<ILogger<HomeController>> _mockLogger;
    private HomeController _controller;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<HomeController>>();
        _controller = new HomeController(_mockLogger.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _controller?.Dispose();
    }

    [Test]
    public void Index_ReturnsView()
    {
        var result = _controller.Index();
        Assert.That(result, Is.TypeOf<ViewResult>());
    }

    [Test]
    public void Privacy_ReturnsView()
    {
        var result = _controller.Privacy();
        Assert.That(result, Is.TypeOf<ViewResult>());
    }

    [Test]
    public void Error_ReturnsViewWithErrorModel()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = _controller.Error() as ViewResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Model, Is.TypeOf<ErrorViewModel>());
    }
}