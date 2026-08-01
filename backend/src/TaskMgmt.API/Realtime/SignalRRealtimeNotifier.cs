using Microsoft.AspNetCore.SignalR;
using TaskMgmt.API.Hubs;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.API.Realtime;

public class SignalRRealtimeNotifier(IHubContext<NotificationHub> hubContext) : IRealtimeNotifier
{
    public Task NotifyTaskUpdatedAsync(
        Guid workTaskId, TaskHistoryActionType actionType, DateTimeOffset occurredAtUtc, CancellationToken cancellationToken) =>
        hubContext.Clients.Group(NotificationHub.TaskGroupName(workTaskId)).SendAsync(
            "TaskUpdated",
            new { workTaskId, actionType = actionType.ToString(), occurredAtUtc },
            cancellationToken);
}
