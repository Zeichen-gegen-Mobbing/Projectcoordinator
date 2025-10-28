using System.Net;
using System.Net.Http.Json;
using FrontEnd.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using TUnit.Assertions.Extensions;
using ZgM.ProjectCoordinator.Shared;

namespace FrontEnd.Tests.Unit.Services;

public class LocationServiceTests : IDisposable
{
    protected readonly Mock<ILogger<LocationService>> _loggerMock = new();
    protected readonly Mock<HttpMessageHandler> _httpMessageHandlerMock = new();
    protected readonly LocationService _service;
    protected readonly HttpClient _httpClient;

    public LocationServiceTests()
    {
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.example.com/")
        };

        _service = new LocationService(_httpClient, _loggerMock.Object);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    private void Verify(System.Linq.Expressions.Expression<Func<HttpRequestMessage, bool>> match, Times? times = null)
    {
        // Verify query parameter was added to request
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            times ?? Times.Once(),
            ItExpr.Is<HttpRequestMessage>(match),
            ItExpr.IsAny<CancellationToken>());
    }

    public class SearchLocationsAsync : LocationServiceTests
    {
        public interface IHttpMessageHandlerProtected
        {
            Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
        }
        private List<HttpRequestMessage> SetupMockResponse(LocationSearchResult[] results, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            List<HttpRequestMessage> capturedRequests = new();
            _httpMessageHandlerMock.Protected().As<IHttpMessageHandlerProtected>()
                .Setup(h => h.SendAsync(
                    Capture.In(capturedRequests),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new HttpResponseMessage(statusCode)
                {
                    Content = JsonContent.Create(results)
                });
            return capturedRequests;
        }

        [Test]
        public async Task ReturnsLocations_WhenApiReturnsSuccess()
        {
            // Arrange
            var expectedResult = new LocationSearchResult
            {
                Label = "Berlin, Germany",
                Longitude = 13.404954,
                Latitude = 52.520008,
                Name = "Berlin",
                Country = "Germany"
            };

            SetupMockResponse([expectedResult]);

            // Act
            var result = await _service.SearchLocationsAsync("anything");

            // Assert
            await Assert.That(result).IsNotNull();
            var resultList = result.ToList();
            await Assert.That(resultList).HasCount().EqualTo(1);
            await Assert.That(resultList[0].Label).IsEqualTo(expectedResult.Label);
            await Assert.That(resultList[0].Longitude).IsEqualTo(expectedResult.Longitude);
            await Assert.That(resultList[0].Latitude).IsEqualTo(expectedResult.Latitude);
        }

        [Test]
        [Arguments("Ää", "%C3%84%C3%A4")]
        [Arguments("ß", "%C3%9F")]
        [Arguments("S p a c e", "S%20p%20a%20c%20e")]
        [Arguments("A&B", "A%26B")]
        public async Task EncodesQueryParameter_WhenSearchingLocations(string query, string expectedEncoded)
        {
            // Arrange
            var requests = SetupMockResponse(Array.Empty<LocationSearchResult>());

            // Act
            await _service.SearchLocationsAsync(query);

            // Assert
            await Assert.That(requests).IsNotNull();
            await Assert.That(requests).HasCount().EqualTo(1);
            await Assert.That(requests[0]?.RequestUri?.AbsoluteUri).Contains(expectedEncoded);

            Verify(req =>
                req.RequestUri != null &&
                req.RequestUri.AbsoluteUri.Contains(expectedEncoded)
            );
        }

        [Test]
        public async Task CallsCorrectEndpoint_WhenSearchingLocations()
        {
            // Arrange
            var searchQuery = "Berlin";
            SetupMockResponse(Array.Empty<LocationSearchResult>());

            // Act
            await _service.SearchLocationsAsync(searchQuery);

            // Assert
            Verify(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri != null &&
                req.RequestUri.ToString().Equals("https://api.example.com/locations?text=Berlin"));
        }

        [Test]
        public async Task ReturnsEmptyList_WhenApiReturnsEmptyArray()
        {
            // Arrange
            SetupMockResponse(Array.Empty<LocationSearchResult>());

            // Act
            var result = await _service.SearchLocationsAsync("anything");

            // Assert
            await Assert.That(result).IsNotNull();
            await Assert.That(result).HasCount().EqualTo(0);
        }

        [Test]
        public async Task ThrowsInvalidOperationException_WhenApiReturnsNull()
        {
            // Arrange
            var searchQuery = "Berlin";

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create<IEnumerable<LocationSearchResult>?>(null)
                });

            // Act
            var act = async () =>
            {
                await _service.SearchLocationsAsync(searchQuery);
            };

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(act);
        }

        [Test]
        public async Task ThrowsInvalidOperationException_WhenApiReturnsErrorStatus()
        {
            // Arrange
            var searchQuery = "Berlin";

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("{\"error\":\"Internal server error\"}")
                });

            // Act
            var act = async () =>
            {
                await _service.SearchLocationsAsync(searchQuery);
            };

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(act);
        }

        [Test]
        public async Task ReturnsMultipleLocations_WhenApiReturnsMultipleResults()
        {
            // Arrange
            var searchQuery = "Springfield";
            var expectedResults = new[]
            {
                new LocationSearchResult
                {
                    Label = "Springfield, Illinois, USA",
                    Longitude = -89.644164,
                    Latitude = 39.781721,
                    Name = "Springfield",
                    Country = "USA",
                    Region = "Illinois"
                },
                new LocationSearchResult
                {
                    Label = "Springfield, Massachusetts, USA",
                    Longitude = -72.589811,
                    Latitude = 42.101483,
                    Name = "Springfield",
                    Country = "USA",
                    Region = "Massachusetts"
                }
            };

            SetupMockResponse(expectedResults);

            // Act
            var result = await _service.SearchLocationsAsync(searchQuery);

            // Assert
            await Assert.That(result).IsNotNull();
            var resultList = result.ToList();
            await Assert.That(resultList).HasCount().EqualTo(2);
            await Assert.That(resultList[0].Region).IsEqualTo("Illinois");
            await Assert.That(resultList[1].Region).IsEqualTo("Massachusetts");
        }

        [Test]
        public async Task ReturnsLocationWithAllProperties_WhenApiReturnsCompleteData()
        {
            // Arrange
            var searchQuery = "Main Street 123, Berlin";
            var expectedResults = new[]
            {
                new LocationSearchResult
                {
                    Label = "Main Street 123, 10115 Berlin, Germany",
                    Longitude = 13.404954,
                    Latitude = 52.520008,
                    Name = "Main Street 123",
                    Street = "Main Street",
                    HouseNumber = "123",
                    PostalCode = "10115",
                    Country = "Germany",
                    Region = "Berlin",
                    County = "Berlin",
                    Locality = "Berlin"
                }
            };

            SetupMockResponse(expectedResults);

            // Act
            var result = await _service.SearchLocationsAsync(searchQuery);

            // Assert
            await Assert.That(result).IsNotNull();
            var location = result.First();
            await Assert.That(location.Label).IsEqualTo("Main Street 123, 10115 Berlin, Germany");
            await Assert.That(location.Street).IsEqualTo("Main Street");
            await Assert.That(location.HouseNumber).IsEqualTo("123");
            await Assert.That(location.PostalCode).IsEqualTo("10115");
            await Assert.That(location.Country).IsEqualTo("Germany");
            await Assert.That(location.Region).IsEqualTo("Berlin");
            await Assert.That(location.County).IsEqualTo("Berlin");
            await Assert.That(location.Locality).IsEqualTo("Berlin");
        }

        [Test]
        public async Task ThrowsInvalidOperationException_WhenHttpRequestFails()
        {
            // Arrange
            var searchQuery = "Berlin";

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            // Act
            var act = async () =>
            {
                await _service.SearchLocationsAsync(searchQuery);
            };

            // Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);

            var innerException = exception?.InnerException;
            await Assert.That(innerException).IsNotNull();
            await Assert.That(innerException!).IsTypeOf<HttpRequestException>();
        }
    }
}
