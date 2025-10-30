using Bunit;
using Bunit.TestDoubles;
using FrontEnd.Pages;
using FrontEnd.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TUnit.Assertions.Extensions;

namespace FrontEnd.Tests.Unit.Pages;

public class TripCalculatorTests : Bunit.TestContext
{
	private readonly Mock<IUserService> _userServiceMock;
	private readonly Mock<ITripService> _tripServiceMock;
	private readonly Mock<ILocationService> _locationServiceMock;

	public TripCalculatorTests()
	{
		_userServiceMock = new Mock<IUserService>();
		_tripServiceMock = new Mock<ITripService>();
		_locationServiceMock = new Mock<ILocationService>();

		Services.AddSingleton(_userServiceMock.Object);
		Services.AddSingleton(_tripServiceMock.Object);
		Services.AddSingleton(_locationServiceMock.Object);
	}

	[Test]
	public async Task ShowsAccessDenied_WhenUserIsNotAuthorized()
	{
		// Arrange
		var authContext = this.AddTestAuthorization();
		authContext.SetAuthorized("TEST USER", AuthorizationState.Unauthorized);

		// Act
		var cut = RenderComponent<TripCalculator>();

		// Assert
		await Assert.That(cut.Markup).Contains("Access Denied");
		await Assert.That(cut.Markup).Contains("projectcoordination");
	}

	[Test]
	public async Task ShowsAccessDenied_WhenUserIsNotAuthenticated()
	{
		// Arrange
		this.AddTestAuthorization(); // Default is unauthenticated

		// Act
		var cut = RenderComponent<TripCalculator>();

		// Assert
		await Assert.That(cut.Markup).Contains("Access Denied");
	}

	[Test]
	public async Task RendersPageTitle_WhenUserIsAuthorized()
	{
		// Arrange
		var authContext = this.AddTestAuthorization();
		authContext.SetAuthorized("TEST USER");
		authContext.SetPolicies("Role:projectcoordination");

		// Act
		var cut = RenderComponent<TripCalculator>();

		// Assert
		var pageTitle = cut.Find("h3");
		await Assert.That(pageTitle.TextContent).Contains("Trip Calculator");
	}

	[Test]
	public async Task RendersLocationSelector_WhenUserIsAuthorized()
	{
		// Arrange
		var authContext = this.AddTestAuthorization();
		authContext.SetAuthorized("TEST USER");
		authContext.SetPolicies("Role:projectcoordination");

		// Act
		var cut = RenderComponent<TripCalculator>();

		// Assert - LocationSelector has a search input
		var searchInput = cut.Find("input[placeholder='Enter address or place name']");
		await Assert.That(searchInput).IsNotNull();
	}

	[Test]
	public async Task InitialStatus_IsWaitingForCoordinates_WhenUserIsAuthorized()
	{
		// Arrange
		var authContext = this.AddTestAuthorization();
		authContext.SetAuthorized("TEST USER");
		authContext.SetPolicies("Role:projectcoordination");

		// Act
		var cut = RenderComponent<TripCalculator>();

		// Assert
		await Assert.That(cut.Markup).Contains("Waiting for coordinates");
	}

	[Test]
	public async Task ShowsPageDescription_WhenUserIsAuthorized()
	{
		// Arrange
		var authContext = this.AddTestAuthorization();
		authContext.SetAuthorized("TEST USER");
		authContext.SetPolicies("Role:projectcoordination");

		// Act
		var cut = RenderComponent<TripCalculator>();

		// Assert
		await Assert.That(cut.Markup).Contains("Reisedauer");
	}
}
