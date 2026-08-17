using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Caching;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.Notifications.Commands.UpdateNotificationPreference;

public class UpdateNotificationPreferenceCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, ICacheService cache)
    : IRequestHandler<UpdateNotificationPreferenceCommand>
{
    public async Task Handle(UpdateNotificationPreferenceCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;

        var existing = await context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Type == request.Type, cancellationToken);

        if (request.IsEnabled)
        {
            // Bật lại = xoá row "đã tắt" nếu có. Đã bật sẵn (không có row) -> không làm gì, idempotent.
            if (existing is not null)
            {
                context.NotificationPreferences.Remove(existing);
            }
        }
        else if (existing is null)
        {
            // Tắt lần đầu = tạo row. Đã tắt sẵn -> không làm gì, idempotent.
            context.NotificationPreferences.Add(new NotificationPreference { UserId = userId, Type = request.Type });
        }

        await context.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync(CacheKeys.DisabledNotificationTypes(userId), cancellationToken);
    }
}
