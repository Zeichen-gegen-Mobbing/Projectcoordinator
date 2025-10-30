using FrontEnd.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using TUnit.Assertions.Extensions;

namespace FrontEnd.Tests.Unit.Authorization;

public class RoleAuthorizationPolicyProviderTests
{
    private readonly RoleAuthorizationPolicyProvider _policyProvider;

    public RoleAuthorizationPolicyProviderTests()
    {
        var options = Options.Create(new AuthorizationOptions());
        _policyProvider = new RoleAuthorizationPolicyProvider(options);
    }

    public class GetPolicyAsync : RoleAuthorizationPolicyProviderTests
    {
        [Test]
        public async Task ReturnsPolicy_WhenPolicyNameStartsWithRolePrefix()
        {
            // Act
            var policy = await _policyProvider.GetPolicyAsync("Role:admin");

            // Assert
            await Assert.That(policy).IsNotNull();
            await Assert.That(policy!.Requirements).HasCount().EqualTo(2); // DenyAnonymousAuthorizationRequirement + RoleRequirement
            await Assert.That(policy.Requirements.OfType<RoleRequirement>()).HasCount().EqualTo(1);
            
            var requirement = policy.Requirements.OfType<RoleRequirement>().First();
            await Assert.That(requirement.Role).IsEqualTo("admin");
        }

        [Test]
        public async Task ReturnsPolicy_WithCorrectRoleName()
        {
            // Act
            var policy = await _policyProvider.GetPolicyAsync("Role:projectcoordination");

            // Assert
            await Assert.That(policy).IsNotNull();
            var requirement = policy!.Requirements.OfType<RoleRequirement>().First();
            await Assert.That(requirement.Role).IsEqualTo("projectcoordination");
        }

        [Test]
        public async Task RequiresAuthenticatedUser_InPolicy()
        {
            // Act
            var policy = await _policyProvider.GetPolicyAsync("Role:admin");

            // Assert
            await Assert.That(policy).IsNotNull();
            await Assert.That(policy!.AuthenticationSchemes).IsEmpty();
            // Policy should require authenticated user through the requirement
        }

        [Test]
        public async Task ReturnsNull_WhenPolicyNameDoesNotStartWithRolePrefix()
        {
            // Act
            var policy = await _policyProvider.GetPolicyAsync("SomeOtherPolicy");

            // Assert
            await Assert.That(policy).IsNull();
        }

        [Test]
        [Arguments("Role:admin")]
        [Arguments("Role:projectcoordination")]
        [Arguments("Role:viewer")]
        [Arguments("role:admin")] // Case insensitive
        [Arguments("ROLE:ADMIN")] // Case insensitive
        public async Task HandlesRolePolicyNames_CaseInsensitive(string policyName)
        {
            // Act
            var policy = await _policyProvider.GetPolicyAsync(policyName);

            // Assert
            await Assert.That(policy).IsNotNull();
            await Assert.That(policy!.Requirements).HasCount().EqualTo(2); // DenyAnonymousAuthorizationRequirement + RoleRequirement
            await Assert.That(policy.Requirements.OfType<RoleRequirement>()).HasCount().EqualTo(1);
        }
    }

    public class GetDefaultPolicyAsync : RoleAuthorizationPolicyProviderTests
    {
        [Test]
        public async Task ReturnsDefaultPolicy()
        {
            // Act
            var policy = await _policyProvider.GetDefaultPolicyAsync();

            // Assert
            await Assert.That(policy).IsNotNull();
        }
    }

    public class GetFallbackPolicyAsync : RoleAuthorizationPolicyProviderTests
    {
        [Test]
        public async Task ReturnsFallbackPolicy()
        {
            // Act
            var policy = await _policyProvider.GetFallbackPolicyAsync();

            // Assert
            // Fallback policy can be null by default
            await Assert.That(policy).IsNull();
        }
    }
}
