using MediatR;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Domain.Enums;
using TaskMgmt.Domain.Events;

namespace TaskMgmt.Application.Features.Realtime.EventHandlers;

// Các handler này chạy SONG SONG với TaskHistory EventHandlers (cùng lắng nghe domain event),
// không phụ thuộc lẫn nhau - tắt/sửa 1 bên không ảnh hưởng bên kia, đúng tinh thần decoupling
// của domain event.

public class WorkTaskCreatedRealtimeHandler(IRealtimeNotifier notifier) : INotificationHandler<WorkTaskCreatedEvent>
{
    public Task Handle(WorkTaskCreatedEvent notification, CancellationToken cancellationToken) =>
        notifier.NotifyTaskUpdatedAsync(notification.WorkTaskId, TaskHistoryActionType.Created, notification.OccurredAtUtc, cancellationToken);
}

public class WorkTaskFieldChangedRealtimeHandler(IRealtimeNotifier notifier) : INotificationHandler<WorkTaskFieldChangedEvent>
{
    public Task Handle(WorkTaskFieldChangedEvent notification, CancellationToken cancellationToken) =>
        notifier.NotifyTaskUpdatedAsync(notification.WorkTaskId, TaskHistoryActionType.FieldChanged, notification.OccurredAtUtc, cancellationToken);
}

public class WorkTaskStatusChangedRealtimeHandler(IRealtimeNotifier notifier) : INotificationHandler<WorkTaskStatusChangedEvent>
{
    public Task Handle(WorkTaskStatusChangedEvent notification, CancellationToken cancellationToken) =>
        notifier.NotifyTaskUpdatedAsync(notification.WorkTaskId, TaskHistoryActionType.StatusChanged, notification.OccurredAtUtc, cancellationToken);
}

public class WorkTaskDeletedRealtimeHandler(IRealtimeNotifier notifier) : INotificationHandler<WorkTaskDeletedEvent>
{
    public Task Handle(WorkTaskDeletedEvent notification, CancellationToken cancellationToken) =>
        notifier.NotifyTaskUpdatedAsync(notification.WorkTaskId, TaskHistoryActionType.Deleted, notification.OccurredAtUtc, cancellationToken);
}
