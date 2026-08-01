using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Caching;
using TaskMgmt.Application.Common.Extensions;
using TaskMgmt.Application.Common.Interfaces;

namespace TaskMgmt.Application.Features.Notifications.Queries.GetUnreadNotificationCount;

public class GetUnreadNotificationCountQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser, ICacheService cache)
    : IRequestHandler<GetUnreadNotificationCountQuery, int>
{
    public Task<int> Handle(GetUnreadNotificationCountQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;

        return cache.GetOrSetAsync(
            CacheKeys.UnreadNotificationCount(userId),
            CacheKeys.UnreadNotificationCountExpiration,
            () => context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken),
            cancellationToken);
    }
}
