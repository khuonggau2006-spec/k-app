using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Common.Models;
using TaskMgmt.Application.Features.Notifications.Common;

namespace TaskMgmt.Application.Features.Notifications.Queries.GetNotifications;

public class GetNotificationsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<GetNotificationsQuery, PagedResult<NotificationDto>>
{
    public async Task<PagedResult<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        var query = context.Notifications.Where(n => n.UserId == currentUser.UserId);

        if (request.UnreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<NotificationDto>(items.Select(NotificationDto.FromEntity).ToList(), pageNumber, pageSize, totalCount);
    }
}
