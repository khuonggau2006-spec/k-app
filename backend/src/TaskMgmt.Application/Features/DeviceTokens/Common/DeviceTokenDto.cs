using TaskMgmt.Domain.Entities;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.Features.DeviceTokens.Common;

public record DeviceTokenDto(Guid Id, string Token, DevicePlatform Platform, DateTimeOffset CreatedAtUtc, DateTimeOffset LastUsedAtUtc)
{
    public static DeviceTokenDto FromEntity(DeviceToken token) => new(
        token.Id, token.Token, token.Platform, token.CreatedAtUtc, token.LastUsedAtUtc);
}
