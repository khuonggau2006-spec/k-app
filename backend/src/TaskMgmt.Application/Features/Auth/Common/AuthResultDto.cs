using TaskMgmt.Domain.Entities;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.Features.Auth.Common;

public record UserDto(Guid Id, string Email, string FullName, SystemRole SystemRole)
{
    public static UserDto FromEntity(User user) => new(user.Id, user.Email, user.FullName, user.SystemRole);
}

public record AuthResultDto(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    UserDto User);
