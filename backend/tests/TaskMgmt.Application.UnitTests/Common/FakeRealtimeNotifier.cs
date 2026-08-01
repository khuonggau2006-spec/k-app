using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.UnitTests.Common;

internal class FakeRealtimeNotifier : IRealtimeNotifier
{
    public List<(Guid WorkTaskId, TaskHistoryActionType ActionType)> Notifications { get; } = [];

    public Task NotifyTaskUpdatedAsync(
        Guid workTaskId, TaskHistoryActionType actionType, DateTimeOffset occurredAtUtc, CancellationToken cancellationToken)
    {
        Notifications.Add((workTaskId, actionType));
        return Task.CompletedTask;
    }
}
