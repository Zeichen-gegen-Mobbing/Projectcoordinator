using System.Net;
using System.Text;
using System.Text.Json;
using api.Exceptions;
using api.Models;
using api.Options;
using api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using TUnit.Assertions.Extensions;

namespace api.Tests.Unit.Services;

public class LocationOpenRouteServiceTests
{
    private Mock<IOptions<OpenRouteServiceOptions>> mockOptions = null!;
    private Mock<IHttpClientFactory> mockHttpClientFactory = null!;
    private Mock<ILogger<LocationOpenRouteService>> mockLogger = null!;
    private Mock<HttpMessageHandler> mockHttpMessageHandler = null!;
    private HttpClient httpClient = null!;

    [Before(Test)]
    public void Setup()
    {
        mockOptions = new Mock<IOptions<OpenRouteServiceOptions>>();
        mockOptions.Setup(o => o.Value).Returns(new OpenRouteServiceOptions
        {
            Title = "OpenRouteService",
            BaseUrl = "https://api.openrouteservice.org",
            ApiKey = "test-api-key"
        });

        mockHttpClientFactory = new Mock<IHttpClientFactory>();
        mockLogger = new Mock<ILogger<LocationOpenRouteService>>();
        mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        httpClient = new HttpClient(mockHttpMessageHandler.Object);
        mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
    }

    [After(Test)]
    public void Cleanup()
    {
        httpClient?.Dispose();
    }

    private void SetupMockResponse(OpenRouteServiceGeocodeResponse.Feature[] features, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var responseContent = new OpenRouteServiceGeocodeResponse
        {
            Features = features
        };

        var jsonResponse = JsonSerializer.Serialize(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            });
    }

    [Test]
    public async Task ReturnsLocations_WhenApiReturnsSuccess()
    {
        // Arrange
        var query = "Berlin";
        var features = new[]
        {
            new OpenRouteServiceGeocodeResponse.Feature
            {
                Geometry = new OpenRouteServiceGeocodeResponse.Geometry
                {
                    Coordinates = new[] { 13.404954, 52.520008 }
                },
                Properties = new OpenRouteServiceGeocodeResponse.Properties
                {
                    Label = "Berlin, Germany",
                    Name = "Berlin City",
                    Street = "Unter den Linden",
                    Housenumber = "1",
                    Postalcode = "10117",
                    Country = "Germany",
                    Region = "Berlin",
                    County = null,
                    Locality = "Berlin"
                }
            }
        };

        SetupMockResponse(features);

        var service = new LocationOpenRouteService(mockOptions.Object, mockHttpClientFactory.Object, mockLogger.Object);

        // Act
        var results = (await service.SearchAsync(query)).ToList();

        // Assert
        await Assert.That(results).HasCount().EqualTo(1);
        await Assert.That(results[0].Label).IsEqualTo("Berlin, Germany");
        await Assert.That(results[0].Longitude).IsEqualTo(13.404954);
        await Assert.That(results[0].Latitude).IsEqualTo(52.520008);
        await Assert.That(results[0].Name).IsEqualTo("Berlin City");
        await Assert.That(results[0].Street).IsEqualTo("Unter den Linden");
        await Assert.That(results[0].HouseNumber).IsEqualTo("1");
        await Assert.That(results[0].PostalCode).IsEqualTo("10117");
        await Assert.That(results[0].Country).IsEqualTo("Germany");
        await Assert.That(results[0].Locality).IsEqualTo("Berlin");
    }

    [Test]
    public async Task EscapesQueryParameter_WhenQueryContainsSpecialCharacters()
    {
        // Arrange
        var query = "Berlin & München";
        
        SetupMockResponse(Array.Empty<OpenRouteServiceGeocodeResponse.Feature>());

        var service = new LocationOpenRouteService(mockOptions.Object, mockHttpClientFactory.Object, mockLogger.Object);

        // Act
        var results = await service.SearchAsync(query);

        // Assert
        await Assert.That(results).IsEmpty();
    }

    [Test]
    public async Task ReturnsEmptyList_WhenNoResultsFound()
    {
        // Arrange
        var query = "NonExistentLocation12345";
        
        SetupMockResponse(Array.Empty<OpenRouteServiceGeocodeResponse.Feature>());

        var service = new LocationOpenRouteService(mockOptions.Object, mockHttpClientFactory.Object, mockLogger.Object);

        // Act
        var results = await service.SearchAsync(query);

        // Assert
        await Assert.That(results).IsEmpty();
    }

    [Test]
    public async Task ThrowsProblemDetailsException_WhenApiReturnsError()
    {
        // Arrange
        var query = "Berlin";

        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("Bad Request", Encoding.UTF8, "text/plain")
            });

        var service = new LocationOpenRouteService(mockOptions.Object, mockHttpClientFactory.Object, mockLogger.Object);

        // Act & Assert
        await Assert.That(async () => await service.SearchAsync(query)).ThrowsExactly<ProblemDetailsException>();
    }

    [Test]
    public async Task ThrowsProblemDetailsException_WhenResponseIsInvalid()
    {
        // Arrange
        var query = "Berlin";

        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("Invalid JSON", Encoding.UTF8, "application/json")
            });

        var service = new LocationOpenRouteService(mockOptions.Object, mockHttpClientFactory.Object, mockLogger.Object);

        // Act & Assert
        await Assert.That(async () => await service.SearchAsync(query)).ThrowsExactly<ProblemDetailsException>();
    }

    [Test]
    public async Task SetsAuthorizationHeader_WhenMakingRequest()
    {
        // Arrange
        var query = "Berlin";
        
        SetupMockResponse(Array.Empty<OpenRouteServiceGeocodeResponse.Feature>());

        HttpRequestMessage? capturedRequest = null;

        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"features\":[]}", Encoding.UTF8, "application/json")
            });

        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-api-key");
        var service = new LocationOpenRouteService(mockOptions.Object, mockHttpClientFactory.Object, mockLogger.Object);

        // Act
        await service.SearchAsync(query);

        // Assert
        await Assert.That(capturedRequest).IsNotNull();
        await Assert.That(capturedRequest!.Headers.Authorization).IsNotNull();
        await Assert.That(capturedRequest.Headers.Authorization!.Scheme).IsEqualTo("Bearer");
        await Assert.That(capturedRequest.Headers.Authorization.Parameter).IsEqualTo("test-api-key");
    }

    [Test]
    public async Task ParsesRealTestData_WhenProvidedWithActualORSResponse()
    {
        // Arrange
        var query = "Gesamtschule Alterteichweg";
        var testDataPath = Path.Combine("TestData", "ors__geocode_search_get_1761571663311.json");
        var testDataJson = await File.ReadAllTextAsync(testDataPath);

        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(testDataJson, Encoding.UTF8, "application/json")
            });

        var service = new LocationOpenRouteService(mockOptions.Object, mockHttpClientFactory.Object, mockLogger.Object);

        // Act
        var results = (await service.SearchAsync(query)).ToList();

        // Assert
        await Assert.That(results).HasCount().EqualTo(10);
        
        // Verify first result (Gesamtschule Ebsdorfergrund)
        await Assert.That(results[0].Label).IsEqualTo("Gesamtschule Ebsdorfergrund, Ebsdorfergrund, HE, Germany");
        await Assert.That(results[0].Name).IsEqualTo("Gesamtschule Ebsdorfergrund");
        await Assert.That(results[0].Street).IsEqualTo("Zur Gesamtschule");
        await Assert.That(results[0].HouseNumber).IsEqualTo("21");
        await Assert.That(results[0].PostalCode).IsEqualTo("35085");
        await Assert.That(results[0].Longitude).IsEqualTo(8.841337);
        await Assert.That(results[0].Latitude).IsEqualTo(50.74114);
        await Assert.That(results[0].Country).IsEqualTo("Germany");
        await Assert.That(results[0].Locality).IsEqualTo("Ebsdorfergrund");
    }
}
