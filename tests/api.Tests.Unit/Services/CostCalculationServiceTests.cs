using api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using TUnit.Assertions.Extensions;
using ZgM.ProjectCoordinator.Shared;

namespace api.Tests.Unit.Services;

public class CostCalculationServiceTests
{
    public class CalculateCostAsync
    {
        private readonly Mock<IUserSettingsService> _userSettingsServiceMock;
        private readonly Mock<ILogger<CostCalculationService>> _loggerMock;
        private readonly CostCalculationService _service;

        public CalculateCostAsync()
        {
            _userSettingsServiceMock = new Mock<IUserSettingsService>();
            _loggerMock = new Mock<ILogger<CostCalculationService>>();
            _service = new CostCalculationService(_userSettingsServiceMock.Object, _loggerMock.Object);

            _userSettingsServiceMock.Setup(s => s.GetDefaultSettingsAsync()).ReturnsAsync(new UserSettings
            {
                CentsPerKilometer = 25
            });
        }

        /// <summary>
        /// Given: User has custom cents per kilometer setting
        /// When: Calculating cost
        /// Then: Uses custom rate instead of default
        /// </summary>
        [Test]
        public async Task UsesCustomPerKmRate_WhenUserHasCustomSetting()
        {
            // Arrange
            var userId = UserId.Parse(Guid.NewGuid().ToString());
            var settings = new Models.UserSettings
            {
                UserId = userId,
                CentsPerKilometer = 30
            };
            _userSettingsServiceMock.Setup(s => s.GetUserSettingsAsync(userId)).ReturnsAsync(settings);

            // Act
            var cost = await _service.CalculateCostAsync(userId, 10000, 600); // 10 km

            // Assert
            await Assert.That(cost).IsEqualTo((uint)300); // ceil(10 km) * 30 = 300 cents
        }

        /// <summary>
        /// Given: User has cents per hour setting
        /// When: Calculating cost
        /// Then: Uses time-based calculation instead of distance-based
        /// </summary>
        [Test]
        public async Task UsesTimeBased_WhenUserHasCentsPerHourSetting()
        {
            // Arrange
            var userId = UserId.Parse(Guid.NewGuid().ToString());
            var settings = new Models.UserSettings
            {
                UserId = userId,
                CentsPerHour = 500 // 5 EUR per hour
            };
            _userSettingsServiceMock.Setup(s => s.GetUserSettingsAsync(userId)).ReturnsAsync(settings);

            // Act
            var cost = await _service.CalculateCostAsync(userId, 10000, 3600); // 10 km, 1 hour

            // Assert
            await Assert.That(cost).IsEqualTo((uint)500); // ceil(1 hour) * 500 = 500 cents
        }

        /// <summary>
        /// Given: User has both cents per hour and cents per kilometer
        /// When: Calculating cost
        /// Then: Prefers time-based calculation (cents per hour)
        /// </summary>
        [Test]
        public async Task PrefersTimeBased_WhenBothSettingsArePresent()
        {
            // Arrange
            var userId = UserId.Parse(Guid.NewGuid().ToString());
            var settings = new Models.UserSettings
            {
                UserId = userId,
                CentsPerKilometer = 30,
                CentsPerHour = 500
            };
            _userSettingsServiceMock.Setup(s => s.GetUserSettingsAsync(userId)).ReturnsAsync(settings);

            // Act
            var cost = await _service.CalculateCostAsync(userId, 10000, 1800); // 10 km, 30 minutes

            // Assert
            await Assert.That(cost).IsEqualTo((uint)250); // ceil(0.5 hours) * 500 = 250 cents (not 300 from distance)
        }

        /// <summary>
        /// Given: Distance is fractional kilometers
        /// When: Calculating cost with distance-based pricing
        /// Then: Rounds up the final cost
        /// </summary>
        [Test]
        public async Task RoundsUpFinalCost_WhenDistanceIsFractional()
        {
            // Arrange
            var userId = UserId.Parse(Guid.NewGuid().ToString());

            // Act
            var cost = await _service.CalculateCostAsync(userId, 5500, 600); // 5.5 km

            // Assert
            await Assert.That(cost).IsEqualTo((uint)138); // ceil(5.5 km * 25) = ceil(137.5) = 138 cents
        }

        /// <summary>
        /// Given: Duration is fractional hours
        /// When: Calculating cost with time-based pricing
        /// Then: Rounds up the final cost
        /// </summary>
        [Test]
        public async Task RoundsUpFinalCost_WhenDurationIsFractional()
        {
            // Arrange
            var userId = UserId.Parse(Guid.NewGuid().ToString());
            var settings = new Models.UserSettings
            {
                UserId = userId,
                CentsPerHour = 600
            };
            _userSettingsServiceMock.Setup(s => s.GetUserSettingsAsync(userId)).ReturnsAsync(settings);

            // Act
            var cost = await _service.CalculateCostAsync(userId, 10000, 5400); // 1.5 hours

            // Assert
            await Assert.That(cost).IsEqualTo((uint)900); // ceil(1.5 hours * 600) = ceil(900) = 900 cents
        }

        /// <summary>
        /// Given: Zero distance
        /// When: Calculating cost
        /// Then: Returns zero cost
        /// </summary>
        [Test]
        public async Task ReturnsZero_WhenDistanceIsZero()
        {
            // Arrange
            var userId = UserId.Parse(Guid.NewGuid().ToString());

            // Act
            var cost = await _service.CalculateCostAsync(userId, 0, 0);

            // Assert
            await Assert.That(cost).IsEqualTo((uint)0);
        }

        /// <summary>
        /// Given: Default settings are configured in Cosmos
        /// When: User has no custom settings
        /// Then: Uses configured default rate
        /// </summary>
        [Test]
        public async Task UsesConfiguredDefaultRate_WhenUserHasNoCustomSettings()
        {
            // Arrange
            var userId = UserId.Parse(Guid.NewGuid().ToString());
            _userSettingsServiceMock.Setup(s => s.GetDefaultSettingsAsync()).ReturnsAsync(new UserSettings
            {
                CentsPerKilometer = 30
            });

            // Act
            var cost = await _service.CalculateCostAsync(userId, 10000, 600); // 10 km

            // Assert
            await Assert.That(cost).IsEqualTo((uint)300); // ceil(10 km) * 30 = 300 cents
        }
    }
}





