using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Caching;
using TaskMgmt.Application.Common.Interfaces;

namespace TaskMgmt.Application.Features.Notifications.Commands.MarkAllNotificationsAsRead;

public class MarkAllNotificationsAsReadCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, ICacheService cache)
    : IRequestHandler<MarkAllNotificationsAsReadCommand>
{
    public async Task Handle(MarkAllNotificationsAsReadCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;

        var unread = await context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        if (unread.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var notification in unread)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = now;
        }

        await context.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync(CacheKeys.UnreadNotificationCount(userId), cancellationToken);
    }
}
