using Bunit;
using FrontEnd.Components;
using FrontEnd.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TUnit.Assertions.Extensions;
using ZgM.ProjectCoordinator.Shared;

namespace FrontEnd.Tests.Unit.Components;

public class LocationSelectorTests : Bunit.TestContext
{
	private readonly Mock<ILocationService> _locationServiceMock = new();

	public LocationSelectorTests()
	{
		Services.AddSingleton(_locationServiceMock.Object);
	}

	public class Rendering : LocationSelectorTests
	{
		[Test]
		public async Task RendersSearchBox()
		{
			// Arrange
			var locationSelectedCalled = false;
			Task OnLocationSelected(LocationSearchResult result)
			{
				locationSelectedCalled = true;
				return Task.CompletedTask;
			}

			// Act
			var cut = RenderComponent<LocationSelector>(parameters => parameters
				.Add(p => p.OnLocationSelected, OnLocationSelected)
				.Add(p => p.DisableSearch, false).Add(p => p.DisableSelect, false));

			// Assert
			var searchInput = cut.Find("input[placeholder='Enter address or place name']");
			await Assert.That(searchInput).IsNotNull();
			await Assert.That(locationSelectedCalled).IsFalse();
		}

		[Test]
		public async Task DisablesSearchButton_WhenDisabledIsTrue()
		{
			// Arrange
			static Task OnLocationSelected(LocationSearchResult result) => Task.CompletedTask;

			// Act
			var cut = RenderComponent<LocationSelector>(parameters => parameters
				.Add(p => p.OnLocationSelected, OnLocationSelected)
				.Add(p => p.DisableSearch, true).Add(p => p.DisableSelect, true));

			// Assert
			var searchButton = cut.Find("button[type='submit']");
			await Assert.That(searchButton.HasAttribute("disabled")).IsTrue();
		}
	}
	public class SearchMethod : LocationSelectorTests
	{
		[Test]
		public async Task CallsOnSearchStarted_WhenSearchIsInitiated()
		{
			// Arrange
			var onSearchStartedCalled = false;
			_locationServiceMock
				.Setup(x => x.SearchLocationsAsync(It.IsAny<string>()))
				.ReturnsAsync(new List<LocationSearchResult>());

			Task OnLocationSelected(LocationSearchResult result) => Task.CompletedTask;
			void OnSearchStarted() => onSearchStartedCalled = true;

			var cut = RenderComponent<LocationSelector>(parameters => parameters
				.Add(p => p.OnLocationSelected, OnLocationSelected)
				.Add(p => p.DisableSearch, false).Add(p => p.DisableSelect, false)
				.Add(p => p.OnSearchStarted, OnSearchStarted));

			// Act
			var searchInput = cut.Find("input[placeholder='Enter address or place name']");
			searchInput.Change("Berlin");
			var searchButton = cut.Find("button[type='submit']");
			await cut.InvokeAsync(() => searchButton.Click());

			// Assert
			await Assert.That(onSearchStartedCalled).IsTrue();
		}

		[Test]
		public async Task DoesNotThrow_WhenOnSearchStartedIsNotProvided()
		{
			// Arrange
			_locationServiceMock
				.Setup(x => x.SearchLocationsAsync(It.IsAny<string>()))
				.ReturnsAsync(new List<LocationSearchResult>());

			Task OnLocationSelected(LocationSearchResult result) => Task.CompletedTask;

			var cut = RenderComponent<LocationSelector>(parameters => parameters
				.Add(p => p.OnLocationSelected, OnLocationSelected)
				.Add(p => p.DisableSearch, false).Add(p => p.DisableSelect, false));
			// OnSearchStarted not provided

			// Act
			var searchInput = cut.Find("input[placeholder='Enter address or place name']");
			searchInput.Change("Berlin");
			var searchButton = cut.Find("button[type='submit']");

			// Assert - should not throw
			await cut.InvokeAsync(() => searchButton.Click());
			// Test passes if no exception is thrown
		}

		[Test]
		public async Task DisplaysSearchResults_WhenSearchReturnsResults()
		{
			// Arrange
			var results = new List<LocationSearchResult>
			{
				new() { Label = "Berlin, Germany", Latitude = 52.52, Longitude = 13.405, Street = "Main St", HouseNumber = "1" },
				new() { Label = "Munich, Germany", Latitude = 48.1351, Longitude = 11.582, PostalCode = "80331", Locality = "Munich" }
			};

			_locationServiceMock
				.Setup(x => x.SearchLocationsAsync(It.IsAny<string>()))
				.ReturnsAsync(results);

			Task OnLocationSelected(LocationSearchResult result) => Task.CompletedTask;

			var cut = RenderComponent<LocationSelector>(parameters => parameters
				.Add(p => p.OnLocationSelected, OnLocationSelected)
				.Add(p => p.DisableSearch, false).Add(p => p.DisableSelect, false));

			// Act
			var searchInput = cut.Find("input[placeholder='Enter address or place name']");
			searchInput.Change("Berlin");
			var searchButton = cut.Find("button[type='submit']");
			await cut.InvokeAsync(() => searchButton.Click());

			// Assert
			var table = cut.Find("table");
			await Assert.That(table).IsNotNull();

			var rows = cut.FindAll("tbody tr");
			await Assert.That(rows.Count).IsEqualTo(2);

			await Assert.That(cut.Markup).Contains("Berlin, Germany");
			await Assert.That(cut.Markup).Contains("Munich, Germany");
		}

		[Test]
		public async Task DisplaysNoResults_WhenSearchReturnsEmpty()
		{
			// Arrange
			_locationServiceMock
				.Setup(x => x.SearchLocationsAsync(It.IsAny<string>()))
				.ReturnsAsync(new List<LocationSearchResult>());

			Task OnLocationSelected(LocationSearchResult result) => Task.CompletedTask;

			var cut = RenderComponent<LocationSelector>(parameters => parameters
				.Add(p => p.OnLocationSelected, OnLocationSelected)
				.Add(p => p.DisableSearch, false).Add(p => p.DisableSelect, false));

			// Act
			var searchInput = cut.Find("input[placeholder='Enter address or place name']");
			searchInput.Change("NonExistentPlace");
			var searchButton = cut.Find("button[type='submit']");
			await cut.InvokeAsync(() => searchButton.Click());

			// Assert
			await Assert.That(cut.Markup).Contains("No results found");
		}

		[Test]
		public async Task DisplaysErrorMessage_WhenSearchThrowsException()
		{
			// Arrange
			_locationServiceMock
				.Setup(x => x.SearchLocationsAsync(It.IsAny<string>()))
				.ThrowsAsync(new Exception("API Error"));

			Task OnLocationSelected(LocationSearchResult result) => Task.CompletedTask;

			var cut = RenderComponent<LocationSelector>(parameters => parameters
				.Add(p => p.OnLocationSelected, OnLocationSelected)
				.Add(p => p.DisableSearch, false).Add(p => p.DisableSelect, false));

			// Act
			var searchInput = cut.Find("input[placeholder='Enter address or place name']");
			searchInput.Change("Berlin");
			var searchButton = cut.Find("button[type='submit']");
			await cut.InvokeAsync(() => searchButton.Click());

			// Assert
			await Assert.That(cut.Markup).Contains("Error searching for locations");
			await Assert.That(cut.Markup).Contains("API Error");
			searchButton = cut.Find("button[type='submit']");
			await Assert.That(searchButton.HasAttribute("disabled")).IsFalse();
		}

		[Test]
		public async Task ClearsSelectedLocation_WhenSearchingAgain()
		{
			// Arrange
			var results = new List<LocationSearchResult>
			{
				new() { Label = "Berlin, Germany", Latitude = 52.52, Longitude = 13.405 }
			};

			_locationServiceMock
				.Setup(x => x.SearchLocationsAsync(It.IsAny<string>()))
				.ReturnsAsync(results);

			var locationSelectedCount = 0;
			Task OnLocationSelected(LocationSearchResult result)
			{
				locationSelectedCount++;
				return Task.CompletedTask;
			}

			var cut = RenderComponent<LocationSelector>(parameters => parameters
				.Add(p => p.OnLocationSelected, OnLocationSelected)
				.Add(p => p.DisableSearch, false).Add(p => p.DisableSelect, false));

			// Act - First search and select
			var searchInput = cut.Find("input[placeholder='Enter address or place name']");
			searchInput.Change("Berlin");
			var searchButton = cut.Find("button[type='submit']");
			await cut.InvokeAsync(() => searchButton.Click());

			var selectButton = cut.Find("button.btn-success");
			await cut.InvokeAsync(() => selectButton.Click());

			// Act - Search again
			searchInput = cut.Find("input[placeholder='Enter address or place name']");
			searchInput.Change("Munich");
			searchButton = cut.Find("button[type='submit']");
			await cut.InvokeAsync(() => searchButton.Click());

			// Assert
			await Assert.That(locationSelectedCount).IsEqualTo(1);
			var selectedLocationDiv = cut.FindAll(".selected-location-label");
			await Assert.That(selectedLocationDiv.Count).IsEqualTo(0);
		}
	}

	public class SelectLocationMethod : LocationSelectorTests
	{
		[Test]
		public async Task CallsOnLocationSelected_WhenLocationIsSelected()
		{
			// Arrange
			var results = new List<LocationSearchResult>
			{
				new() { Label = "Berlin, Germany", Latitude = 52.52, Longitude = 13.405 }
			};

			_locationServiceMock
				.Setup(x => x.SearchLocationsAsync(It.IsAny<string>()))
				.ReturnsAsync(results);

			LocationSearchResult? capturedResult = null;
			Task OnLocationSelected(LocationSearchResult result)
			{
				capturedResult = result;
				return Task.CompletedTask;
			}

			var cut = RenderComponent<LocationSelector>(parameters => parameters
				.Add(p => p.OnLocationSelected, OnLocationSelected)
				.Add(p => p.DisableSearch, false).Add(p => p.DisableSelect, false));

			// Search first
			var searchInput = cut.Find("input[placeholder='Enter address or place name']");
			searchInput.Change("Berlin");
			var searchButton = cut.Find("button[type='submit']");
			await cut.InvokeAsync(() => searchButton.Click());

			// Act
			var selectButton = cut.Find("button.btn-success");
			await cut.InvokeAsync(() => selectButton.Click());

			// Assert
			await Assert.That(capturedResult).IsNotNull();
			await Assert.That(capturedResult!.Value.Latitude).IsEqualTo(52.52);
			await Assert.That(capturedResult.Value.Longitude).IsEqualTo(13.405);
		}

		[Test]
		public async Task HidesSearchResults_WhenLocationIsSelected()
		{
			// Arrange
			var results = new List<LocationSearchResult>
			{
				new() { Label = "Berlin, Germany", Latitude = 52.52, Longitude = 13.405 }
			};

			_locationServiceMock
				.Setup(x => x.SearchLocationsAsync(It.IsAny<string>()))
				.ReturnsAsync(results);

			Task OnLocationSelected(LocationSearchResult result) => Task.CompletedTask;

			var cut = RenderComponent<LocationSelector>(parameters => parameters
				.Add(p => p.OnLocationSelected, OnLocationSelected)
				.Add(p => p.DisableSearch, false).Add(p => p.DisableSelect, false));

			var searchInput = cut.Find("input[placeholder='Enter address or place name']");
			searchInput.Change("Berlin");
			var searchButton = cut.Find("button[type='submit']");
			await cut.InvokeAsync(() => searchButton.Click());

			// Act
			var selectButton = cut.Find("button.btn-success");
			await cut.InvokeAsync(() => selectButton.Click());

			// Assert
			var tables = cut.FindAll("table");
			await Assert.That(tables.Count).IsEqualTo(0);
		}

		[Test]
		public async Task ShowsSelectedLocationLabel_WhenLocationIsSelected()
		{
			// Arrange
			var results = new List<LocationSearchResult>
			{
				new() { Label = "Berlin, Germany", Latitude = 52.52, Longitude = 13.405 }
			};

			_locationServiceMock
				.Setup(x => x.SearchLocationsAsync(It.IsAny<string>()))
				.ReturnsAsync(results);

			Task OnLocationSelected(LocationSearchResult result) => Task.CompletedTask;

			var cut = RenderComponent<LocationSelector>(parameters => parameters
				.Add(p => p.OnLocationSelected, OnLocationSelected)
				.Add(p => p.DisableSearch, false).Add(p => p.DisableSelect, false));

			var searchInput = cut.Find("input[placeholder='Enter address or place name']");
			searchInput.Change("Berlin");
			var searchButton = cut.Find("button[type='submit']");
			await cut.InvokeAsync(() => searchButton.Click());

			// Act
			var selectButton = cut.Find("button.btn-success");
			await cut.InvokeAsync(() => selectButton.Click());

			// Assert
			var selectedLocationDiv = cut.Find(".selected-location-label");
			await Assert.That(selectedLocationDiv.TextContent).Contains("Berlin, Germany");
			await Assert.That(cut.Markup).DoesNotContain("No results found. Try a different search term.");
		}
	}

	public class ShowMapMethod : LocationSelectorTests
	{
		[Test]
		public async Task OpensMapModal_WhenMapButtonIsClicked()
		{
			// Arrange
			var results = new List<LocationSearchResult>
			{
				new() { Label = "Berlin, Germany", Latitude = 52.52, Longitude = 13.405 }
			};

			_locationServiceMock
				.Setup(x => x.SearchLocationsAsync(It.IsAny<string>()))
				.ReturnsAsync(results);

			Task OnLocationSelected(LocationSearchResult result) => Task.CompletedTask;

			var cut = RenderComponent<LocationSelector>(parameters => parameters
				.Add(p => p.OnLocationSelected, OnLocationSelected)
				.Add(p => p.DisableSearch, false).Add(p => p.DisableSelect, false));

			var searchInput = cut.Find("input[placeholder='Enter address or place name']");
			searchInput.Change("Berlin");
			var searchButton = cut.Find("button[type='submit']");
			await cut.InvokeAsync(() => searchButton.Click());

			// Act
			var mapButton = cut.Find("button.btn-secondary");
			await cut.InvokeAsync(() => mapButton.Click());

			// Assert
			var modal = cut.Find(".modal");
			await Assert.That(modal).IsNotNull();
			await Assert.That(cut.Markup).Contains("Berlin, Germany");

			var iframe = cut.Find("iframe");
			await Assert.That(iframe).IsNotNull();
		}

		[Test]
		public async Task ClosesMapModal_WhenCloseButtonIsClicked()
		{
			// Arrange
			var results = new List<LocationSearchResult>
			{
				new() { Label = "Berlin, Germany", Latitude = 52.52, Longitude = 13.405 }
			};

			_locationServiceMock
				.Setup(x => x.SearchLocationsAsync(It.IsAny<string>()))
				.ReturnsAsync(results);

			Task OnLocationSelected(LocationSearchResult result) => Task.CompletedTask;

			var cut = RenderComponent<LocationSelector>(parameters => parameters
				.Add(p => p.OnLocationSelected, OnLocationSelected)
				.Add(p => p.DisableSearch, false).Add(p => p.DisableSelect, false));

			var searchInput = cut.Find("input[placeholder='Enter address or place name']");
			searchInput.Change("Berlin");
			var searchButton = cut.Find("button[type='submit']");
			await cut.InvokeAsync(() => searchButton.Click());

			var mapButton = cut.Find("button.btn-secondary");
			await cut.InvokeAsync(() => mapButton.Click());

			// Act
			var closeButton = cut.Find(".btn-close");
			await cut.InvokeAsync(() => closeButton.Click());

			// Assert
			var modals = cut.FindAll(".modal");
			await Assert.That(modals.Count).IsEqualTo(0);
		}

		[Test]
		public async Task ShowsAddressInModal_WhenMapIsOpened()
		{
			// Arrange
			var results = new List<LocationSearchResult>
			{
				new()
				{
					Label = "Berlin, Germany",
					Latitude = 52.52,
					Longitude = 13.405,
					Street = "Unter den Linden",
					HouseNumber = "1",
					PostalCode = "10117",
					Locality = "Berlin",
					Country = "Germany"
				}
			};

			_locationServiceMock
				.Setup(x => x.SearchLocationsAsync(It.IsAny<string>()))
				.ReturnsAsync(results);

			Task OnLocationSelected(LocationSearchResult result) => Task.CompletedTask;

			var cut = RenderComponent<LocationSelector>(parameters => parameters
				.Add(p => p.OnLocationSelected, OnLocationSelected)
				.Add(p => p.DisableSearch, false).Add(p => p.DisableSelect, false));

			var searchInput = cut.Find("input[placeholder='Enter address or place name']");
			searchInput.Change("Berlin");
			var searchButton = cut.Find("button[type='submit']");
			await cut.InvokeAsync(() => searchButton.Click());

			// Act
			var mapButton = cut.Find("button.btn-secondary");
			await cut.InvokeAsync(() => mapButton.Click());

			// Assert
			await Assert.That(cut.Markup).Contains("Address:");
			await Assert.That(cut.Markup).Contains("Unter den Linden 1");
			await Assert.That(cut.Markup).Contains("10117 Berlin");
			await Assert.That(cut.Markup).Contains("Germany");
		}

		[Test]
		public async Task SelectsLocationFromModal_WhenSelectButtonIsClicked()
		{
			// Arrange
			var results = new List<LocationSearchResult>
			{
				new() { Label = "Berlin, Germany", Latitude = 52.52, Longitude = 13.405 }
			};

			_locationServiceMock
				.Setup(x => x.SearchLocationsAsync(It.IsAny<string>()))
				.ReturnsAsync(results);

			LocationSearchResult? capturedResult = null;
			Task OnLocationSelected(LocationSearchResult result)
			{
				capturedResult = result;
				return Task.CompletedTask;
			}

			var cut = RenderComponent<LocationSelector>(parameters => parameters
				.Add(p => p.OnLocationSelected, OnLocationSelected)
				.Add(p => p.DisableSearch, false).Add(p => p.DisableSelect, false));

			var searchInput = cut.Find("input[placeholder='Enter address or place name']");
			searchInput.Change("Berlin");
			var searchButton = cut.Find("button[type='submit']");
			await cut.InvokeAsync(() => searchButton.Click());

			var mapButton = cut.Find("button.btn-secondary");
			await cut.InvokeAsync(() => mapButton.Click());

			// Act - Find the select button in the modal footer
			var selectButton = cut.Find(".modal-footer button.btn-success");
			await cut.InvokeAsync(() => selectButton.Click());

			// Assert
			await Assert.That(capturedResult).IsNotNull();
			await Assert.That(capturedResult!.Value.Latitude).IsEqualTo(52.52);
			await Assert.That(capturedResult.Value.Longitude).IsEqualTo(13.405);

			// Modal should be closed
			var modals = cut.FindAll(".modal");
			await Assert.That(modals.Count).IsEqualTo(0);

			// Search results should be hidden
			var tables = cut.FindAll("table");
			await Assert.That(tables.Count).IsEqualTo(0);

			// Selected location label should be shown
			var selectedLocationDiv = cut.Find(".selected-location-label");
			await Assert.That(selectedLocationDiv.TextContent).Contains("Berlin, Germany");
		}
	}
}

