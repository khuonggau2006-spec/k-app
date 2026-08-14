using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Notifications.Common;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Infrastructure.BackgroundJobs;

// Nhắc 1 lần duy nhất cho mỗi công việc khi còn trong vòng 24h trước hạn - không nhắc lại nếu
// đã nhắc rồi (DueSoonReminderSentAtUtc), trừ khi hạn đổi (xem UpdateWorkTaskCommandHandler).
public class SendDueSoonReminderJob(
    IApplicationDbContext context,
    IBackgroundJobScheduler jobScheduler,
    ICacheService cache,
    ILogger<SendDueSoonReminderJob> logger)
{
    private static readonly TimeSpan ReminderWindow = TimeSpan.FromHours(24);

    public async Task ExecuteAsync()
    {
        var cancellationToken = CancellationToken.None;
        var now = DateTimeOffset.UtcNow;
        var threshold = now.Add(ReminderWindow);

        var tasks = await context.WorkTasks
            .Include(t => t.Assignees)
            .Where(t => t.IsActive
                && t.DueSoonReminderSentAtUtc == null
                && t.DueDateUtc != null
                && t.DueDateUtc > now
                && t.DueDateUtc <= threshold
                && t.Status != WorkTaskStatus.Done
                && t.Status != WorkTaskStatus.Cancelled)
            .ToListAsync(cancellationToken);

        foreach (var task in tasks)
        {
            var assigneeIds = task.Assignees.Select(a => a.UserId).Distinct().ToList();
            foreach (var userId in assigneeIds)
            {
                // Dùng chung TaskNotificationHelper (như comment/assignee...) để nhắc hạn cũng
                // hiện trong Trung tâm thông báo trong app, không chỉ mỗi push (có thể tắt/không
                // cấu hình Firebase).
                await TaskNotificationHelper.NotifyAsync(
                    context, jobScheduler, cache, userId,
                    "Công việc sắp đến hạn", $"\"{task.Title}\" sắp đến hạn vào {task.DueDateUtc:dd/MM/yyyy HH:mm} UTC.",
                    task.Id, "DueSoon", now, cancellationToken);
            }

            task.DueSoonReminderSentAtUtc = now;
        }

        if (tasks.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("SendDueSoonReminderJob: đã gửi nhắc hạn cho {Count} công việc.", tasks.Count);
    }
}
