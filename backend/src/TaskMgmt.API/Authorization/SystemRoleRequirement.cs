using Microsoft.AspNetCore.Authorization;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.API.Authorization;

public class SystemRoleRequirement(SystemRole minimumRole) : IAuthorizationRequirement
{
    public SystemRole MinimumRole { get; } = minimumRole;
}
