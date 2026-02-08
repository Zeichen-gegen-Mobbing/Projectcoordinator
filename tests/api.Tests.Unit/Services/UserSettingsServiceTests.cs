using api.Exceptions;
using api.Repositories;
using api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using TUnit.Assertions.Extensions;
using ZgM.ProjectCoordinator.Shared;

namespace api.Tests.Unit.Services;

public class UserSettingsServiceTests
{
    private readonly Mock<IUserSettingRepository> _repositoryMock;
    private readonly Mock<ILogger<UserSettingsService>> _loggerMock;
    private readonly UserSettingsService _service;
    private static readonly UserId DefaultUserId = UserId.Parse("00000000-0000-0000-0000-000000000000");

    public UserSettingsServiceTests()
    {
        _repositoryMock = new Mock<IUserSettingRepository>();
        _loggerMock = new Mock<ILogger<UserSettingsService>>();
        _service = new UserSettingsService(_repositoryMock.Object, _loggerMock.Object);
    }

    public class GetUserSettingsAsync : UserSettingsServiceTests
    {
        /// <summary>
        /// Given: UserId is the default settings user ID
        /// When: Getting user settings
        /// Then: Throws ProblemDetailsException
        /// </summary>
        [Test]
        public async Task ThrowsException_WhenUserIdIsDefaultUserId()
        {
            // Act & Assert
            var exception = await Assert.That(async () => await _service.GetUserSettingsAsync(DefaultUserId))
                .Throws<ProblemDetailsException>();
        }

        /// <summary>
        /// Given: User has settings in repository
        /// When: Getting user settings
        /// Then: Returns the user settings
        /// </summary>
        [Test]
        public async Task ReturnsSettings_WhenUserHasSettings()
        {
            // Arrange
            var userId = UserId.Parse(Guid.NewGuid().ToString());
            var settings = new Models.UserSettings
            {
                UserId = userId,
                CentsPerKilometer = 30
            };
            _repositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(settings);

            // Act
            var result = await _service.GetUserSettingsAsync(userId);

            // Assert
            await Assert.That(result).IsNotNull();
            await Assert.That(result!.UserId).IsEqualTo(userId);
            await Assert.That(result.CentsPerKilometer).IsEqualTo((uint)30);
        }

        /// <summary>
        /// Given: User has no settings in repository
        /// When: Getting user settings
        /// Then: Returns null
        /// </summary>
        [Test]
        public async Task ReturnsNull_WhenUserHasNoSettings()
        {
            // Arrange
            var userId = UserId.Parse(Guid.NewGuid().ToString());
            _repositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((Models.UserSettings?)null);

            // Act
            var result = await _service.GetUserSettingsAsync(userId);

            // Assert
            await Assert.That(result).IsNull();
        }
    }

    public class UpsertUserSettingsAsync : UserSettingsServiceTests
    {
        /// <summary>
        /// Given: UserId is the default settings user ID
        /// When: Upserting user settings
        /// Then: Throws ProblemDetailsException
        /// </summary>
        [Test]
        public async Task ThrowsException_WhenUserIdIsDefaultUserId()
        {
            // Arrange
            var settings = new Models.UserSettings
            {
                UserId = DefaultUserId,
                CentsPerKilometer = 30
            };

            // Act & Assert
            var exception = await Assert.That(async () => await _service.UpsertUserSettingsAsync(settings))
                .Throws<ProblemDetailsException>();
        }

        /// <summary>
        /// Given: Valid user ID and settings
        /// When: Upserting user settings
        /// Then: Calls repository and returns updated settings
        /// </summary>
        [Test]
        public async Task UpsertsSettings_WhenValidUserId()
        {
            // Arrange
            var userId = UserId.Parse(Guid.NewGuid().ToString());
            var settings = new Models.UserSettings
            {
                UserId = userId,
                CentsPerKilometer = 30,
                CentsPerHour = 500
            };

            // Act
            var result = await _service.UpsertUserSettingsAsync(settings);

            // Assert
            _repositoryMock.Verify(r => r.UpsertAsync(It.Is<Models.UserSettings>(s =>
                s.UserId.Equals(userId) &&
                s.CentsPerKilometer == settings.CentsPerKilometer &&
                s.CentsPerHour == settings.CentsPerHour)), Times.Once);

            await Assert.That(result.UserId).IsEqualTo(userId);
            await Assert.That(result.CentsPerKilometer).IsEqualTo((uint)30);
            await Assert.That(result.CentsPerHour).IsEqualTo((uint)500);
        }
    }

    public class DeleteUserSettingsAsync : UserSettingsServiceTests
    {
        /// <summary>
        /// Given: UserId is the default settings user ID
        /// When: Deleting user settings
        /// Then: Throws ProblemDetailsException
        /// </summary>
        [Test]
        public async Task ThrowsException_WhenUserIdIsDefaultUserId()
        {
            // Act & Assert
            var exception = await Assert.That(async () => await _service.DeleteUserSettingsAsync(DefaultUserId))
                .Throws<ProblemDetailsException>();
        }

        /// <summary>
        /// Given: Valid user ID
        /// When: Deleting user settings
        /// Then: Calls repository delete
        /// </summary>
        [Test]
        public async Task DeletesSettings_WhenValidUserId()
        {
            // Arrange
            var userId = UserId.Parse(Guid.NewGuid().ToString());

            // Act
            await _service.DeleteUserSettingsAsync(userId);

            // Assert
            _repositoryMock.Verify(r => r.DeleteAsync(userId), Times.Once);
        }
    }

    public class GetDefaultSettingsAsync : UserSettingsServiceTests
    {
        /// <summary>
        /// Given: Default settings exist in repository
        /// When: Getting default settings
        /// Then: Returns the default settings from repository
        /// </summary>
        [Test]
        public async Task ReturnsDefaultSettings_WhenSettingsExist()
        {
            // Arrange
            var settings = new Models.UserSettings
            {
                UserId = DefaultUserId,
                CentsPerKilometer = 30
            };
            _repositoryMock.Setup(r => r.GetByUserIdAsync(DefaultUserId)).ReturnsAsync(settings);

            // Act
            var result = await _service.GetDefaultSettingsAsync();

            // Assert
            await Assert.That(result.CentsPerKilometer).IsEqualTo((uint)30);
        }

        /// <summary>
        /// Given: No default settings in repository
        /// When: Getting default settings
        /// Then: Returns new settings with 25 cents per kilometer
        /// </summary>
        [Test]
        public async Task Returns25Cents_WhenNoSettingsExist()
        {
            // Arrange
            _repositoryMock.Setup(r => r.GetByUserIdAsync(DefaultUserId)).ReturnsAsync((Models.UserSettings?)null);

            // Act
            var result = await _service.GetDefaultSettingsAsync();

            // Assert
            await Assert.That(result.CentsPerKilometer).IsEqualTo((uint)25);
            await Assert.That(result.CentsPerHour).IsNull();
        }

        /// <summary>
        /// Given: No default settings in repository
        /// When: Getting default settings multiple times
        /// Then: Always returns consistent fallback values
        /// </summary>
        [Test]
        public async Task ReturnsConsistentFallback_WhenNoSettingsExist()
        {
            // Arrange
            _repositoryMock.Setup(r => r.GetByUserIdAsync(DefaultUserId)).ReturnsAsync((Models.UserSettings?)null);

            // Act
            var result1 = await _service.GetDefaultSettingsAsync();
            var result2 = await _service.GetDefaultSettingsAsync();

            // Assert
            await Assert.That(result1.CentsPerKilometer).IsEqualTo(result2.CentsPerKilometer);
        }
    }

    public class UpsertDefaultSettingsAsync : UserSettingsServiceTests
    {
        /// <summary>
        /// Given: Valid default settings
        /// When: Upserting default settings
        /// Then: Sets UserId to default and saves to repository
        /// </summary>
        [Test]
        public async Task UpsertsDefaultSettings_WithDefaultUserId()
        {
            // Arrange
            var settings = new UserSettings
            {
                CentsPerKilometer = 35,
            };

            // Act
            var result = await _service.UpsertDefaultSettingsAsync(settings);

            // Assert
            _repositoryMock.Verify(r => r.UpsertAsync(It.Is<Models.UserSettings>(s =>
                s.UserId.Equals(DefaultUserId) &&
                s.CentsPerKilometer == settings.CentsPerKilometer)), Times.Once);

            await Assert.That(result.CentsPerKilometer).IsEqualTo((uint)35);
        }

        /// <summary>
        /// Given: Default settings with CentsPerHour
        /// When: Upserting default settings
        /// Then: Throws
        /// </summary>
        [Test]
        public async Task UpsertsPartialSettings_WhenOnlyCentsPerKilometer()
        {
            // Arrange
            var settings = new UserSettings
            {
                CentsPerHour = 10
            };

            // Act && Assert
            var exception = await Assert.That(async () => await _service.UpsertDefaultSettingsAsync(settings))
                .Throws<ProblemDetailsException>();
        }
    }
}
