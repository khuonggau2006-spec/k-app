namespace TaskMgmt.Application.Common.Interfaces;

// Trừu tượng hoá việc enqueue job nền (fire-and-forget) khỏi Application, để lớp này không phụ
// thuộc trực tiếp vào Hangfire - tương tự cách IRealtimeNotifier/IPushNotificationService đã làm.
public interface IBackgroundJobScheduler
{
    void EnqueuePushNotification(Guid userId, string title, string body, IReadOnlyDictionary<string, string>? data = null);
}
