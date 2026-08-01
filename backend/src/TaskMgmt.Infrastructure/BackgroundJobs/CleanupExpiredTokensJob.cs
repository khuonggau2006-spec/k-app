using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskMgmt.Application.Common.Interfaces;

namespace TaskMgmt.Infrastructure.BackgroundJobs;

// Dọn refresh token đã hết hạn để bảng RefreshTokens không phình vô hạn theo thời gian.
public class CleanupExpiredTokensJob(IApplicationDbContext context, ILogger<CleanupExpiredTokensJob> logger)
{
    public async Task ExecuteAsync()
    {
        var cancellationToken = CancellationToken.None;
        var now = DateTimeOffset.UtcNow;

        var expiredTokens = await context.RefreshTokens
            .Where(t => t.ExpiresAtUtc < now)
            .ToListAsync(cancellationToken);

        if (expiredTokens.Count > 0)
        {
            context.RefreshTokens.RemoveRange(expiredTokens);
            await context.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("CleanupExpiredTokensJob: đã xoá {Count} refresh token hết hạn.", expiredTokens.Count);
    }
}
