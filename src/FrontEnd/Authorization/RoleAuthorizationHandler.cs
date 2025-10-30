using Microsoft.AspNetCore.Authorization;
using FrontEnd.Services;

namespace FrontEnd.Authorization;

public class RoleAuthorizationHandler : AuthorizationHandler<RoleRequirement>
{
    private readonly IRoleService _roleService;

    public RoleAuthorizationHandler(IRoleService roleService)
    {
        _roleService = roleService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RoleRequirement requirement)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            return;
        }

        var hasRole = await _roleService.HasRole(requirement.Role);
        
        if (hasRole)
        {
            context.Succeed(requirement);
        }
    }
}
