using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Infrastructure.BackgroundJobs;

// Nhắc 1 lần duy nhất cho mỗi công việc khi còn trong vòng 24h trước hạn - không nhắc lại nếu
// đã nhắc rồi (DueSoonReminderSentAtUtc), trừ khi hạn đổi (xem UpdateWorkTaskCommandHandler).
public class SendDueSoonReminderJob(IApplicationDbContext context, IPushNotificationService pushNotificationService, ILogger<SendDueSoonReminderJob> logger)
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
            foreach (var userId in task.Assignees.Select(a => a.UserId).Distinct())
            {
                await pushNotificationService.SendToUserAsync(
                    userId,
                    "Công việc sắp đến hạn",
                    $"\"{task.Title}\" sắp đến hạn vào {task.DueDateUtc:dd/MM/yyyy HH:mm} UTC.",
                    new Dictionary<string, string> { ["workTaskId"] = task.Id.ToString(), ["type"] = "DueSoon" },
                    cancellationToken);
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
