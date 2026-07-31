using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.API.Authorization;

public class SystemRoleAuthorizationHandler : AuthorizationHandler<SystemRoleRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SystemRoleRequirement requirement)
    {
        var roleClaim = context.User.FindFirstValue(ClaimTypes.Role);

        // SystemRole is declared Admin=0, Manager=1, Member=2 — lower value means
        // higher privilege, so the user's role must be numerically <= the minimum required.
        if (Enum.TryParse<SystemRole>(roleClaim, out var role) && role <= requirement.MinimumRole)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
