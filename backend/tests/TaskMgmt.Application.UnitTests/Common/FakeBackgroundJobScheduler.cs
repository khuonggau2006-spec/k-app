using TaskMgmt.Application.Common.Interfaces;

namespace TaskMgmt.Application.UnitTests.Common;

internal class FakeBackgroundJobScheduler : IBackgroundJobScheduler
{
    public List<(Guid UserId, string Title, string Body, IReadOnlyDictionary<string, string>? Data)> Enqueued { get; } = [];

    public void EnqueuePushNotification(Guid userId, string title, string body, IReadOnlyDictionary<string, string>? data = null) =>
        Enqueued.Add((userId, title, body, data));
}
