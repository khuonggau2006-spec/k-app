using Microsoft.Extensions.Logging.Abstractions;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Entities;
using TaskMgmt.Infrastructure.BackgroundJobs;

namespace TaskMgmt.Application.UnitTests.BackgroundJobs;

public class CleanupExpiredTokensJobTests
{
    [Fact]
    public async Task Execute_RemovesOnlyExpiredTokens()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        context.Users.Add(user);

        var expired = new RefreshToken
        {
            UserId = user.Id,
            Token = "expired-token",
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-20),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
        };
        var valid = new RefreshToken
        {
            UserId = user.Id,
            Token = "valid-token",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(13),
        };
        context.RefreshTokens.AddRange(expired, valid);
        await context.SaveChangesAsync(default);

        var job = new CleanupExpiredTokensJob(context, NullLogger<CleanupExpiredTokensJob>.Instance);

        await job.ExecuteAsync();

        var remaining = context.RefreshTokens.ToList();
        Assert.Single(remaining);
        Assert.Equal("valid-token", remaining[0].Token);
    }

    [Fact]
    public async Task Execute_NoExpiredTokens_DoesNothing()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        context.Users.Add(user);
        context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = "valid-token",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(13),
        });
        await context.SaveChangesAsync(default);

        var job = new CleanupExpiredTokensJob(context, NullLogger<CleanupExpiredTokensJob>.Instance);

        await job.ExecuteAsync();

        Assert.Single(context.RefreshTokens.ToList());
    }
}
