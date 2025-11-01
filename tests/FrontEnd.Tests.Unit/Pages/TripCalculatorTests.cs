using Bunit;
using Bunit.TestDoubles;
using FrontEnd.Pages;
using FrontEnd.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Globalization;
using TUnit.Assertions.Extensions;
using TUnit;

namespace FrontEnd.Tests.Unit.Pages;

public class TripCalculatorTests : Bunit.TestContext
{
	protected readonly Mock<IUserService> _userServiceMock;
	protected readonly Mock<ITripService> _tripServiceMock;
	protected readonly Mock<ILocationService> _locationServiceMock;

	public TripCalculatorTests()
	{
		_userServiceMock = new Mock<IUserService>();
		_tripServiceMock = new Mock<ITripService>();
		_locationServiceMock = new Mock<ILocationService>();

		Services.AddSingleton(_userServiceMock.Object);
		Services.AddSingleton(_tripServiceMock.Object);
		Services.AddSingleton(_locationServiceMock.Object);
	}
	public class AccessControl : TripCalculatorTests
	{

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

	public class CostDisplay : TripCalculatorTests
	{
		[Test]
		[Arguments("de-DE", "123,45")]
		[Arguments("en-US", "123.45")]
		public async Task ShowsCostWithEuroSymbol_WhenTripsLoaded(string cultureName, string expectedCost)
		{
			// Arrange: enforce German culture for predictable formatting
			Thread.CurrentThread.CurrentCulture = new CultureInfo(cultureName);

			var authContext = this.AddTestAuthorization();
			authContext.SetAuthorized("TEST USER");
			authContext.SetPolicies("Role:projectcoordination");

			var searchResult = new ZgM.ProjectCoordinator.Shared.LocationSearchResult
			{
				Label = "Test Location",
				Latitude = 52.5200,
				Longitude = 13.4050
			};

			_locationServiceMock.Setup(s => s.SearchLocationsAsync(It.IsAny<string>()))
				.ReturnsAsync(new[] { searchResult });

			var trip = new ZgM.ProjectCoordinator.Shared.Trip
			{
				Place = new ZgM.ProjectCoordinator.Shared.Place
				{
					Id = new ZgM.ProjectCoordinator.Shared.PlaceId("place1"),
					UserId = ZgM.ProjectCoordinator.Shared.UserId.Parse("00000000-0000-0000-0000-000000000001"),
					Name = "Test Place"
				},
				Time = TimeSpan.FromMinutes(10),
				Cost = 12345 // cents => 123.45
			};

			_tripServiceMock.Setup(s => s.GetTripsAsync(It.IsAny<double>(), It.IsAny<double>()))
				.ReturnsAsync(new[] { trip });

			// Configure JSInterop to handle QuickGrid module import (needed when trips render)
			JSInterop.Mode = JSRuntimeMode.Loose;

			// Act
			var cut = RenderComponent<TripCalculator>();

			// 1. Enter search string into the LocationSearch
			var searchInput = cut.Find("input[placeholder='Enter address or place name']");
			searchInput.Change("Berlin");

			// 2. Click the search button
			var searchButton = cut.Find("button[type='submit']");
			await cut.InvokeAsync(() => searchButton.Click());

			// 3. Select a found location - this will trigger trips loading
			var selectButton = cut.Find("button.btn-success");
			await cut.InvokeAsync(() => selectButton.Click());

			// Assert - wait for cost value and column header with euro symbol to appear
			cut.WaitForAssertion(async () =>
			{
				await Assert.That(cut.Markup).Contains(expectedCost);
				await Assert.That(cut.Markup).Contains("Costs (€)");
			});
		}
	}
}