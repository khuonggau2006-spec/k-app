using MediatR;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Notifications.Common;
using TaskMgmt.Domain.Events;

namespace TaskMgmt.Application.Features.Notifications.EventHandlers;

// Cố ý chỉ báo khi THÊM tệp đính kèm, không báo khi xoá - việc xoá tệp ít quan trọng hơn và
// tránh làm phiền người dùng với quá nhiều thông báo (AttachmentRemovedEvent vẫn được TaskHistory
// và Realtime xử lý bình thường, chỉ không tạo Notification/push).

public class AttachmentAddedNotificationHandler(IApplicationDbContext context, IBackgroundJobScheduler jobScheduler, ICacheService cache)
    : INotificationHandler<AttachmentAddedEvent>
{
    public async Task Handle(AttachmentAddedEvent notification, CancellationToken cancellationToken)
    {
        var (task, recipients) = await TaskNotificationHelper.GetTaskAndOtherAssigneesAsync(
            context, notification.WorkTaskId, notification.ActorUserId, cancellationToken);
        if (task is null)
        {
            return;
        }

        foreach (var userId in recipients)
        {
            await TaskNotificationHelper.NotifyAsync(
                context, jobScheduler, cache, userId,
                "Tệp đính kèm mới", $"Đã thêm \"{notification.FileName}\" vào \"{task.Title}\".",
                notification.WorkTaskId, "AttachmentAdded", notification.OccurredAtUtc, cancellationToken);
        }
    }
}
