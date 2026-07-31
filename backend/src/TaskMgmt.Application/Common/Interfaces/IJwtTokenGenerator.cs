using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    (string Token, DateTimeOffset ExpiresAtUtc) GenerateAccessToken(User user);
}
