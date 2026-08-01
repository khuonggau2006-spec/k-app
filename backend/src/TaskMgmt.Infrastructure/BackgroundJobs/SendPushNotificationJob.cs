using Hangfire;
using Microsoft.Extensions.Logging;
using TaskMgmt.Application.Common.Interfaces;

namespace TaskMgmt.Infrastructure.BackgroundJobs;

// Job fire-and-forget: được enqueue từ Notification Event Handler ngay khi domain event xảy ra,
// chạy NGOÀI transaction gốc nên gửi push chậm/lỗi không ảnh hưởng tới thao tác nghiệp vụ ban đầu.
// Retry tối đa 3 lần với backoff tăng dần - FCM/mạng có thể chập chờn tạm thời.
[AutomaticRetry(Attempts = 3, DelaysInSeconds = [30, 300, 900])]
public class SendPushNotificationJob(IPushNotificationService pushNotificationService, ILogger<SendPushNotificationJob> logger)
{
    public async Task ExecuteAsync(Guid userId, string title, string body, Dictionary<string, string>? data)
    {
        var result = await pushNotificationService.SendToUserAsync(userId, title, body, data, CancellationToken.None);
        logger.LogInformation(
            "SendPushNotificationJob: user {UserId} - thành công {Success}, thất bại {Failure}.",
            userId, result.SuccessCount, result.FailureCount);
    }
}
