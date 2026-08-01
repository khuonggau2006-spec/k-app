using MediatR;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Domain.Enums;
using TaskMgmt.Domain.Events;

namespace TaskMgmt.Application.Features.Realtime.EventHandlers;

public class TaskAssigneeAddedRealtimeHandler(IRealtimeNotifier notifier) : INotificationHandler<TaskAssigneeAddedEvent>
{
    public Task Handle(TaskAssigneeAddedEvent notification, CancellationToken cancellationToken) =>
        notifier.NotifyTaskUpdatedAsync(notification.WorkTaskId, TaskHistoryActionType.AssigneeAdded, notification.OccurredAtUtc, cancellationToken);
}

public class TaskAssigneeRemovedRealtimeHandler(IRealtimeNotifier notifier) : INotificationHandler<TaskAssigneeRemovedEvent>
{
    public Task Handle(TaskAssigneeRemovedEvent notification, CancellationToken cancellationToken) =>
        notifier.NotifyTaskUpdatedAsync(notification.WorkTaskId, TaskHistoryActionType.AssigneeRemoved, notification.OccurredAtUtc, cancellationToken);
}

public class TaskAssigneeRoleChangedRealtimeHandler(IRealtimeNotifier notifier) : INotificationHandler<TaskAssigneeRoleChangedEvent>
{
    public Task Handle(TaskAssigneeRoleChangedEvent notification, CancellationToken cancellationToken) =>
        notifier.NotifyTaskUpdatedAsync(
            notification.WorkTaskId, TaskHistoryActionType.AssigneeRoleChanged, notification.OccurredAtUtc, cancellationToken);
}
