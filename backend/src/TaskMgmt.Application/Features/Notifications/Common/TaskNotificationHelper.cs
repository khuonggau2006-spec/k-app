using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Caching;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.Notifications.Common;

// Public (không internal) vì các recurring job nhắc hạn ở TaskMgmt.Infrastructure cũng cần dùng
// chung điểm hội tụ này, tránh lặp lại logic ghi Notification + enqueue push + invalidate cache.
public static class TaskNotificationHelper
{
    // Trả về công việc + danh sách userId của các assignee khác (loại trừ actor - không cần tự
    // báo cho chính người vừa thực hiện hành động).
    public static async Task<(WorkTask? Task, List<Guid> OtherAssigneeUserIds)> GetTaskAndOtherAssigneesAsync(
        IApplicationDbContext context, Guid workTaskId, Guid? excludeUserId, CancellationToken cancellationToken)
    {
        var task = await context.WorkTasks.FirstOrDefaultAsync(t => t.Id == workTaskId, cancellationToken);
        if (task is null)
        {
            return (null, []);
        }

        var recipientIds = await context.TaskAssignees
            .Where(a => a.WorkTaskId == workTaskId && a.UserId != excludeUserId)
            .Select(a => a.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return (task, recipientIds);
    }

    // Điểm hội tụ duy nhất cho 1 lượt "báo cho user" - vừa ghi Notification vào cùng transaction
    // với thao tác nghiệp vụ gốc (để hiện trong danh sách trong-app + đếm chưa đọc), vừa enqueue
    // push fire-and-forget (mục 3.5), vừa invalidate cache đếm chưa đọc của đúng người nhận.
    public static async Task NotifyAsync(
        IApplicationDbContext context, IBackgroundJobScheduler jobScheduler, ICacheService cache,
        Guid userId, string title, string body, Guid? workTaskId, string type, DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken)
    {
        context.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Body = body,
            Type = type,
            WorkTaskId = workTaskId,
            CreatedAtUtc = occurredAtUtc,
        });

        await cache.RemoveAsync(CacheKeys.UnreadNotificationCount(userId), cancellationToken);

        var data = new Dictionary<string, string> { ["type"] = type };
        if (workTaskId is not null)
        {
            data["workTaskId"] = workTaskId.Value.ToString();
        }

        jobScheduler.EnqueuePushNotification(userId, title, body, data);
    }
}
