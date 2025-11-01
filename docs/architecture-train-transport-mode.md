# Architecture Design: Train Transport Mode Support (Revised)

## Overview

This document outlines the architecture for adding configurable transport mode support to Projectcoordinator, specifically enabling train time calculations while maintaining car-based distance calculations for cost estimation.

**Key Design Principle**: Maintain **single responsibility** and **replaceability** of route calculation services through a **Strategy Pattern** architecture.

## Current State

### Data Flow

```
User Location → TripOpenRouteService → OpenRouteService API (driving-car) → Trip[]
                                          ↓
                                    [Time + Distance]
                                          ↓
                                    Time = duration
                                    Cost = distance * 30 cents/km
```

### Current Implementation

- **PlaceEntity**: Stores location (lat/lon) and name
- **TripOpenRouteService**: Calculates trips using OpenRouteService Matrix API with "driving-car" profile
- **Trip Model**: Contains Place, Time (TimeSpan), and Cost (ushort in cents)
- **Cost Calculation**: `Cost = ceil(distance_km) * 30`

## Requirements

1. ✅ Each place should be configurable for either car or train time calculation
2. ✅ Cost calculation must ALWAYS use car distance (regardless of transport mode)
3. ✅ Time calculation should respect the configured transport mode
4. ✅ Existing places default to car mode (backward compatibility)
5. ✅ **Route calculation services must remain easily replaceable** (preserve original design intent)

## Proposed Architecture

### Architecture Overview: Strategy Pattern

Instead of modifying `TripOpenRouteService` to handle multiple transport modes, we **separate concerns**:

```
ITripService (unchanged public contract)
    └── TripOrchestrationService (new implementation)
            ├── IPlaceRepository (data access)
            └── IRouteCalculatorFactory (strategy selector)
                    └── IRouteCalculator (strategy interface)
                            ├── CarRouteCalculator (OpenRouteService driving-car)
                            └── TrainRouteCalculator (OpenRouteService cycling, easily replaceable)
```

**Benefits**:

- ✅ **Easy to replace**: Each calculator is independent, can use different API providers
- ✅ **Easy to extend**: Add new transport modes without modifying existing code
- ✅ **Single Responsibility**: Each calculator only knows about one transport mode
- ✅ **Testable**: Mock individual calculators independently
- ✅ **Open/Closed Principle**: Extend functionality without modifying existing services

### 1. Data Model Changes

#### 1.1 Add Transport Mode Enum (Shared)

**Location**: `src/Shared/TransportMode.cs`

```csharp
namespace ZgM.ProjectCoordinator.Shared
{
    /// <summary>
    /// Transport mode for calculating travel time
    /// </summary>
    public enum TransportMode
    {
        /// <summary>
        /// Calculate time using car/driving profile
        /// </summary>
        Car = 0,  // Default value for backward compatibility
        
        /// <summary>
        /// Calculate time using public transport (train) profile
        /// </summary>
        Train = 1
    }
}
```

#### 1.2 Update PlaceEntity

**Location**: `src/api/Entities/PlaceEntity.cs`

```csharp
public struct PlaceEntity
{
    public required UserId UserId { get; init; }
    public required PlaceId Id { get; init; }
    public required string Name { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
    public TransportMode TransportMode { get; init; } = TransportMode.Car; // New field
}
```

#### 1.3 Update PlaceRequest Models

**Location**: `src/Shared/PlaceRequest.cs`

```csharp
public struct PlaceRequest
{
    public required string Name { get; init; }
    public required double Longitude { get; init; }
    public required double Latitude { get; init; }
    public TransportMode TransportMode { get; init; } = TransportMode.Car; // New field
}
```

**Location**: `src/api/Models/PlaceRequest.cs`

```csharp
public struct PlaceRequest
{
    public required UserId UserId { get; init; }
    public required string Name { get; init; }
    public required double Longitude { get; init; }
    public required double Latitude { get; init; }
    public TransportMode TransportMode { get; init; } = TransportMode.Car; // New field

    public static PlaceRequest FromShared(ZgM.ProjectCoordinator.Shared.PlaceRequest request, UserId userId)
    {
        return new PlaceRequest
        {
            UserId = userId,
            Name = request.Name,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            TransportMode = request.TransportMode // Pass through
        };
    }
}
```

#### 1.4 Update Place Model (Shared)

**Location**: `src/Shared/Place.cs`

```csharp
public struct Place
{
    public required PlaceId Id { get; set; }
    public required UserId UserId { get; set; }
    public required string Name { get; set; }
    public TransportMode TransportMode { get; set; } = TransportMode.Car; // New field

    public override string ToString()
    {
        return Name;
    }
}
```

### 2. Service Layer Changes

#### 2.1 Create IRouteCalculator Interface

**Location**: `src/api/Services/IRouteCalculator.cs`

This is the **Strategy Interface** - each transport mode has its own implementation.

```csharp
using api.Entities;
using ZgM.ProjectCoordinator.Shared;

namespace api.Services
{
    /// <summary>
    /// Calculates route metrics (duration and distance) for a specific transport mode.
    /// Implementations can use different APIs or calculation methods.
    /// This enables easy replacement of route calculation providers.
    /// </summary>
    public interface IRouteCalculator
    {
        /// <summary>
        /// The transport mode this calculator supports
        /// </summary>
        TransportMode SupportedMode { get; }

        /// <summary>
        /// Calculate duration in seconds from origin to each place
        /// </summary>
        /// <param name="places">Places to calculate routes to</param>
        /// <param name="originLatitude">Starting point latitude</param>
        /// <param name="originLongitude">Starting point longitude</param>
        /// <returns>Dictionary mapping PlaceId to duration in seconds</returns>
        Task<Dictionary<PlaceId, double>> CalculateDurationsAsync(
            IEnumerable<PlaceEntity> places,
            double originLatitude,
            double originLongitude);

        /// <summary>
        /// Calculate distance in meters from origin to each place
        /// </summary>
        /// <param name="places">Places to calculate routes to</param>
        /// <param name="originLatitude">Starting point latitude</param>
        /// <param name="originLongitude">Starting point longitude</param>
        /// <returns>Dictionary mapping PlaceId to distance in meters</returns>
        Task<Dictionary<PlaceId, double>> CalculateDistancesAsync(
            IEnumerable<PlaceEntity> places,
            double originLatitude,
            double originLongitude);
    }
}
```

#### 2.2 Implement CarRouteCalculator

**Location**: `src/api/Services/CarRouteCalculator.cs`

This extracts the current `TripOpenRouteService` logic into a dedicated calculator. Easy to replace with HERE Maps, Google Maps, etc.

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
    /// Can be replaced with alternative providers (HERE, Google Maps, etc.)
    /// </summary>
    public sealed class CarRouteCalculator : IRouteCalculator
    {
        private readonly HttpClient client;
        private readonly ILogger<CarRouteCalculator> logger;
        private static readonly JsonSerializerOptions _serializeOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public TransportMode SupportedMode => TransportMode.Car;

        public CarRouteCalculator(
            IHttpClientFactory clientFactory,
            IOptions<OpenRouteServiceOptions> options,
            ILogger<CarRouteCalculator> logger)
        {
            client = clientFactory.CreateClient().ConfigureForOpenRouteService(options.Value);
            this.logger = logger;
        }

        public async Task<Dictionary<PlaceId, double>> CalculateDurationsAsync(
            IEnumerable<PlaceEntity> places,
            double originLatitude,
            double originLongitude)
        {
            return await CalculateMetricAsync(places, originLatitude, originLongitude, "duration");
        }

        public async Task<Dictionary<PlaceId, double>> CalculateDistancesAsync(
            IEnumerable<PlaceEntity> places,
            double originLatitude,
            double originLongitude)
        {
            return await CalculateMetricAsync(places, originLatitude, originLongitude, "distance");
        }

        private async Task<Dictionary<PlaceId, double>> CalculateMetricAsync(
            IEnumerable<PlaceEntity> places,
            double originLatitude,
            double originLongitude,
            string metric)
        {
            var placesList = places.ToList();
            var response = await client.PostAsJsonAsync("v2/matrix/driving-car", new
            {
                locations = placesList.Select(p => new[] { p.Longitude, p.Latitude })
                                     .Append([originLongitude, originLatitude])
                                     .ToArray(),
                destinations = Enumerable.Range(placesList.Count, 1),
                sources = Enumerable.Range(0, placesList.Count),
                metrics = new[] { metric }
            });

            string responseBody = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                try
                {
                    var result = JsonSerializer.Deserialize<OpenRouteServiceMatrixResponse>(
                        responseBody, _serializeOptions);

                    var values = metric == "duration" ? result.Durations : result.Distances;

                    return placesList.Select((place, index) => new
                    {
                        PlaceId = place.Id,
                        Value = values[index].Single()
                    }).ToDictionary(x => x.PlaceId, x => x.Value);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to deserialize {Metric} from OpenRouteService: {Content}",
                        metric, responseBody);
                    throw new ProblemDetailsException(
                        System.Net.HttpStatusCode.InternalServerError,
                        "Internal Server Error",
                        $"Failed to get {metric} from route service");
                }
            }
            else
            {
                logger.LogError("OpenRouteService returned {Status} for {Metric}: {Content}",
                    response.StatusCode, metric, responseBody);
                throw new ProblemDetailsException(
                    System.Net.HttpStatusCode.InternalServerError,
                    "Internal Server Error",
                    $"Failed to get {metric} from route service");
            }
        }
    }
}
```

#### 2.3 Implement TrainRouteCalculator

**Location**: `src/api/Services/TrainRouteCalculator.cs`

Similar structure to CarRouteCalculator. Uses cycling profile as proxy. **Easily replaceable** with Deutsche Bahn API, Google Transit, etc.

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
    /// Calculates train/public transport routes using OpenRouteService cycling profile as proxy.
    /// TODO: Replace with proper transit API (Google Directions Transit, Deutsche Bahn API, HERE Transit, etc.)
    /// </summary>
    public sealed class TrainRouteCalculator : IRouteCalculator
    {
        private readonly HttpClient client;
        private readonly ILogger<TrainRouteCalculator> logger;
        private static readonly JsonSerializerOptions _serializeOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public TransportMode SupportedMode => TransportMode.Train;

        public TrainRouteCalculator(
            IHttpClientFactory clientFactory,
            IOptions<OpenRouteServiceOptions> options,
            ILogger<TrainRouteCalculator> logger)
        {
            client = clientFactory.CreateClient().ConfigureForOpenRouteService(options.Value);
            this.logger = logger;
        }

        public async Task<Dictionary<PlaceId, double>> CalculateDurationsAsync(
            IEnumerable<PlaceEntity> places,
            double originLatitude,
            double originLongitude)
        {
            return await CalculateMetricAsync(places, originLatitude, originLongitude, "duration");
        }

        public async Task<Dictionary<PlaceId, double>> CalculateDistancesAsync(
            IEnumerable<PlaceEntity> places,
            double originLatitude,
            double originLongitude)
        {
            return await CalculateMetricAsync(places, originLatitude, originLongitude, "distance");
        }

        private async Task<Dictionary<PlaceId, double>> CalculateMetricAsync(
            IEnumerable<PlaceEntity> places,
            double originLatitude,
            double originLongitude,
            string metric)
        {
            var placesList = places.ToList();
            
            // Using cycling-regular as proxy for public transport
            // This is a temporary solution until proper transit API is integrated
            var response = await client.PostAsJsonAsync("v2/matrix/cycling-regular", new
            {
                locations = placesList.Select(p => new[] { p.Longitude, p.Latitude })
                                     .Append([originLongitude, originLatitude])
                                     .ToArray(),
                destinations = Enumerable.Range(placesList.Count, 1),
                sources = Enumerable.Range(0, placesList.Count),
                metrics = new[] { metric }
            });

            string responseBody = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                try
                {
                    var result = JsonSerializer.Deserialize<OpenRouteServiceMatrixResponse>(
                        responseBody, _serializeOptions);

                    var values = metric == "duration" ? result.Durations : result.Distances;

                    return placesList.Select((place, index) => new
                    {
                        PlaceId = place.Id,
                        Value = values[index].Single()
                    }).ToDictionary(x => x.PlaceId, x => x.Value);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to deserialize {Metric} from OpenRouteService: {Content}",
                        metric, responseBody);
                    throw new ProblemDetailsException(
                        System.Net.HttpStatusCode.InternalServerError,
                        "Internal Server Error",
                        $"Failed to get {metric} from route service");
                }
            }
            else
            {
                logger.LogError("OpenRouteService returned {Status} for {Metric}: {Content}",
                    response.StatusCode, metric, responseBody);
                throw new ProblemDetailsException(
                    System.Net.HttpStatusCode.InternalServerError,
                    "Internal Server Error",
                    $"Failed to get {metric} from route service");
            }
        }
    }
}
```

#### 2.4 Create Route Calculator Factory

**Location**: `src/api/Services/IRouteCalculatorFactory.cs`

```csharp
using ZgM.ProjectCoordinator.Shared;

namespace api.Services
{
    /// <summary>
    /// Factory for getting the appropriate route calculator for a transport mode
    /// </summary>
    public interface IRouteCalculatorFactory
    {
        /// <summary>
        /// Gets the route calculator that supports the specified transport mode
        /// </summary>
        /// <param name="mode">Transport mode</param>
        /// <returns>Route calculator for the specified mode</returns>
        /// <exception cref="NotSupportedException">If no calculator exists for the mode</exception>
        IRouteCalculator GetCalculator(TransportMode mode);
    }
}
```

**Location**: `src/api/Services/RouteCalculatorFactory.cs`

```csharp
using ZgM.ProjectCoordinator.Shared;

namespace api.Services
{
    public sealed class RouteCalculatorFactory : IRouteCalculatorFactory
    {
        private readonly IEnumerable<IRouteCalculator> calculators;

        public RouteCalculatorFactory(IEnumerable<IRouteCalculator> calculators)
        {
            this.calculators = calculators;
        }

        public IRouteCalculator GetCalculator(TransportMode mode)
        {
            var calculator = calculators.FirstOrDefault(c => c.SupportedMode == mode);
            if (calculator == null)
            {
                throw new NotSupportedException($"No route calculator found for transport mode: {mode}");
            }
            return calculator;
        }
    }
}
```

#### 2.5 Create Trip Orchestration Service

**Keep ITripService interface unchanged** - this preserves the existing contract.

**Create new orchestration service**:

**Location**: `src/api/Services/TripOrchestrationService.cs`

```csharp
using api.Entities;
using api.Repositories;
using Microsoft.Extensions.Logging;
using ZgM.ProjectCoordinator.Shared;

namespace api.Services
{
    /// <summary>
    /// Orchestrates trip calculations by coordinating multiple route calculators
    /// and applying business rules (e.g., cost always based on car distance).
    /// This service delegates route calculation to pluggable IRouteCalculator implementations.
    /// </summary>
    public sealed class TripOrchestrationService : ITripService
    {
        private readonly IPlaceRepository repository;
        private readonly IRouteCalculatorFactory calculatorFactory;
        private readonly ILogger<TripOrchestrationService> logger;

        public TripOrchestrationService(
            IPlaceRepository repository,
            IRouteCalculatorFactory calculatorFactory,
            ILogger<TripOrchestrationService> logger)
        {
            this.repository = repository;
            this.calculatorFactory = calculatorFactory;
            this.logger = logger;
        }

        public async Task<IEnumerable<Trip>> GetAllTripsAsync(double latitude, double longitude)
        {
            var places = (await repository.GetAllAsync()).ToList();
            if (!places.Any())
            {
                return [];
            }

            // Step 1: ALWAYS calculate car distances for cost calculation (business rule)
            var carCalculator = calculatorFactory.GetCalculator(TransportMode.Car);
            var carDistances = await carCalculator.CalculateDistancesAsync(
                places, latitude, longitude);

            // Step 2: Group places by transport mode
            var placesByMode = places.GroupBy(p => p.TransportMode).ToList();

            // Step 3: Calculate durations for each transport mode group using appropriate calculator
            var trips = new List<Trip>();
            foreach (var group in placesByMode)
            {
                var calculator = calculatorFactory.GetCalculator(group.Key);
                var durations = await calculator.CalculateDurationsAsync(
                    group, latitude, longitude);

                trips.AddRange(BuildTrips(group, durations, carDistances));
            }

            return trips;
        }

        private IEnumerable<Trip> BuildTrips(
            IEnumerable<PlaceEntity> places,
            Dictionary<PlaceId, double> durations,
            Dictionary<PlaceId, double> carDistances)
        {
            return places.Select(place => new Trip
            {
                Place = new Place
                {
                    Id = place.Id,
                    Name = place.Name,
                    UserId = place.UserId,
                    TransportMode = place.TransportMode
                },
                Time = TimeSpan.FromSeconds(durations[place.Id]),
                Cost = (ushort)(Math.Ceiling(carDistances[place.Id] / 1000) * 30) // Always car-based
            });
        }
    }
}
```

#### 2.6 Update Dependency Injection

**Location**: `src/api/Program.cs`

Replace:

```csharp
services.AddScoped<ITripService, TripOpenRouteService>();
```

With:

```csharp
// Register route calculators (automatically collected by factory)
services.AddScoped<IRouteCalculator, CarRouteCalculator>();
services.AddScoped<IRouteCalculator, TrainRouteCalculator>();

// Register factory
services.AddScoped<IRouteCalculatorFactory, RouteCalculatorFactory>();

// Register orchestration service as ITripService implementation
services.AddScoped<ITripService, TripOrchestrationService>();
```

**Note**: `TripOpenRouteService.cs` should be **kept for reference** during migration, then deleted after successful deployment.

#### 2.7 Update PlaceCosmosService

**Location**: `src/api/Services/PlaceCosmosService.cs`

Update the `AddPlace` method to include TransportMode:

```csharp
public async Task<Place> AddPlace(Models.PlaceRequest placeRequest)
{
    var coordinatesValid = await locationService.ValidateAsync(placeRequest.Latitude, placeRequest.Longitude);
    if (coordinatesValid)
    {
        var place = new PlaceEntity()
        {
            Id = new PlaceId(Guid.NewGuid().ToString()),
            UserId = placeRequest.UserId,
            Name = placeRequest.Name,
            Longitude = placeRequest.Longitude,
            Latitude = placeRequest.Latitude,
            TransportMode = placeRequest.TransportMode  // Add this line
        };
        place = await repository.AddAsync(place);
        return new Place()
        {
            Id = place.Id,
            UserId = place.UserId,
            Name = place.Name,
            TransportMode = place.TransportMode  // Add this line
        };
    }
    else
    {
        throw new ProblemDetailsException(System.Net.HttpStatusCode.BadRequest, "Coordinates not valid", "Failed to find streets to snap to");
    }
}
```

Update the `GetAllPlacesAsync` and `GetPlacesAsync` methods:

```csharp
public async Task<IEnumerable<Place>> GetAllPlacesAsync()
{
    var placeEntities = await repository.GetAllAsync();
    return placeEntities.Select(entity =>
    {
        return new Place
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Name = entity.Name,
            TransportMode = entity.TransportMode  // Add this line
        };
    });
}

public async Task<IEnumerable<Place>> GetPlacesAsync(UserId userId)
{
    var placeEntities = await repository.GetAsync(userId);
    return placeEntities.Select(entity =>
    {
        return new Place
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Name = entity.Name,
            TransportMode = entity.TransportMode  // Add this line
        };
    });
}
```

### 3. OpenRouteService Profile Mapping

**Important Note**: OpenRouteService Matrix API does NOT have a direct "public transport" or "train" profile.

**Current Implementation (Phase 1)**:

- **Car**: `driving-car` profile ✅ (accurate)
- **Train**: `cycling-regular` profile ⚠️ (proxy, not accurate)

**Future Implementations**:

**Option A: Replace TrainRouteCalculator entirely**
Create a new `TrainTransitApiCalculator` that implements `IRouteCalculator` and uses:

- Google Directions API (transit mode)
- Deutsche Bahn API
- HERE Transit API
- OpenTripPlanner

**Option B: Hybrid approach**
Keep TrainRouteCalculator for fallback, add preferred API:

```csharp
services.AddScoped<IRouteCalculator, CarRouteCalculator>();
services.AddScoped<IRouteCalculator, GoogleTransitCalculator>(); // Preferred for train
services.AddScoped<IRouteCalculator, TrainRouteCalculator>(); // Fallback
```

Update factory to prefer first matching calculator.

### 4. Database Migration Strategy

#### 4.1 CosmosDB Schema Evolution

CosmosDB is schema-less, but we need to handle existing documents:

**Approach**: Default value strategy

- New `TransportMode` field is optional with default value `TransportMode.Car`
- Existing documents without this field will deserialize with the default value
- No data migration script required

**Validation**: After deployment, verify in CosmosDB Data Explorer:

```sql
SELECT c.id, c.name, c.transportMode FROM c WHERE c.transportMode = null
```

Should return 0 results after first read operation on each document.

### 5. API Changes

#### 5.1 Update Place Creation Endpoint

**No route changes needed** - the existing endpoint will accept the new optional field:

```json
POST /api/users/{userId}/places
{
  "name": "Home",
  "latitude": 52.52,
  "longitude": 13.405,
  "transportMode": 1  // Optional, defaults to 0 (Car)
}
```

#### 5.2 Update Place Retrieval Endpoints

**Location**: `src/api/GetPlaces.cs`

TransportMode will automatically be included in responses due to struct serialization.

### 6. Frontend Changes

#### 6.1 Update Place Creation Form

Add transport mode selector to the place creation form/page.

```razor
<div class="mb-3">
    <label for="transportMode" class="form-label">Transport Mode</label>
    <select id="transportMode" class="form-select" @bind="request.TransportMode">
        <option value="@TransportMode.Car">🚗 Car</option>
        <option value="@TransportMode.Train">🚆 Train</option>
    </select>
    <div class="form-text">
        Time calculation uses selected mode. Cost is always based on car distance.
    </div>
</div>
```

#### 6.2 Update Place Display

Display transport mode badge on place cards/lists:

```razor
<span class="badge bg-@(place.TransportMode == TransportMode.Car ? "primary" : "success")">
    @(place.TransportMode == TransportMode.Car ? "🚗" : "🚆")
</span>
```

#### 6.3 Update Fake Services (Debug Mode)

**Location**: `src/FrontEnd/Services/FakePlaceService.cs` (or similar)

Ensure fake implementations include TransportMode for local development.

### 7. Testing Strategy

#### 7.1 Unit Tests

**New Test Classes**:

1. **`CarRouteCalculatorTests.cs`**
   - Test successful duration calculation
   - Test successful distance calculation
   - Test error handling (API errors, deserialization errors)
   - Test with empty places list

2. **`TrainRouteCalculatorTests.cs`**
   - Same tests as CarRouteCalculator
   - Document that cycling profile is proxy

3. **`RouteCalculatorFactoryTests.cs`**
   - Test getting calculator by mode
   - Test exception when mode not supported
   - Test with multiple calculators registered

4. **`TripOrchestrationServiceTests.cs`**
   - Test with all car places (backward compatibility)
   - Test with all train places
   - Test with mixed transport modes
   - Test cost always uses car distance
   - Test time uses mode-specific calculator
   - Test empty places list
   - Test grouping logic

**Update Existing Tests**:

- Any tests mocking `TripOpenRouteService` should be updated to mock the new services

#### 7.2 Integration Tests

1. Create place with Car mode → verify in database
2. Create place with Train mode → verify in database
3. Get trips with mixed modes → verify correct calculations
4. Verify existing places still work (backward compatibility)

#### 7.3 Manual Testing Checklist

- [ ] Create new place with Car mode → verify trip calculation
- [ ] Create new place with Train mode → verify trip calculation
- [ ] Create mixed places → verify separate time calculations
- [ ] Verify existing places still work (backward compatibility)
- [ ] Verify cost calculation unchanged for both modes
- [ ] Test with no places (empty state)
- [ ] Test with only train places
- [ ] Test with only car places
- [ ] Verify API error handling

### 8. Configuration Changes

**Location**: `src/api/local.settings.json` (and Azure App Settings)

No new configuration required for Phase 1 - uses existing OpenRouteService API key.

**Future**: When adding third-party transit API:

```json
{
  "TransitApi": {
    "Provider": "GoogleDirections",
    "ApiKey": "...",
    "BaseUrl": "https://maps.googleapis.com/maps/api/"
  }
}
```

### 9. Implementation Phases

#### Phase 1: Core Infrastructure (MVP)

**Goal**: Establish architecture, get train mode working with cycling proxy

1. ✅ Add TransportMode enum to Shared
2. ✅ Update PlaceEntity, PlaceRequest, and Place models
3. ✅ Create IRouteCalculator interface
4. ✅ Implement CarRouteCalculator (extract from TripOpenRouteService)
5. ✅ Implement TrainRouteCalculator (using cycling proxy)
6. ✅ Create IRouteCalculatorFactory and implementation
7. ✅ Create TripOrchestrationService
8. ✅ Update DI configuration
9. ✅ Update PlaceCosmosService
10. ✅ Write unit tests for new services
11. ✅ Test with existing data (verify backward compatibility)
12. ✅ Deploy to staging environment

**Success Criteria**:

- All existing tests pass
- Existing places work without migration
- Can create places with train mode
- Different time calculations per mode
- Cost always car-based

#### Phase 2: Frontend Integration

**Goal**: Allow users to select transport mode

1. ✅ Add transport mode selector to place creation form
2. ✅ Add transport mode indicator to place display
3. ✅ Update place list/grid UI
4. ✅ Update fake services for debug mode
5. ✅ Write frontend component tests
6. ✅ Update user documentation

#### Phase 3: Real Transit API Integration

**Goal**: Replace cycling proxy with actual transit data

1. 🔄 Research and select transit API provider
2. 🔄 Create configuration for transit API
3. 🔄 Implement new calculator (e.g., GoogleTransitCalculator)
4. 🔄 Register new calculator in DI
5. 🔄 Add fallback logic if needed
6. 🔄 Test with real transit data
7. 🔄 Deploy and monitor

#### Phase 4: Future Enhancements

1. 🔄 Add real-time schedule information
2. 🔄 Add transport mode to trip result display (show which calculator was used)
3. 🔄 Add analytics on transport mode usage
4. 🔄 Consider additional transport modes (bike, walk, etc.)
5. 🔄 Add UpdatePlace endpoint to change transport mode
6. 🔄 Consider caching route calculations

### 10. Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| OpenRouteService cycling profile inaccurate for trains | High | Medium | Document as limitation, plan for real transit API in Phase 3 |
| Backward compatibility issues with existing places | Low | High | Use default values, extensive testing, gradual rollout |
| Performance degradation from multiple API calls | Medium | Medium | Monitor API call counts, consider caching, optimize grouping |
| Cost calculation confusion for users | Medium | Low | Clear UI messaging, documentation, help text |
| Factory returns wrong calculator | Low | High | Unit tests, integration tests, logging |
| New dependencies break existing functionality | Low | High | Comprehensive test suite, feature flags for rollback |

### 11. Open Questions & Decisions

1. **Q**: Should we support editing transport mode for existing places?
   **A**: Yes - add UpdatePlace endpoint in Phase 2 or 3

2. **Q**: What happens if OpenRouteService doesn't support cycling in a region?
   **A**: API will return error - need error handling and potentially fallback to car mode with warning

3. **Q**: Should transport mode affect the search radius or trip filtering?
   **A**: No - keep existing logic for now, consider in future based on user feedback

4. **Q**: Should we show which calculator/profile was used in the UI?
   **A**: Yes - helpful for transparency and debugging. Add to Trip model in Phase 3

5. **Q**: Should we cache route calculations?
   **A**: Not in Phase 1. Consider in Phase 3 if performance becomes an issue

6. **Q**: How do we handle calculator failures?
   **A**: Throw ProblemDetailsException, let middleware handle. Consider fallback strategies in Phase 3

### 12. Documentation Updates Required

1. Update README.md with new TransportMode feature
2. Update API documentation (add examples with transportMode field)
3. Add user guide section on transport modes
4. Update architecture diagrams
5. Document OpenRouteService profile limitations
6. Add developer guide on creating new route calculators

### 13. Success Metrics

**Phase 1 Success Criteria**:

- ✅ All existing tests pass
- ✅ New unit tests achieve >80% coverage
- ✅ Zero regression issues with existing places
- ✅ API response time increase <20%
- ✅ Successful deployment without data migration
- ✅ Code review approval

**Phase 2 Success Criteria**:

- ✅ Users can create places with transport mode selection
- ✅ Transport mode visible in UI
- ✅ User feedback positive
- ✅ No UI bugs reported

**Phase 3 Success Criteria**:

- ✅ Train times are accurate (within 10% of actual)
- ✅ Transit API integration stable (99% uptime)
- ✅ Performance acceptable (<2s response time)

---

## Implementation Checklist

### Backend - Phase 1

- [ ] Create `src/Shared/TransportMode.cs`
- [ ] Update `src/api/Entities/PlaceEntity.cs`
- [ ] Update `src/api/Models/PlaceRequest.cs`
- [ ] Update `src/Shared/PlaceRequest.cs`
- [ ] Update `src/Shared/Place.cs`
- [ ] Create `src/api/Services/IRouteCalculator.cs`
- [ ] Create `src/api/Services/CarRouteCalculator.cs`
- [ ] Create `src/api/Services/TrainRouteCalculator.cs`
- [ ] Create `src/api/Services/IRouteCalculatorFactory.cs`
- [ ] Create `src/api/Services/RouteCalculatorFactory.cs`
- [ ] Create `src/api/Services/TripOrchestrationService.cs`
- [ ] Update `src/api/Services/PlaceCosmosService.cs`
- [ ] Update `src/api/Program.cs` DI registration
- [ ] Write `tests/api.Tests.Unit/Services/CarRouteCalculatorTests.cs`
- [ ] Write `tests/api.Tests.Unit/Services/TrainRouteCalculatorTests.cs`
- [ ] Write `tests/api.Tests.Unit/Services/RouteCalculatorFactoryTests.cs`
- [ ] Write `tests/api.Tests.Unit/Services/TripOrchestrationServiceTests.cs`
- [ ] Update existing tests that mock TripOpenRouteService
- [ ] Run all tests and verify pass
- [ ] Test locally with Cosmos emulator
- [ ] Code review

### Frontend - Phase 2

- [ ] Update place creation form/page with transport mode selector
- [ ] Update place display components to show transport mode
- [ ] Add transport mode icons/badges
- [ ] Update fake services (for debug mode)
- [ ] Write component tests
- [ ] Test UI locally
- [ ] User acceptance testing

### DevOps

- [ ] Review CI/CD pipeline (should work without changes)
- [ ] Add monitoring for new route calculators
- [ ] Update deployment documentation
- [ ] Prepare rollback plan
- [ ] Schedule maintenance window if needed

### Documentation

- [ ] Update README.md with transport mode feature
- [ ] Create user guide for transport modes
- [ ] Update API documentation
- [ ] Document architecture decisions
- [ ] Update this document with lessons learned

---

## How to Replace a Route Calculator (Example)

**Scenario**: Replace TrainRouteCalculator with Google Directions Transit API

**Step 1**: Create new calculator class

```csharp
public sealed class GoogleTransitCalculator : IRouteCalculator
{
    public TransportMode SupportedMode => TransportMode.Train;
    // Implement CalculateDurationsAsync using Google API
    // Implement CalculateDistancesAsync using Google API
}
```

**Step 2**: Update DI registration

```csharp
// Replace TrainRouteCalculator
services.AddScoped<IRouteCalculator, CarRouteCalculator>();
services.AddScoped<IRouteCalculator, GoogleTransitCalculator>(); // New
// services.AddScoped<IRouteCalculator, TrainRouteCalculator>(); // Remove or keep as fallback
```

**Step 3**: Add configuration

```csharp
services.AddOptionsWithValidateOnStart<GoogleMapsOptions>()...
```

**Step 4**: Test and deploy

- Unit tests for GoogleTransitCalculator
- Integration tests
- Deploy to staging
- Monitor and validate
- Deploy to production

**No changes needed** to:

- ITripService interface
- TripOrchestrationService
- Any other services
- Frontend
- Database

---

**Document Version**: 2.0  
**Author**: AI Assistant  
**Date**: 2025-11-01  
**Status**: Proposed (Revised based on feedback)  
**Key Change**: Separated route calculation into pluggable strategy pattern to maintain replaceability
