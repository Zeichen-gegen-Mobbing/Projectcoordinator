using FrontEnd.Authorization;
using TUnit.Assertions.Extensions;

namespace FrontEnd.Tests.Unit.Authorization;

public class RoleRequirementTests
{
    [Test]
    public async Task Constructor_SetsRoleProperty()
    {
        // Arrange & Act
        var requirement = new RoleRequirement("admin");

        // Assert
        await Assert.That(requirement.Role).IsEqualTo("admin");
    }

    [Test]
    [Arguments("admin")]
    [Arguments("projectcoordination")]
    [Arguments("viewer")]
    [Arguments("")]
    public async Task Constructor_AcceptsAnyString(string role)
    {
        // Arrange & Act
        var requirement = new RoleRequirement(role);

        // Assert
        await Assert.That(requirement.Role).IsEqualTo(role);
    }
}
