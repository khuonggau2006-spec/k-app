using Hangfire;
using TaskMgmt.Application.Common.Interfaces;

namespace TaskMgmt.Infrastructure.BackgroundJobs;

public class HangfireBackgroundJobScheduler(IBackgroundJobClient jobClient) : IBackgroundJobScheduler
{
    public void EnqueuePushNotification(Guid userId, string title, string body, IReadOnlyDictionary<string, string>? data = null)
    {
        var dataDictionary = data is null ? null : new Dictionary<string, string>(data);
        jobClient.Enqueue<SendPushNotificationJob>(job => job.ExecuteAsync(userId, title, body, dataDictionary));
    }
}
