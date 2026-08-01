using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Infrastructure.BackgroundJobs;

// Khác với nhắc sắp đến hạn (chỉ nhắc 1 lần), quá hạn thì nhắc lại định kỳ (mỗi ResendInterval)
// cho tới khi công việc được xử lý xong hoặc huỷ - vẫn còn quá hạn thì vẫn còn cần nhắc.
public class SendOverdueReminderJob(IApplicationDbContext context, IPushNotificationService pushNotificationService, ILogger<SendOverdueReminderJob> logger)
{
    private static readonly TimeSpan ResendInterval = TimeSpan.FromHours(24);

    public async Task ExecuteAsync()
    {
        var cancellationToken = CancellationToken.None;
        var now = DateTimeOffset.UtcNow;
        var resendBefore = now.Subtract(ResendInterval);

        var tasks = await context.WorkTasks
            .Include(t => t.Assignees)
            .Where(t => t.IsActive
                && t.DueDateUtc != null
                && t.DueDateUtc < now
                && t.Status != WorkTaskStatus.Done
                && t.Status != WorkTaskStatus.Cancelled
                && (t.OverdueReminderSentAtUtc == null || t.OverdueReminderSentAtUtc < resendBefore))
            .ToListAsync(cancellationToken);

        foreach (var task in tasks)
        {
            foreach (var userId in task.Assignees.Select(a => a.UserId).Distinct())
            {
                await pushNotificationService.SendToUserAsync(
                    userId,
                    "Công việc đã quá hạn",
                    $"\"{task.Title}\" đã quá hạn từ {task.DueDateUtc:dd/MM/yyyy HH:mm} UTC.",
                    new Dictionary<string, string> { ["workTaskId"] = task.Id.ToString(), ["type"] = "Overdue" },
                    cancellationToken);
            }

            task.OverdueReminderSentAtUtc = now;
        }

        if (tasks.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("SendOverdueReminderJob: đã gửi nhắc quá hạn cho {Count} công việc.", tasks.Count);
    }
}
