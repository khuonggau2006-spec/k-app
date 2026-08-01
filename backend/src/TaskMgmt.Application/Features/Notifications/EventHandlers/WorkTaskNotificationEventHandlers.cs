using MediatR;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Notifications.Common;
using TaskMgmt.Application.Features.TaskHistories.Common;
using TaskMgmt.Domain.Events;

namespace TaskMgmt.Application.Features.Notifications.EventHandlers;

// Song song với TaskHistory EventHandlers và Realtime EventHandlers - cùng lắng nghe domain event,
// không phụ thuộc lẫn nhau. Nhóm này quyết định AI cần nhận thông báo rồi gọi
// TaskNotificationHelper.NotifyAsync để vừa ghi Notification (mục 3.7) vừa enqueue push
// fire-and-forget (mục 3.5) qua IBackgroundJobScheduler, tránh việc gửi push (có thể chậm/lỗi do
// mạng/FCM) chặn hay làm rollback transaction nghiệp vụ gốc.
//
// Cố ý KHÔNG báo khi tạo mới (WorkTaskCreatedEvent) - người tạo đã biết, chưa có ai khác liên
// quan để báo.

public class WorkTaskFieldChangedNotificationHandler(IApplicationDbContext context, IBackgroundJobScheduler jobScheduler, ICacheService cache)
    : INotificationHandler<WorkTaskFieldChangedEvent>
{
    public async Task Handle(WorkTaskFieldChangedEvent notification, CancellationToken cancellationToken)
    {
        var (task, recipients) = await TaskNotificationHelper.GetTaskAndOtherAssigneesAsync(
            context, notification.WorkTaskId, notification.ActorUserId, cancellationToken);
        if (task is null)
        {
            return;
        }

        var label = TaskHistoryFieldLabels.GetLabel(notification.FieldName);
        foreach (var userId in recipients)
        {
            await TaskNotificationHelper.NotifyAsync(
                context, jobScheduler, cache, userId,
                "Công việc đã được cập nhật", $"\"{task.Title}\" vừa đổi {label}.",
                notification.WorkTaskId, "FieldChanged", notification.OccurredAtUtc, cancellationToken);
        }
    }
}

public class WorkTaskStatusChangedNotificationHandler(IApplicationDbContext context, IBackgroundJobScheduler jobScheduler, ICacheService cache)
    : INotificationHandler<WorkTaskStatusChangedEvent>
{
    public async Task Handle(WorkTaskStatusChangedEvent notification, CancellationToken cancellationToken)
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
                "Trạng thái công việc đã đổi", $"\"{task.Title}\" đã chuyển sang {notification.NewStatus}.",
                notification.WorkTaskId, "StatusChanged", notification.OccurredAtUtc, cancellationToken);
        }
    }
}

public class WorkTaskDeletedNotificationHandler(IApplicationDbContext context, IBackgroundJobScheduler jobScheduler, ICacheService cache)
    : INotificationHandler<WorkTaskDeletedEvent>
{
    public async Task Handle(WorkTaskDeletedEvent notification, CancellationToken cancellationToken)
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
                "Công việc đã bị xoá", $"\"{task.Title}\" đã bị xoá.",
                notification.WorkTaskId, "Deleted", notification.OccurredAtUtc, cancellationToken);
        }
    }
}
