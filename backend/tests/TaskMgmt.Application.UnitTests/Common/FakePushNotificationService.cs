using TaskMgmt.Application.Common.Interfaces;

namespace TaskMgmt.Application.UnitTests.Common;

internal class FakePushNotificationService : IPushNotificationService
{
    public List<(Guid UserId, string Title, string Body)> Sent { get; } = [];

    public Task<PushSendResult> SendToUserAsync(
        Guid userId, string title, string body, IReadOnlyDictionary<string, string>? data, CancellationToken cancellationToken)
    {
        Sent.Add((userId, title, body));
        return Task.FromResult(new PushSendResult(1, 0));
    }
}
