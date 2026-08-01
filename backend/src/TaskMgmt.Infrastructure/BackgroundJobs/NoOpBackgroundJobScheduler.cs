using Microsoft.Extensions.Logging;
using TaskMgmt.Application.Common.Interfaces;

namespace TaskMgmt.Infrastructure.BackgroundJobs;

// Dùng khi Hangfire bị tắt (Hangfire:Disabled=true, chỉ xảy ra trong test) - để Notification Event
// Handler vẫn resolve được IBackgroundJobScheduler mà không cần Hangfire storage thật, tương tự
// cách Firebase graceful-degrade khi chưa cấu hình.
public class NoOpBackgroundJobScheduler(ILogger<NoOpBackgroundJobScheduler> logger) : IBackgroundJobScheduler
{
    public void EnqueuePushNotification(Guid userId, string title, string body, IReadOnlyDictionary<string, string>? data = null) =>
        logger.LogDebug("Hangfire đã tắt - bỏ qua enqueue push notification cho user {UserId}.", userId);
}
