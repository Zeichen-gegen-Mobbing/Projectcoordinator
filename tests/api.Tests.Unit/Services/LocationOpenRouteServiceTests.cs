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
    private readonly Mock<IOptions<OpenRouteServiceOptions>> mockOptions;
    private readonly Mock<IHttpClientFactory> mockHttpClientFactory;
    private readonly Mock<ILogger<LocationOpenRouteService>> mockLogger;
    private readonly Mock<HttpMessageHandler> mockHttpMessageHandler;
    private readonly HttpClient httpClient;
    private readonly LocationOpenRouteService service;

    public LocationOpenRouteServiceTests()
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

        service = new LocationOpenRouteService(mockOptions.Object, mockHttpClientFactory.Object, mockLogger.Object);
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

    private void VerifyQueryParameter(string parameterName, string expectedValue, Times? times = null)
    {
        Verify(req =>
            req.RequestUri != null &&
            req.RequestUri.Query.Contains($"{parameterName}={expectedValue}"),
            times);
    }

    private void Verify(System.Linq.Expressions.Expression<Func<HttpRequestMessage, bool>> match, Times? times = null)
    {
        // Verify query parameter was added to request
        mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            times ?? Times.Once(),
            ItExpr.Is<HttpRequestMessage>(match),
            ItExpr.IsAny<CancellationToken>());
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
    [Arguments("Ää", "%C3%84%C3%A4")]
    [Arguments("ß", "%C3%9F")]
    [Arguments("S p a c e", "S%20p%20a%20c%20e")]
    [Arguments("A&B", "A%26B")]
    public async Task EscapesQueryParameter_WhenQueryContainsSpecialCharacters(string query, string expected)
    {
        // Arrange
        SetupMockResponse(Array.Empty<OpenRouteServiceGeocodeResponse.Feature>());

        // Act
        _ = await service.SearchAsync(query);

        // Verify query parameter was added with proper encoding
        VerifyQueryParameter("text", expected);
    }

    [Test]
    public async Task LimitsToGermany()
    {
        // Arrange
        SetupMockResponse(Array.Empty<OpenRouteServiceGeocodeResponse.Feature>());

        // Act
        _ = await service.SearchAsync("anything");

        // Verify country parameter was added
        VerifyQueryParameter("boundary.country", "DE");
    }

    [Test]
    public async Task ReturnsEmptyList_WhenNoResultsFound()
    {
        // Arrange
        SetupMockResponse(Array.Empty<OpenRouteServiceGeocodeResponse.Feature>());

        // Act
        var results = await service.SearchAsync("anything");

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

        // Act & Assert
        await Assert.That(async () => await service.SearchAsync(query)).ThrowsExactly<ProblemDetailsException>();
    }

    [Test]
    public async Task SetsAuthorizationHeader_WhenMakingRequest()
    {
        // Arrange
        var query = "Berlin";

        SetupMockResponse(Array.Empty<OpenRouteServiceGeocodeResponse.Feature>());

        // Act
        await service.SearchAsync(query);

        // Assert
        Verify(req =>
                req.Headers.Authorization != null &&
                req.Headers.Authorization.Scheme == "Bearer" &&
                req.Headers.Authorization.Parameter == mockOptions.Object.Value.ApiKey
        );
    }

    [Test]
    public async Task ParsesRealTestData_WhenProvidedWithActualORSResponse()
    {
        // Arrange
        var query = "Gesamtschule Alterteichweg";
        var testDataPath = Path.Combine("TestData", "ors__geocode_search_get_1761571663311.json");
        var testDataJson = await File.ReadAllTextAsync(testDataPath);

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
                Content = new StringContent(testDataJson, Encoding.UTF8, "application/json")
            });

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
