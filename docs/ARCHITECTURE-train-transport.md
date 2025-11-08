# Architecture Design: Train Transport Mode Support (Final)

## Overview

This document outlines the architecture for adding configurable transport mode support to Projectcoordinator, specifically enabling train time calculations while maintaining car-based distance calculations for cost estimation.

**Key Design Principle**: Separate **time calculation** (varies by transport mode) from **cost calculation** (always car-based) with efficient, replaceable calculators.

## Requirements

1. ✅ Each place should be configurable for either car or train time calculation
2. ✅ Cost calculation must ALWAYS use car distance (regardless of transport mode)
3. ✅ Time calculation should respect the configured transport mode
4. ✅ Existing places default to car mode (backward compatibility)
5. ✅ **Route calculation services must remain easily replaceable**
6. ✅ **Efficient**: Single API call per calculator (not separate time/distance calls)
7. ✅ **No business logic in orchestrator**: Calculators know their own capabilities

## Proposed Architecture

### Architecture Overview

```
ITripService (unchanged public contract)
    └── TripOrchestrationService
            ├── IPlaceRepository (data access)
            ├── Dictionary<TransportMode, ITimeCalculator> (time per mode)
            │       ├── CarTimeCalculator (OpenRouteService driving-car)
            │       └── TrainTimeCalculator (OpenRouteService cycling, replaceable)
            └── ICostCalculator (single - always car)
                    └── CarDistanceCostCalculator (OpenRouteService driving-car)
```

**Key Design Decisions**:

1. **Separate Time and Cost**: Different interfaces with different responsibilities
2. **Single Calculator Call**: `RouteResult` contains BOTH time AND distance
3. **Dictionary Resolution**: Use `Dictionary<TransportMode, ITimeCalculator>` for lookup
4. **Cost Calculator Returns Distance**: Business logic (cost = distance * 30) stays in orchestrator
5. **No Duplicate Work**: Cost calculator only called once for all places

### 1. Data Model Changes

#### 1.1 Add Transport Mode Enum

**Location**: `src/Shared/TransportMode.cs`

```csharp
namespace ZgM.ProjectCoordinator.Shared
{
    public enum TransportMode
    {
        Car = 0,  // Default for backward compatibility
        Train = 1
    }
}
```

#### 1.2 Update Models

**Update these files** (add `TransportMode` field with default `= TransportMode.Car`):
- `src/api/Entities/PlaceEntity.cs`
- `src/Shared/PlaceRequest.cs`
- `src/api/Models/PlaceRequest.cs` (update `FromShared` method too)
- `src/Shared/Place.cs`

### 2. Service Layer Changes

#### 2.1 Create RouteResult Model

**Location**: `src/api/Models/RouteResult.cs`

```csharp
using ZgM.ProjectCoordinator.Shared;

namespace api.Models
{
    /// <summary>
    /// Result of a route calculation for a single place
    /// </summary>
    public sealed class RouteResult
    {
        public required PlaceId PlaceId { get; init; }
        
        /// <summary>
        /// Travel time in seconds
        /// </summary>
        public required double DurationSeconds { get; init; }
        
        /// <summary>
        /// Distance in meters
        /// </summary>
        public required double DistanceMeters { get; init; }
    }
}
```

#### 2.2 Create ITimeCalculator Interface

**Location**: `src/api/Services/ITimeCalculator.cs`

```csharp
using api.Entities;
using api.Models;

namespace api.Services
{
    /// <summary>
    /// Calculates travel time (and optionally distance) for a specific transport mode.
    /// Each implementation is easily replaceable with different API providers.
    /// </summary>
    public interface ITimeCalculator
    {
        /// <summary>
        /// Calculate routes from origin to multiple places.
        /// Returns both duration and distance in a single API call for efficiency.
        /// </summary>
        Task<IEnumerable<RouteResult>> CalculateRoutesAsync(
            IEnumerable<PlaceEntity> places,
            double originLatitude,
            double originLongitude);
    }
}
```

#### 2.3 Create ICostCalculator Interface

**Location**: `src/api/Services/ICostCalculator.cs`

```csharp
using api.Entities;
using ZgM.ProjectCoordinator.Shared;

namespace api.Services
{
    /// <summary>
    /// Calculates cost basis (distance) for trips.
    /// Always uses car-based distance regardless of time calculation mode.
    /// </summary>
    public interface ICostCalculator
    {
        /// <summary>
        /// Calculate distances from origin to multiple places.
        /// Returns distance in meters.
        /// </summary>
        Task<Dictionary<PlaceId, double>> CalculateDistancesAsync(
            IEnumerable<PlaceEntity> places,
            double originLatitude,
            double originLongitude);
    }
}
```

#### 2.4 Implement CarTimeCalculator

**Location**: `src/api/Services/CarTimeCalculator.cs`

```csharp
using api.Entities;
using api.Exceptions;
using api.Extensions;
using api.Models;
using api.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using ZgM.ProjectCoordinator.Shared;

namespace api.Services
{
    /// <summary>
    /// Calculates car routes using OpenRouteService driving-car profile.
    /// Easily replaceable with HERE Maps, Google Maps, etc.
    /// </summary>
    public sealed class CarTimeCalculator : ITimeCalculator
    {
        private readonly HttpClient client;
        private readonly ILogger<CarTimeCalculator> logger;
        private static readonly JsonSerializerOptions _serializeOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public CarTimeCalculator(
            IHttpClientFactory clientFactory,
            IOptions<OpenRouteServiceOptions> options,
            ILogger<CarTimeCalculator> logger)
        {
            client = clientFactory.CreateClient().ConfigureForOpenRouteService(options.Value);
            this.logger = logger;
        }

        public async Task<IEnumerable<RouteResult>> CalculateRoutesAsync(
            IEnumerable<PlaceEntity> places,
            double originLatitude,
            double originLongitude)
        {
            var placesList = places.ToList();
            
            var response = await client.PostAsJsonAsync("v2/matrix/driving-car", new
            {
                locations = placesList.Select(p => new[] { p.Longitude, p.Latitude })
                                     .Append([originLongitude, originLatitude])
                                     .ToArray(),
                destinations = Enumerable.Range(placesList.Count, 1),
                sources = Enumerable.Range(0, placesList.Count),
                metrics = new[] { "duration", "distance" } // Both in one call
            });

            string responseBody = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                try
                {
                    var result = JsonSerializer.Deserialize<OpenRouteServiceMatrixResponse>(
                        responseBody, _serializeOptions);

                    return placesList.Select((place, index) => new RouteResult
                    {
                        PlaceId = place.Id,
                        DurationSeconds = result.Durations[index].Single(),
                        DistanceMeters = result.Distances[index].Single()
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to deserialize matrix from OpenRouteService: {Content}",
                        responseBody);
                    throw new ProblemDetailsException(
                        System.Net.HttpStatusCode.InternalServerError,
                        "Internal Server Error",
                        "Failed to calculate car routes");
                }
            }
            else
            {
                logger.LogError("OpenRouteService returned {Status}: {Content}",
                    response.StatusCode, responseBody);
                throw new ProblemDetailsException(
                    System.Net.HttpStatusCode.InternalServerError,
                    "Internal Server Error",
                    "Failed to calculate car routes");
            }
        }
    }
}
```

#### 2.5 Implement TrainTimeCalculator

**Location**: `src/api/Services/TrainTimeCalculator.cs`

```csharp
using api.Entities;
using api.Exceptions;
using api.Extensions;
using api.Models;
using api.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace api.Services
{
    /// <summary>
    /// Calculates train/public transport routes using OpenRouteService cycling profile as proxy.
    /// TODO: Replace with proper transit API (Google Transit, Deutsche Bahn, HERE Transit, etc.)
    /// </summary>
    public sealed class TrainTimeCalculator : ITimeCalculator
    {
        private readonly HttpClient client;
        private readonly ILogger<TrainTimeCalculator> logger;
        private static readonly JsonSerializerOptions _serializeOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public TrainTimeCalculator(
            IHttpClientFactory clientFactory,
            IOptions<OpenRouteServiceOptions> options,
            ILogger<TrainTimeCalculator> logger)
        {
            client = clientFactory.CreateClient().ConfigureForOpenRouteService(options.Value);
            this.logger = logger;
        }

        public async Task<IEnumerable<RouteResult>> CalculateRoutesAsync(
            IEnumerable<PlaceEntity> places,
            double originLatitude,
            double originLongitude)
        {
            var placesList = places.ToList();
            
            // Using cycling-regular as proxy for public transport
            var response = await client.PostAsJsonAsync("v2/matrix/cycling-regular", new
            {
                locations = placesList.Select(p => new[] { p.Longitude, p.Latitude })
                                     .Append([originLongitude, originLatitude])
                                     .ToArray(),
                destinations = Enumerable.Range(placesList.Count, 1),
                sources = Enumerable.Range(0, placesList.Count),
                metrics = new[] { "duration", "distance" }
            });

            string responseBody = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                try
                {
                    var result = JsonSerializer.Deserialize<OpenRouteServiceMatrixResponse>(
                        responseBody, _serializeOptions);

                    return placesList.Select((place, index) => new RouteResult
                    {
                        PlaceId = place.Id,
                        DurationSeconds = result.Durations[index].Single(),
                        DistanceMeters = result.Distances[index].Single()
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to deserialize matrix from OpenRouteService: {Content}",
                        responseBody);
                    throw new ProblemDetailsException(
                        System.Net.HttpStatusCode.InternalServerError,
                        "Internal Server Error",
                        "Failed to calculate train routes");
                }
            }
            else
            {
                logger.LogError("OpenRouteService returned {Status}: {Content}",
                    response.StatusCode, responseBody);
                throw new ProblemDetailsException(
                    System.Net.HttpStatusCode.InternalServerError,
                    "Internal Server Error",
                    "Failed to calculate train routes");
            }
        }
    }
}
```

#### 2.6 Implement CarDistanceCostCalculator

**Location**: `src/api/Services/CarDistanceCostCalculator.cs`

```csharp
using api.Entities;
using api.Exceptions;
using api.Extensions;
using api.Models;
using api.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using ZgM.ProjectCoordinator.Shared;

namespace api.Services
{
    /// <summary>
    /// Calculates distances for cost calculation, always using car routes.
    /// Easily replaceable with different map providers.
    /// </summary>
    public sealed class CarDistanceCostCalculator : ICostCalculator
    {
        private readonly HttpClient client;
        private readonly ILogger<CarDistanceCostCalculator> logger;
        private static readonly JsonSerializerOptions _serializeOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public CarDistanceCostCalculator(
            IHttpClientFactory clientFactory,
            IOptions<OpenRouteServiceOptions> options,
            ILogger<CarDistanceCostCalculator> logger)
        {
            client = clientFactory.CreateClient().ConfigureForOpenRouteService(options.Value);
            this.logger = logger;
        }

        public async Task<Dictionary<PlaceId, double>> CalculateDistancesAsync(
            IEnumerable<PlaceEntity> places,
            double originLatitude,
            double originLongitude)
        {
            var placesList = places.ToList();
            
            var response = await client.PostAsJsonAsync("v2/matrix/driving-car", new
            {
                locations = placesList.Select(p => new[] { p.Longitude, p.Latitude })
                                     .Append([originLongitude, originLatitude])
                                     .ToArray(),
                destinations = Enumerable.Range(placesList.Count, 1),
                sources = Enumerable.Range(0, placesList.Count),
                metrics = new[] { "distance" } // Only distance needed for cost
            });

            string responseBody = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                try
                {
                    var result = JsonSerializer.Deserialize<OpenRouteServiceMatrixResponse>(
                        responseBody, _serializeOptions);

                    return placesList.Select((place, index) => new
                    {
                        PlaceId = place.Id,
                        Distance = result.Distances[index].Single()
                    }).ToDictionary(x => x.PlaceId, x => x.Distance);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to deserialize distances from OpenRouteService: {Content}",
                        responseBody);
                    throw new ProblemDetailsException(
                        System.Net.HttpStatusCode.InternalServerError,
                        "Internal Server Error",
                        "Failed to calculate distances for cost");
                }
            }
            else
            {
                logger.LogError("OpenRouteService returned {Status}: {Content}",
                    response.StatusCode, responseBody);
                throw new ProblemDetailsException(
                    System.Net.HttpStatusCode.InternalServerError,
                    "Internal Server Error",
                    "Failed to calculate distances for cost");
            }
        }
    }
}
```

#### 2.7 Create TripOrchestrationService

**Location**: `src/api/Services/TripOrchestrationService.cs`

```csharp
using api.Repositories;
using Microsoft.Extensions.Logging;
using ZgM.ProjectCoordinator.Shared;

namespace api.Services
{
    /// <summary>
    /// Orchestrates trip calculations by delegating to time and cost calculators.
    /// Contains business logic (cost formula) but delegates route calculations.
    /// </summary>
    public sealed class TripOrchestrationService : ITripService
    {
        private readonly IPlaceRepository repository;
        private readonly Dictionary<TransportMode, ITimeCalculator> timeCalculators;
        private readonly ICostCalculator costCalculator;
        private readonly ILogger<TripOrchestrationService> logger;

        public TripOrchestrationService(
            IPlaceRepository repository,
            IEnumerable<KeyValuePair<TransportMode, ITimeCalculator>> timeCalculators,
            ICostCalculator costCalculator,
            ILogger<TripOrchestrationService> logger)
        {
            this.repository = repository;
            this.timeCalculators = timeCalculators.ToDictionary(x => x.Key, x => x.Value);
            this.costCalculator = costCalculator;
            this.logger = logger;
        }

        public async Task<IEnumerable<Trip>> GetAllTripsAsync(double latitude, double longitude)
        {
            var places = (await repository.GetAllAsync()).ToList();
            if (!places.Any())
            {
                return [];
            }

            // Step 1: Calculate distances for cost (always car-based, single call for all places)
            var distances = await costCalculator.CalculateDistancesAsync(places, latitude, longitude);

            // Step 2: Group places by transport mode
            var placesByMode = places.GroupBy(p => p.TransportMode);

            // Step 3: Calculate time for each transport mode group
            var trips = new List<Trip>();
            foreach (var group in placesByMode)
            {
                if (!timeCalculators.TryGetValue(group.Key, out var calculator))
                {
                    logger.LogWarning("No time calculator found for transport mode {Mode}", group.Key);
                    continue;
                }

                var routes = await calculator.CalculateRoutesAsync(group, latitude, longitude);

                // Step 4: Build trips with time from calculator and cost from distances
                foreach (var route in routes)
                {
                    var place = group.First(p => p.Id == route.PlaceId);
                    
                    trips.Add(new Trip
                    {
                        Place = new Place
                        {
                            Id = place.Id,
                            Name = place.Name,
                            UserId = place.UserId,
                            TransportMode = place.TransportMode
                        },
                        Time = TimeSpan.FromSeconds(route.DurationSeconds),
                        Cost = CalculateCost(distances[route.PlaceId]) // Business logic here
                    });
                }
            }

            return trips;
        }

        private static ushort CalculateCost(double distanceMeters)
        {
            // Business rule: 30 cents per kilometer
            return (ushort)(Math.Ceiling(distanceMeters / 1000) * 30);
        }
    }
}
```

#### 2.8 Update Dependency Injection

**Location**: `src/api/Program.cs`

Replace:
```csharp
services.AddScoped<ITripService, TripOpenRouteService>();
```

With:
```csharp
// Register time calculators with their transport modes
services.AddScoped<KeyValuePair<TransportMode, ITimeCalculator>>(sp =>
    new KeyValuePair<TransportMode, ITimeCalculator>(
        TransportMode.Car,
        sp.GetRequiredService<CarTimeCalculator>()));

services.AddScoped<KeyValuePair<TransportMode, ITimeCalculator>>(sp =>
    new KeyValuePair<TransportMode, ITimeCalculator>(
        TransportMode.Train,
        sp.GetRequiredService<TrainTimeCalculator>()));

// Register concrete calculator implementations
services.AddScoped<CarTimeCalculator>();
services.AddScoped<TrainTimeCalculator>();

// Register cost calculator
services.AddScoped<ICostCalculator, CarDistanceCostCalculator>();

// Register orchestration service
services.AddScoped<ITripService, TripOrchestrationService>();
```

#### 2.9 Update PlaceCosmosService

**Location**: `src/api/Services/PlaceCosmosService.cs`

Add `TransportMode` to place creation and retrieval (3 methods to update):
- `AddPlace`: Add `TransportMode = placeRequest.TransportMode` when creating PlaceEntity and Place
- `GetAllPlacesAsync`: Add `TransportMode = entity.TransportMode` when mapping to Place
- `GetPlacesAsync`: Add `TransportMode = entity.TransportMode` when mapping to Place

### 3. Benefits of This Design

| Aspect | How it's Achieved |
|--------|-------------------|
| **Easy to Replace** | Want Google Maps for cars? Create `GoogleMapsTimeCalculator`, register with `TransportMode.Car` |
| **Efficient** | Single API call per transport mode group (both time and distance) |
| **No Business Logic Leakage** | Orchestrator only knows: "get time from calculator, get distance from cost calculator, apply formula" |
| **No Duplicate Registrations** | Use `KeyValuePair<TransportMode, ITimeCalculator>` pattern for DI |
| **Testable** | Mock `ITimeCalculator` for specific mode, mock `ICostCalculator` independently |
| **Extensible** | Add bike mode: create `BikeTimeCalculator`, register with `TransportMode.Bike` |

### 4. How to Replace a Calculator

**Example: Replace TrainTimeCalculator with Google Transit API**

**Step 1**: Create new calculator
```csharp
public sealed class GoogleTransitCalculator : ITimeCalculator
{
    public async Task<IEnumerable<RouteResult>> CalculateRoutesAsync(...)
    {
        // Call Google Directions API with mode=transit
        // Parse response and return RouteResult collection
    }
}
```

**Step 2**: Update DI registration (ONE LINE CHANGE)
```csharp
services.AddScoped<KeyValuePair<TransportMode, ITimeCalculator>>(sp =>
    new KeyValuePair<TransportMode, ITimeCalculator>(
        TransportMode.Train,
        sp.GetRequiredService<GoogleTransitCalculator>())); // Changed this

services.AddScoped<GoogleTransitCalculator>();
```

**Step 3**: Done! No other changes needed.

### 5. Testing Strategy

#### Unit Tests

1. **`CarTimeCalculatorTests.cs`**
   - Test successful route calculation
   - Test both duration and distance returned
   - Test error handling

2. **`TrainTimeCalculatorTests.cs`**
   - Same as CarTimeCalculatorTests

3. **`CarDistanceCostCalculatorTests.cs`**
   - Test distance calculation
   - Test error handling

4. **`TripOrchestrationServiceTests.cs`**
   - Test with all car places
   - Test with all train places
   - Test with mixed transport modes
   - Test cost calculation formula
   - Verify single cost calculator call
   - Verify correct time calculator used per mode
   - Test missing calculator for mode

### 6. Implementation Checklist

#### Backend - Phase 1
- [ ] Create `src/Shared/TransportMode.cs`
- [ ] Update PlaceEntity, PlaceRequest, Place models
- [ ] Create `src/api/Models/RouteResult.cs`
- [ ] Create `src/api/Services/ITimeCalculator.cs`
- [ ] Create `src/api/Services/ICostCalculator.cs`
- [ ] Create `src/api/Services/CarTimeCalculator.cs`
- [ ] Create `src/api/Services/TrainTimeCalculator.cs`
- [ ] Create `src/api/Services/CarDistanceCostCalculator.cs`
- [ ] Create `src/api/Services/TripOrchestrationService.cs`
- [ ] Update `src/api/Services/PlaceCosmosService.cs`
- [ ] Update `src/api/Program.cs` DI registration
- [ ] Write unit tests
- [ ] Test locally
- [ ] Code review

#### Frontend - Phase 2
- [ ] Add transport mode selector to place creation form
- [ ] Add transport mode indicator to place display
- [ ] Update fake services for debug mode

### 7. Migration Notes

- **Database**: No migration needed (CosmosDB schema-less, default values)
- **Backward Compatibility**: Existing places without `TransportMode` default to `Car`
- **Old Service**: Keep `TripOpenRouteService.cs` for reference, delete after successful deployment

---

**Document Version**: 3.0 (Final)  
**Author**: AI Assistant  
**Date**: 2025-11-01  
**Status**: Proposed  
**Key Changes**: 
- Separated time and cost calculators
- Single API call per calculator (efficiency)
- Dictionary-based DI pattern (no duplicate registrations)
- Business logic stays in orchestrator
