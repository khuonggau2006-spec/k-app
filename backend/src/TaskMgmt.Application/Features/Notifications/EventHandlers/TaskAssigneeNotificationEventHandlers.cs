using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Notifications.Common;
using TaskMgmt.Domain.Events;

namespace TaskMgmt.Application.Features.Notifications.EventHandlers;

// Khác với nhóm WorkTask ở trên - các sự kiện assignee nhắm tới MỘT người cụ thể (người được
// gán/gỡ/đổi vai trò), không phải toàn bộ danh sách assignee còn lại, nên không dùng
// TaskNotificationHelper.GetTaskAndOtherAssigneesAsync.

public class TaskAssigneeAddedNotificationHandler(IApplicationDbContext context, IBackgroundJobScheduler jobScheduler, ICacheService cache)
    : INotificationHandler<TaskAssigneeAddedEvent>
{
    public async Task Handle(TaskAssigneeAddedEvent notification, CancellationToken cancellationToken)
    {
        var task = await context.WorkTasks.FirstOrDefaultAsync(t => t.Id == notification.WorkTaskId, cancellationToken);
        if (task is null)
        {
            return;
        }

        await TaskNotificationHelper.NotifyAsync(
            context, jobScheduler, cache, notification.AssigneeUserId,
            "Bạn đã được gán vào công việc", $"Bạn đã được thêm vào \"{task.Title}\" với vai trò {notification.Role}.",
            notification.WorkTaskId, "AssigneeAdded", notification.OccurredAtUtc, cancellationToken);
    }
}

public class TaskAssigneeRemovedNotificationHandler(IApplicationDbContext context, IBackgroundJobScheduler jobScheduler, ICacheService cache)
    : INotificationHandler<TaskAssigneeRemovedEvent>
{
    public async Task Handle(TaskAssigneeRemovedEvent notification, CancellationToken cancellationToken)
    {
        var task = await context.WorkTasks.FirstOrDefaultAsync(t => t.Id == notification.WorkTaskId, cancellationToken);
        if (task is null)
        {
            return;
        }

        await TaskNotificationHelper.NotifyAsync(
            context, jobScheduler, cache, notification.AssigneeUserId,
            "Bạn đã được gỡ khỏi công việc", $"Bạn không còn tham gia \"{task.Title}\" nữa.",
            notification.WorkTaskId, "AssigneeRemoved", notification.OccurredAtUtc, cancellationToken);
    }
}

public class TaskAssigneeRoleChangedNotificationHandler(IApplicationDbContext context, IBackgroundJobScheduler jobScheduler, ICacheService cache)
    : INotificationHandler<TaskAssigneeRoleChangedEvent>
{
    public async Task Handle(TaskAssigneeRoleChangedEvent notification, CancellationToken cancellationToken)
    {
        var task = await context.WorkTasks.FirstOrDefaultAsync(t => t.Id == notification.WorkTaskId, cancellationToken);
        if (task is null)
        {
            return;
        }

        await TaskNotificationHelper.NotifyAsync(
            context, jobScheduler, cache, notification.AssigneeUserId,
            "Vai trò của bạn đã thay đổi", $"Vai trò của bạn trong \"{task.Title}\" đã đổi thành {notification.NewRole}.",
            notification.WorkTaskId, "AssigneeRoleChanged", notification.OccurredAtUtc, cancellationToken);
    }
}
