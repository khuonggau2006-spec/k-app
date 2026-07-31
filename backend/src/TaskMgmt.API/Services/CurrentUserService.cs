using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.API.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid? UserId =>
        Guid.TryParse(User?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : null;

    public SystemRole? Role =>
        Enum.TryParse<SystemRole>(User?.FindFirstValue(ClaimTypes.Role), out var role) ? role : null;
}
