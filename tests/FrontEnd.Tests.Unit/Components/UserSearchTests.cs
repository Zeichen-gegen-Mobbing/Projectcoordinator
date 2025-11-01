using Bunit;
using FrontEnd.Components;
using FrontEnd.Models;
using FrontEnd.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TUnit.Assertions.Extensions;

namespace FrontEnd.Tests.Unit.Components;

public class UserSearchTests : Bunit.TestContext
{
	private readonly Mock<IUserService> _userServiceMock = new();

	public UserSearchTests()
	{
		Services.AddSingleton(_userServiceMock.Object);
	}

	public class Rendering : UserSearchTests
	{
		[Test]
		public async Task RendersUserSearchBox()
		{
			// Arrange & Act
			var cut = RenderComponent<UserSearch>();

			// Assert
			var searchInput = cut.Find("input[placeholder='Enter user name or email']");
			await Assert.That(searchInput).IsNotNull();
			var searchButton = cut.Find("button[type='submit']");
			await Assert.That(searchButton).IsNotNull();
		}

		[Test]
		public async Task RendersCustomTitle_WhenProvided()
		{
			// Arrange & Act
			var title = Guid.NewGuid().ToString();
			var cut = RenderComponent<UserSearch>(parameters => parameters
				.Add(p => p.Title, title));

			// Assert
			await Assert.That(cut.Markup).Contains(title);
		}

		[Test]
		public async Task RendersCustomLabel_WhenProvided()
		{
			// Arrange & Act
			var cut = RenderComponent<UserSearch>(parameters => parameters
				.Add(p => p.Label, "Search by name"));

			// Assert
			await Assert.That(cut.Markup).Contains("Search by name");
		}
	}

	public class SearchMethod : UserSearchTests
	{
		[Test]
		public async Task DisplaysUserSearchResults_WhenSearchReturnsResults()
		{
			// Arrange
			var users = new List<GraphUser>
			{
				new() { Id = "1", DisplayName = "John Doe", Mail = "john@example.com" },
				new() { Id = "2", DisplayName = "Jane Smith", Mail = "jane@example.com" }
			};

			_userServiceMock
				.Setup(x => x.SearchUsersAsync(It.IsAny<string>()))
				.ReturnsAsync(users);

			var cut = RenderComponent<UserSearch>();

			// Act
			var searchInput = cut.Find("input[placeholder='Enter user name or email']");
			searchInput.Change("John");
			var searchButton = cut.Find("button[type='submit']");
			await cut.InvokeAsync(() => searchButton.Click());

			// Assert
			await Assert.That(cut.Markup).Contains("John Doe");
			await Assert.That(cut.Markup).Contains("john@example.com");
			await Assert.That(cut.Markup).Contains("Jane Smith");
			await Assert.That(cut.Markup).Contains("jane@example.com");

			var userButtons = cut.FindAll(".list-group-item");
			await Assert.That(userButtons.Count).IsEqualTo(2);
		}

		[Test]
		public async Task DisplaysNoUsersFoundMessage_WhenSearchReturnsEmpty()
		{
			// Arrange
			_userServiceMock
				.Setup(x => x.SearchUsersAsync(It.IsAny<string>()))
				.ReturnsAsync(new List<GraphUser>());

			var cut = RenderComponent<UserSearch>();

			// Act
			var searchInput = cut.Find("input[placeholder='Enter user name or email']");
			searchInput.Change("NonExistentUser");
			var searchButton = cut.Find("button[type='submit']");
			await cut.InvokeAsync(() => searchButton.Click());

			// Assert
			await Assert.That(cut.Markup).Contains("No users found matching");
			await Assert.That(cut.Markup).Contains("NonExistentUser");
		}

		[Test]
		public async Task ShowsLoadingIndicator_WhenSearching()
		{
			// Arrange
			var tcs = new TaskCompletionSource<IEnumerable<GraphUser>>();
			_userServiceMock
				.Setup(x => x.SearchUsersAsync(It.IsAny<string>()))
				.Returns(tcs.Task);

			var cut = RenderComponent<UserSearch>();

			// Act
			var searchInput = cut.Find("input[placeholder='Enter user name or email']");
			searchInput.Change("John");
			var searchButton = cut.Find("button[type='submit']");
			await cut.InvokeAsync(() => searchButton.Click());

			// Assert - Button should be disabled and show spinner
			searchButton = cut.Find("button[type='submit']");
			await Assert.That(cut.Markup).Contains("spinner-border");

			// Cleanup
			tcs.SetResult(new List<GraphUser>());
		}

		[Test]
		public async Task DisablesButton_WhenSearchIsInProgress()
		{
			// Arrange
			var tcs = new TaskCompletionSource<IEnumerable<GraphUser>>();
			_userServiceMock
				.Setup(x => x.SearchUsersAsync(It.IsAny<string>()))
				.Returns(tcs.Task);

			var cut = RenderComponent<UserSearch>();

			// Act
			var searchInput = cut.Find("input[placeholder='Enter user name or email']");
			searchInput.Change("John");
			var searchButton = cut.Find("button[type='submit']");
			await cut.InvokeAsync(() => searchButton.Click());

			// Assert
			searchButton = cut.Find("button[type='submit']");
			await Assert.That(searchButton.HasAttribute("disabled")).IsTrue();

			// Complete the search
			tcs.SetResult(new List<GraphUser>());
			await cut.InvokeAsync(async () => await Task.Delay(10));

			// Assert - Button should be enabled again
			searchButton = cut.Find("button[type='submit']");
			await Assert.That(searchButton.HasAttribute("disabled")).IsFalse();
		}

		/// <summary>
		/// Given: UserService throws an exception during search
		/// When: The search is executed
		/// Then: The error is handled gracefully, no results are displayed, and OnUserSelected is not invoked
		/// </summary>
		[Test]
		public async Task ShowsErrorMessage_WhenSearchThrowsException()
		{
			// Arrange
			_userServiceMock
				.Setup(x => x.SearchUsersAsync(It.IsAny<string>()))
				.ThrowsAsync(new Exception("API Error"));

			GraphUser? capturedUser = new() { Id = "initial", DisplayName = "Initial User", Mail = "initial@example.com" };
			var callbackInvoked = false;

			var cut = RenderComponent<UserSearch>(parameters => parameters
				.Add(p => p.OnUserSelected, async (user) =>
				{
					capturedUser = user;
					callbackInvoked = true;
					await Task.CompletedTask;
				}));

			// Act
			var searchInput = cut.Find("input[placeholder='Enter user name or email']");
			searchInput.Change("TestUser");
			var searchButton = cut.Find("button[type='submit']");
			await cut.InvokeAsync(() => searchButton.Click());

			// Assert - No user items should be displayed
			var userButtons = cut.FindAll(".list-group-item");
			await Assert.That(userButtons.Count).IsEqualTo(0);

			// Assert - No "No users found" message should be shown (because searchPerformed is not set on exception)
			await Assert.That(cut.Markup).DoesNotContain("No users found matching");
			await Assert.That(cut.Markup).Contains("Something went wrong");

			// Assert - Button should be enabled again
			searchButton = cut.Find("button[type='submit']");
			await Assert.That(searchButton.HasAttribute("disabled")).IsFalse();
		}

		[Test]
		public async Task DoesNotSearchOrClearResults_WhenQueryIsEmpty()
		{
			// Arrange
			var cut = RenderComponent<UserSearch>();

			// Act - Submit with empty query
			var searchButton = cut.Find("button[type='submit']");
			await cut.InvokeAsync(() => searchButton.Click());

			// Assert - Service should not be called
			_userServiceMock.Verify(x => x.SearchUsersAsync(It.IsAny<string>()), Times.Never);

			// No results or messages should be shown
			var listGroups = cut.FindAll(".list-group");
			await Assert.That(listGroups.Count).IsEqualTo(0);
			await Assert.That(cut.Markup).DoesNotContain("No users found");
		}
	}

	public class SelectUserMethod : UserSearchTests
	{
		[Test]
		public async Task InvokesOnUserSelected_WhenUserIsClicked()
		{
			// Arrange
			var users = new List<GraphUser>
			{
				new() { Id = "1", DisplayName = "John Doe", Mail = "john@example.com" }
			};

			_userServiceMock
				.Setup(x => x.SearchUsersAsync(It.IsAny<string>()))
				.ReturnsAsync(users);

			GraphUser? capturedUser = null;
			var cut = RenderComponent<UserSearch>(parameters => parameters
				.Add(p => p.OnUserSelected, async (user) =>
				{
					capturedUser = user;
					await Task.CompletedTask;
				}));

			var searchInput = cut.Find("input[placeholder='Enter user name or email']");
			searchInput.Change("John");
			var searchButton = cut.Find("button[type='submit']");
			await cut.InvokeAsync(() => searchButton.Click());

			// Act
			var userButton = cut.Find(".list-group-item");
			await cut.InvokeAsync(() => userButton.Click());

			// Assert
			await Assert.That(capturedUser).IsNotNull();
			await Assert.That(capturedUser!.Id).IsEqualTo("1");
			await Assert.That(capturedUser.DisplayName).IsEqualTo("John Doe");
			await Assert.That(capturedUser.Mail).IsEqualTo("john@example.com");
		}

		[Test]
		public async Task HighlightsSelectedUser_WhenUserIsSelected()
		{
			// Arrange
			var selectedUser = new GraphUser { Id = "1", DisplayName = "John Doe", Mail = "john@example.com" };
			var users = new List<GraphUser>
			{
				selectedUser,
				new() { Id = "2", DisplayName = "Jane Smith", Mail = "jane@example.com" }
			};

			_userServiceMock
				.Setup(x => x.SearchUsersAsync(It.IsAny<string>()))
				.ReturnsAsync(users);

			var cut = RenderComponent<UserSearch>(parameters => parameters
				.Add(p => p.SelectedUser, selectedUser));

			var searchInput = cut.Find("input[placeholder='Enter user name or email']");
			searchInput.Change("John");
			var searchButton = cut.Find("button[type='submit']");
			await cut.InvokeAsync(() => searchButton.Click());

			// Act
			var userButtons = cut.FindAll(".list-group-item");

			// Assert
			await Assert.That(userButtons[0].ClassList.Contains("active")).IsTrue();
			await Assert.That(userButtons[1].ClassList.Contains("active")).IsFalse();
		}
	}
}
