using Bunit;
using Bunit.TestDoubles;
using FrontEnd.Layout;
using TUnit.Assertions.Extensions;

namespace FrontEnd.Tests.Unit.Layout;

public class NavMenuTests : Bunit.TestContext
{
	[Test]
	public async Task ShowsTripCalculatorLink_WhenUserHasProjectCoordinationRole()
	{
		// Arrange
		var authContext = this.AddTestAuthorization();
		authContext.SetAuthorized("TEST USER");
		authContext.SetPolicies("Role:projectcoordination");

		// Act
		var cut = RenderComponent<NavMenu>();

		// Assert
		await Assert.That(cut.Markup).Contains("Trip Calculator");
		var tripLink = cut.Find("a[href='trips']");
		await Assert.That(tripLink).IsNotNull();
	}

	[Test]
	[Arguments(AuthorizationState.Unauthorized)]
	[Arguments(AuthorizationState.Authorized)]
	public async Task HidesTripCalculatorLink_WhenUserDoesNotHaveProjectCoordinationRole(AuthorizationState authState)
	{
		// Arrange
		var authContext = this.AddTestAuthorization();
		authContext.SetAuthorized("TEST USER", authState);

		// Act
		var cut = RenderComponent<NavMenu>();

		// Assert
		await Assert.That(cut.Markup).DoesNotContain("Trip Calculator");
		var tripLinks = cut.FindAll("a[href='trips']");
		await Assert.That(tripLinks.Count).IsEqualTo(0);
	}

	[Test]
	public async Task HidesTripCalculatorLink_WhenUserIsNotAuthenticated()
	{
		// Arrange
		this.AddTestAuthorization(); // Default is unauthenticated

		// Act
		var cut = RenderComponent<NavMenu>();

		// Assert
		await Assert.That(cut.Markup).DoesNotContain("Trip Calculator");
	}

	[Test]
	public async Task TogglesNavMenu_WhenButtonClicked()
	{
		// Arrange
		this.AddTestAuthorization();
		var cut = RenderComponent<NavMenu>();
		var toggleButton = cut.Find(".navbar-toggler");
		var navMenu = cut.Find(".nav-scrollable");

		// Assert initial state - collapsed
		await Assert.That(navMenu.ClassList).Contains("collapse");

		// Act - First click
		toggleButton.Click();
		navMenu = cut.Find(".nav-scrollable");

		// Assert - expanded
		await Assert.That(navMenu.ClassList).DoesNotContain("collapse");

		// Act - Second click
		toggleButton.Click();
		navMenu = cut.Find(".nav-scrollable");

		// Assert - collapsed again
		await Assert.That(navMenu.ClassList).Contains("collapse");
	}

	[Test]
	public async Task ClickingNavMenu_TogglesMenu()
	{
		// Arrange
		this.AddTestAuthorization();
		var cut = RenderComponent<NavMenu>();
		var toggleButton = cut.Find(".navbar-toggler");

		// Expand first
		toggleButton.Click();

		// Act - Click on the nav menu itself
		var navMenu = cut.Find(".nav-scrollable");
		navMenu.Click();

		// Assert - Should collapse
		navMenu = cut.Find(".nav-scrollable");
		await Assert.That(navMenu.ClassList).Contains("collapse");
	}
}
