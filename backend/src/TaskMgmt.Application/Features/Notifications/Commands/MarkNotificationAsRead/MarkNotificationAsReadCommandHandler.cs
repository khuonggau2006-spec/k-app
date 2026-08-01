using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Caching;
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.Notifications.Commands.MarkNotificationAsRead;

public class MarkNotificationAsReadCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, ICacheService cache)
    : IRequestHandler<MarkNotificationAsReadCommand>
{
    public async Task Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        // Lọc luôn theo UserId của người gọi - thông báo của người khác coi như không tồn tại,
        // tránh lộ thông tin về sự tồn tại của thông báo đó (404 thay vì 403).
        var notification = await context.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.Id && n.UserId == currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Notification), request.Id);

        if (notification.IsRead)
        {
            return;
        }

        notification.IsRead = true;
        notification.ReadAtUtc = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync(CacheKeys.UnreadNotificationCount(currentUser.UserId!.Value), cancellationToken);
    }
}
