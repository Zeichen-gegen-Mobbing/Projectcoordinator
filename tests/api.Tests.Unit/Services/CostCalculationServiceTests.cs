using api.Repositories;
using api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using TUnit.Assertions.Extensions;
using ZgM.ProjectCoordinator.Shared;
using UserSettings = api.Models.UserSettings;

namespace api.Tests.Unit.Services;

public class CostCalculationServiceTests
{
    public class CalculateCostAsync
    {
        private readonly Mock<IUserSettingRepository> _repositoryMock;
        private readonly Mock<ILogger<CostCalculationService>> _loggerMock;
        private readonly CostCalculationService _service;

        public CalculateCostAsync()
        {
            _repositoryMock = new Mock<IUserSettingRepository>();
            _loggerMock = new Mock<ILogger<CostCalculationService>>();
            _service = new CostCalculationService(_repositoryMock.Object, _loggerMock.Object);
        }

        /// <summary>
        /// Given: User has no custom settings
        /// When: Calculating cost
        /// Then: Uses default rate of 25 cents per kilometer
        /// </summary>
        [Test]
        public async Task UsesDefaultRate_WhenUserHasNoSettings()
        {
            // Arrange
            var userId = UserId.Parse(Guid.NewGuid().ToString());
            _repositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((UserSettings?)null);

            // Act
            var cost = await _service.CalculateCostAsync(userId, 10000, 600); // 10 km, 10 minutes

            // Assert
            await Assert.That(cost).IsEqualTo((uint)250); // ceil(10 km) * 25 = 250 cents
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
            var settings = new UserSettings
            {
                UserId = userId,
                CentsPerKilometer = 30
            };
            _repositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(settings);

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
            var settings = new UserSettings
            {
                UserId = userId,
                CentsPerHour = 500 // 5 EUR per hour
            };
            _repositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(settings);

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
            var settings = new UserSettings
            {
                UserId = userId,
                CentsPerKilometer = 30,
                CentsPerHour = 500
            };
            _repositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(settings);

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
            _repositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((UserSettings?)null);

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
            var settings = new UserSettings
            {
                UserId = userId,
                CentsPerHour = 600
            };
            _repositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(settings);

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
            _repositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((UserSettings?)null);

            // Act
            var cost = await _service.CalculateCostAsync(userId, 0, 0);

            // Assert
            await Assert.That(cost).IsEqualTo((uint)0);
        }

        /// <summary>
        /// Given: User settings are cached
        /// When: Calculating cost multiple times for same user
        /// Then: Repository is only called once
        /// </summary>
        [Test]
        public async Task CachesUserSettings_WhenCalledMultipleTimes()
        {
            // Arrange
            var userId = UserId.Parse(Guid.NewGuid().ToString());
            var settings = new UserSettings
            {
                UserId = userId,
                CentsPerKilometer = 30
            };
            _repositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(settings);

            // Act
            await _service.CalculateCostAsync(userId, 10000, 600);
            await _service.CalculateCostAsync(userId, 5000, 300);
            await _service.CalculateCostAsync(userId, 15000, 900);

            // Assert
            _repositoryMock.Verify(r => r.GetByUserIdAsync(userId), Times.Once);
        }

        /// <summary>
        /// Given: Multiple users
        /// When: Calculating cost for different users
        /// Then: Each user's settings are cached independently
        /// </summary>
        [Test]
        public async Task CachesSettingsPerUser_WhenCalculatingForMultipleUsers()
        {
            // Arrange
            var userId1 = UserId.Parse(Guid.NewGuid().ToString());
            var userId2 = UserId.Parse(Guid.NewGuid().ToString());

            var settings1 = new UserSettings { UserId = userId1, CentsPerKilometer = 30 };
            var settings2 = new UserSettings { UserId = userId2, CentsPerKilometer = 40 };

            _repositoryMock.Setup(r => r.GetByUserIdAsync(userId1)).ReturnsAsync(settings1);
            _repositoryMock.Setup(r => r.GetByUserIdAsync(userId2)).ReturnsAsync(settings2);

            // Act
            var cost1a = await _service.CalculateCostAsync(userId1, 10000, 600);
            var cost2a = await _service.CalculateCostAsync(userId2, 10000, 600);
            var cost1b = await _service.CalculateCostAsync(userId1, 10000, 600);
            var cost2b = await _service.CalculateCostAsync(userId2, 10000, 600);

            // Assert
            await Assert.That(cost1a).IsEqualTo((uint)300); // 10 km * 30
            await Assert.That(cost1b).IsEqualTo((uint)300);
            await Assert.That(cost2a).IsEqualTo((uint)400); // 10 km * 40
            await Assert.That(cost2b).IsEqualTo((uint)400);

            _repositoryMock.Verify(r => r.GetByUserIdAsync(userId1), Times.Once);
            _repositoryMock.Verify(r => r.GetByUserIdAsync(userId2), Times.Once);
        }
    }
}





