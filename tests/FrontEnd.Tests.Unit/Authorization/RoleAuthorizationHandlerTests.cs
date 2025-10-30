using System.Security.Claims;
using FrontEnd.Authorization;
using FrontEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Moq;
using TUnit.Assertions.Extensions;

namespace FrontEnd.Tests.Unit.Authorization;

public class RoleAuthorizationHandlerTests
{
    // Test-specific subclass to expose protected method
    private class TestableRoleAuthorizationHandler : RoleAuthorizationHandler
    {
        public TestableRoleAuthorizationHandler(IRoleService roleService) : base(roleService)
        {
        }

        public Task HandleRequirementAsyncPublic(
            AuthorizationHandlerContext context,
            RoleRequirement requirement)
        {
            return HandleRequirementAsync(context, requirement);
        }
    }

    private readonly Mock<IRoleService> _roleServiceMock;
    private readonly Mock<AuthorizationHandlerContext> _contextMock;
    private readonly ClaimsPrincipal _authenticatedUser;
    private readonly TestableRoleAuthorizationHandler _handler;

    public RoleAuthorizationHandlerTests()
    {
        _roleServiceMock = new Mock<IRoleService>();
        _handler = new TestableRoleAuthorizationHandler(_roleServiceMock.Object);

        // Default authenticated user
        _authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "Test User")],
            "TestAuth"));

        var requirement = new RoleRequirement("admin");
        _contextMock = new Mock<AuthorizationHandlerContext>(
            new[] { requirement },
            _authenticatedUser,
            null!);
        _contextMock.SetupGet(x => x.User).Returns(_authenticatedUser);
    }

    public class HandleRequirementAsync : RoleAuthorizationHandlerTests
    {
        [Test]
        public async Task Succeeds_WhenUserHasRole()
        {
            // Arrange
            var requirement = new RoleRequirement("admin");
            _roleServiceMock.Setup(x => x.HasRole("admin"))
                .ReturnsAsync(true);

            // Act
            await _handler.HandleRequirementAsyncPublic(_contextMock.Object, requirement);

            // Assert
            _contextMock.Verify(x => x.Succeed(requirement), Times.Once);
            _roleServiceMock.Verify(x => x.HasRole("admin"), Times.Once);
        }

        [Test]
        public async Task DoesNotSucceed_WhenUserDoesNotHaveRole()
        {
            // Arrange
            var requirement = new RoleRequirement("admin");
            _roleServiceMock.Setup(x => x.HasRole("admin"))
                .ReturnsAsync(false);

            // Act
            await _handler.HandleRequirementAsyncPublic(_contextMock.Object, requirement);

            // Assert
            _contextMock.Verify(x => x.Succeed(It.IsAny<IAuthorizationRequirement>()), Times.Never);
            _roleServiceMock.Verify(x => x.HasRole("admin"), Times.Once);
        }

        [Test]
        public async Task DoesNotSucceed_WhenUserIsNotAuthenticated()
        {
            // Arrange
            var unauthenticatedUser = new ClaimsPrincipal(new ClaimsIdentity()); // Not authenticated
            var requirement = new RoleRequirement("admin");
            _contextMock.SetupGet(x => x.User).Returns(unauthenticatedUser);

            // Act
            await _handler.HandleRequirementAsyncPublic(_contextMock.Object, requirement);

            // Assert
            _contextMock.Verify(x => x.Succeed(It.IsAny<IAuthorizationRequirement>()), Times.Never);
            _roleServiceMock.Verify(x => x.HasRole(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task PassesCorrectRoleName_ToRoleService()
        {
            // Arrange
            var requirement = new RoleRequirement("projectcoordination");
            _roleServiceMock.Setup(x => x.HasRole("projectcoordination"))
                .ReturnsAsync(true);

            // Act
            await _handler.HandleRequirementAsyncPublic(_contextMock.Object, requirement);

            // Assert
            _roleServiceMock.Verify(x => x.HasRole("projectcoordination"), Times.Once);
        }
    }
}
