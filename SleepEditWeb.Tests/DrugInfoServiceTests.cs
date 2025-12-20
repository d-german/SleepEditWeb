using System.Net;
using Moq;
using Moq.Protected;
using SleepEditWeb.Services;
using Microsoft.Extensions.Logging;
using CSharpFunctionalExtensions;

namespace SleepEditWeb.Tests;

[TestFixture]
public class DrugInfoServiceTests
{
    private Mock<HttpMessageHandler> _handlerMock;
    private HttpClient _httpClient;
    private Mock<ILogger<OpenFdaDrugInfoService>> _loggerMock;
    private OpenFdaDrugInfoService _service;

    [SetUp]
    public void Setup()
    {
        _handlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_handlerMock.Object);
        _loggerMock = new Mock<ILogger<OpenFdaDrugInfoService>>();
        _service = new OpenFdaDrugInfoService(_httpClient, _loggerMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
    }

    [Test]
    public async Task GetDrugInfoAsync_EmptyName_ReturnsFailure()
    {
        var result = await _service.GetDrugInfoAsync("");
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Is.EqualTo("Drug name is required."));
    }

    [Test]
    public async Task GetDrugInfoAsync_FoundByBrandName_ReturnsSuccess()
    {
        // Arrange
        var drugName = "Advil";
        var jsonResponse = "{\"results\": [{\"openfda\": {\"brand_name\": [\"Advil\"], \"generic_name\": [\"Ibuprofen\"]}}]}";
        
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _service.GetDrugInfoAsync(drugName);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Name, Is.EqualTo("Advil"));
        Assert.That(result.Value.GenericName, Is.EqualTo("Ibuprofen"));
    }

    [Test]
    public async Task GetDrugInfoAsync_FoundByGenericName_ReturnsSuccess()
    {
        // Arrange
        var drugName = "Ibuprofen";
        
        // Mock first call (Brand Name) to return null
        // Mock second call (Generic Name) to return valid result
        var callCount = 0;
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(() => 
            {
                callCount++;
                if (callCount == 1) return new HttpResponseMessage { StatusCode = HttpStatusCode.NotFound };
                return new HttpResponseMessage 
                { 
                    StatusCode = HttpStatusCode.OK, 
                    Content = new StringContent("{\"results\": [{\"openfda\": {\"brand_name\": [\"Advil\"], \"generic_name\": [\"Ibuprofen\"]}}]}")
                };
            });

        // Act
        var result = await _service.GetDrugInfoAsync(drugName);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(callCount, Is.EqualTo(2));
    }

    [Test]
    public async Task GetDrugInfoAsync_NotFound_ReturnsFailure()
    {
        // Arrange
        var drugName = "UnknownDrug";
        
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        // Act
        var result = await _service.GetDrugInfoAsync(drugName);

        // Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Contains.Substring("No FDA drug label information found"));
    }

    [Test]
    public async Task GetDrugInfoAsync_WithHtmlTags_CleansText()
    {
        // Arrange
        var drugName = "HtmlDrug";
        var jsonResponse = "{\"results\": [{\"purpose\": [\"<p>Cleans <b>this</b></p>\"], \"openfda\": {\"brand_name\": [\"HtmlDrug\"]}}]}";
        
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _service.GetDrugInfoAsync(drugName);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Purpose, Is.EqualTo("Cleans this"));
    }

    [Test]
    public async Task GetDrugInfoAsync_HttpError_ReturnsFailure()
    {
        // Arrange
        var drugName = "ErrorDrug";
        
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _service.GetDrugInfoAsync(drugName);

        // Assert
        Assert.That(result.IsFailure, Is.True);
        Assert.That(result.Error, Contains.Substring("Unable to connect to FDA database"));
    }
}
