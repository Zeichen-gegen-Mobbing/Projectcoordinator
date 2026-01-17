using api.Entities;
using api.Models;
using api.Options;
using api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TUnit.Assertions.Extensions;
using ZgM.ProjectCoordinator.Shared;

namespace api.Tests.Integration.Services;

/// <summary>
/// Integration tests for TrainTransitousService that make actual HTTP calls to the Transitous API.
/// </summary>
public class TrainTransitousServiceIntegrationTests
{
    private readonly TrainTransitousService _service;
    private readonly Mock<ICarRouteService> _carServiceMock;

    public TrainTransitousServiceIntegrationTests()
    {
        // Mock car service - will be configured per test
        _carServiceMock = new Mock<ICarRouteService>();

        var options = Microsoft.Extensions.Options.Options.Create(new TransitousOptions
        {
            Title = "Transitous",
            BaseUrl = "https://api.transitous.org"
        });

        var loggerMock = new Mock<ILogger<TrainTransitousService>>();

        // Setup real HTTP client for integration tests
        var httpClient = new HttpClient();
        TrainTransitousService.ConfigureClient(httpClient, options.Value);

        _service = new TrainTransitousService(
            httpClient,
            _carServiceMock.Object,
            options,
            loggerMock.Object);
    }

    /// <summary>
    /// Given: Real Transitous API and real coordinates (Berlin to Munich)
    /// When: Calling CalculateRoutesAsync
    /// Then: Returns actual train route results with realistic durations
    /// </summary>
    [Test]
    public async Task CalculatesRealTrainRoutes_WhenCallingActualTransitousApi()
    {
        // Arrange - Real coordinates
        // Origin: Berlin (52.5200° N, 13.4050° E)
        // Destination: Munich (48.1351° N, 11.5820° E)
        var originLat = 52.5200;
        var originLon = 13.4050;

        var places = new List<PlaceEntity>
        {
            new()
            {
                Id = PlaceId.Parse("munich-test"),
                Name = "Munich",
                Latitude = 48.1351,
                Longitude = 11.5820,
                TransportMode = TransportMode.Train,
                UserId = UserId.Parse("00000000-0000-0000-0000-000000000001")
            }
        };

        _carServiceMock
            .Setup(s => s.CalculateRoutesAsync(places, originLat, originLon))
            .ReturnsAsync(new List<CarRouteResult>
            {
                new()
                {
                    PlaceId = PlaceId.Parse("munich-test"),
                    CostCents = 500,
                    DurationSeconds = 3600,
                    DistanceMeters = 50000
                }
            });

        // Act
        var results = (await _service.CalculateRoutesAsync(places, originLat, originLon)).ToList();

        // Assert
        await Assert.That(results.Count).IsEqualTo(1);

        var result = results.Single();
        await Assert.That(result.PlaceId).IsEqualTo(PlaceId.Parse("munich-test"));

        // Train from Berlin to Munich should take between 3-8 hours (10800-28800 seconds)
        // We're averaging outbound + return, so expect realistic durations
        await Assert.That(result.DurationSeconds).IsGreaterThan((uint)0);
        await Assert.That(result.DurationSeconds).IsLessThan((uint)50000); // Less than ~14 hours

        // Should have car cost from the mock
        await Assert.That(result.CostCents).IsEqualTo((uint)500);
    }

    /// <summary>
    /// Given: Real Transitous API with very short distance (Hamburg neighborhood)
    /// When: Calling CalculateRoutesAsync
    /// Then: Returns route with very short duration (50-80 seconds from direct walking route)
    /// </summary>
    [Test]
    public async Task CalculatesShortDistanceRoute_WhenCallingActualTransitousApi()
    {
        // Arrange - Hamburg short distance
        var originLat = 53.584460; // Hamburg origin
        var originLon = 10.060934;

        var places = new List<PlaceEntity>
        {
            new()
            {
                Id = PlaceId.Parse("hamburg-short-test"),
                Name = "Hamburg Nearby",
                Latitude = 53.584917, // Hamburg destination (very close)
                Longitude = 10.060352,
                TransportMode = TransportMode.Train,
                UserId = UserId.Parse("00000000-0000-0000-0000-000000000001")
            }
        };

        _carServiceMock
            .Setup(s => s.CalculateRoutesAsync(places, originLat, originLon))
            .ReturnsAsync(new List<CarRouteResult>
            {
                new()
                {
                    PlaceId = PlaceId.Parse("hamburg-short-test"),
                    CostCents = 500,
                    DurationSeconds = 60,
                    DistanceMeters = 100
                }
            });

        // Act
        var results = (await _service.CalculateRoutesAsync(places, originLat, originLon)).ToList();

        // Assert
        await Assert.That(results.Count).IsEqualTo(1);

        var result = results.Single();

        // Very short trip should use direct walking route (50-80 seconds)
        await Assert.That(result.DurationSeconds).IsGreaterThanOrEqualTo((uint)50);
        await Assert.That(result.DurationSeconds).IsLessThanOrEqualTo((uint)80);
    }
}
