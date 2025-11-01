# Train Transport Mode - Phase 1: Interfaces and Tests

## Status: Ready for Review

## What Has Been Implemented

### 1. Data Models

**Created Files:**
- `src/Shared/TransportMode.cs` - Enum with Car (0) and Train (1)
- `src/api/Models/CarRouteResult.cs` - Result with PlaceId, DurationSeconds, DistanceMeters, CostCents
- `src/api/Models/TrainRouteResult.cs` - Result with PlaceId, DurationSeconds, CostCents

### 2. Service Interfaces

**Created Files:**
- `src/api/Services/ICarRouteService.cs`
  - `CalculateRoutesAsync(places, lat, lon)` → CarRouteResult[]
  - Calculates time, distance, and cost for car routes
  
- `src/api/Services/ITrainRouteService.cs`
  - `CalculateRoutesAsync(places, lat, lon, Task<Dictionary<PlaceId, ushort>> carCosts)` → TrainRouteResult[]
  - Calculates train time, uses provided car costs
  - Accepts Task to enable parallel execution

### 3. Service Implementations (Stubs)

**Created Files:**
- `src/api/Services/CarOpenRouteService.cs` - Throws NotImplementedException
- `src/api/Services/TrainOpenRouteService.cs` - Throws NotImplementedException
- `src/api/Services/TripOrchestrationService.cs` - Throws NotImplementedException

All have proper constructor injection and dependencies set up.

### 4. Comprehensive Tests

**Created Files:**
- `tests/api.Tests.Unit/Services/CarOpenRouteServiceTests.cs` (12 test cases)
- `tests/api.Tests.Unit/Services/TrainOpenRouteServiceTests.cs` (8 test cases)
- `tests/api.Tests.Unit/Services/TripOrchestrationServiceTests.cs` (8 test cases)

**Total: 28 test cases**

## Test Coverage

### CarOpenRouteServiceTests
1. ✅ Returns car route results when API succeeds
2. ✅ Rounds up cost to next kilometer for fractional distances
3. ✅ Returns empty when no places provided
4. ✅ Throws exception when API returns error
5. ✅ Throws exception when API returns invalid JSON
6. ✅ Sends correct request to driving-car endpoint
7. *(Additional boundary tests included)*

### TrainOpenRouteServiceTests
1. ✅ Returns train route results using car costs
2. ✅ Awaits car costs when not yet available (parallel execution)
3. ✅ Returns empty when no places provided
4. ✅ Throws exception when API returns error
5. ✅ Throws exception when API returns invalid JSON
6. ✅ Sends correct request to cycling-regular endpoint
7. ✅ Propagates exception when car costs task fails
8. *(Additional edge cases included)*

### TripOrchestrationServiceTests
1. ✅ Uses only car service when all places are Car mode
2. ✅ Uses both services when all places are Train mode
3. ✅ Handles mixed transport modes correctly
4. ✅ Returns empty when no places exist
5. ✅ Executes services in parallel for train places
6. ✅ Propagates exception when car service fails
7. ✅ Propagates exception when train service fails
8. *(Includes timing verification for parallel execution)*

## Architecture Highlights

### Key Design Decisions

1. **Separate Interfaces**: `ICarRouteService` and `ITrainRouteService` keep responsibilities clear
2. **Task-Based Cost Sharing**: Train service accepts `Task<Dictionary<PlaceId, ushort>> carCosts` enabling parallel API calls
3. **Single API Call**: Each service calculates everything needed in one API call (car: time+distance+cost, train: time)
4. **Cost Calculation**: Car service owns cost calculation formula (`ceil(distance/1000) * 30`)
5. **Orchestration**: `TripOrchestrationService` groups by transport mode and delegates appropriately

### Parallel Execution Flow

```
GetAllTripsAsync(lat, lon)
    ├─ Get all places from repository
    ├─ Group by transport mode
    ├─ Start car service task (for all places)
    ├─ Start train service task (with car costs task) ← Parallel!
    ├─ Extract car costs from car results
    ├─ Await both tasks
    └─ Combine results into Trip[]
```

## Next Steps (After Review)

### If Tests are Approved:

1. **Implement CarOpenRouteService**:
   - Copy logic from existing `TripOpenRouteService`
   - Call OpenRouteService Matrix API with driving-car profile
   - Request both duration and distance metrics
   - Calculate cost using formula: `ceil(distance/1000) * 30`

2. **Implement TrainOpenRouteService**:
   - Call OpenRouteService Matrix API with cycling-regular profile
   - Request only duration metric (distance not needed)
   - Await car costs task
   - Combine train duration with car costs

3. **Implement TripOrchestrationService**:
   - Get all places from repository
   - Group by transport mode
   - Calculate car routes for all places (parallel start)
   - Calculate train routes for train places (parallel start with car costs task)
   - Extract car costs dictionary from car results
   - Build Trip objects from results

### Still Needed (Not in this PR):

- Update `PlaceEntity.cs` to add `TransportMode` field
- Update `Place.cs` to add `TransportMode` field
- Update `PlaceRequest.cs` models to add `TransportMode` field
- Update `PlaceCosmosService.cs` to handle `TransportMode`
- Update DI registration in `Program.cs`
- Frontend changes (Phase 2)

## How to Run Tests

```powershell
# Build solution
dotnet build

# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~CarOpenRouteServiceTests"
dotnet test --filter "FullyQualifiedName~TrainOpenRouteServiceTests"
dotnet test --filter "FullyQualifiedName~TripOrchestrationServiceTests"
```

## Review Checklist

### Test Quality
- [ ] Test names clearly describe what is being tested
- [ ] Tests follow Given/When/Then pattern (documented in XML comments)
- [ ] Tests cover success cases, error cases, and edge cases
- [ ] Tests use appropriate assertions
- [ ] Tests are independent and can run in any order

### Architecture
- [ ] Interfaces are clear and follow single responsibility
- [ ] Task-based cost sharing enables parallel execution
- [ ] Services have appropriate dependencies
- [ ] Error handling strategy is appropriate

### Implementation Strategy
- [ ] Stub implementations throw NotImplementedException
- [ ] Tests define the expected behavior clearly
- [ ] Ready for TDD implementation (tests first, then implementation)

## Notes

- **Cost Calculation Formula**: `ceil(distance_meters / 1000) * 30` cents
  - Example: 5000m = 5km → 150 cents
  - Example: 5500m = 5.5km → ceil(5.5) = 6km → 180 cents

- **OpenRouteService Profiles**:
  - Car: `driving-car` (accurate)
  - Train: `cycling-regular` (temporary proxy until real transit API)

- **Parallel Execution**: Train service can start API call before awaiting car costs, enabling true parallelization

---

**Ready for Review**: Please review tests and architecture. Once approved, I'll implement the actual service logic.
