using MediatR;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Domain.Enums;
using TaskMgmt.Domain.Events;

namespace TaskMgmt.Application.Features.Realtime.EventHandlers;

public class AttachmentAddedRealtimeHandler(IRealtimeNotifier notifier) : INotificationHandler<AttachmentAddedEvent>
{
    public Task Handle(AttachmentAddedEvent notification, CancellationToken cancellationToken) =>
        notifier.NotifyTaskUpdatedAsync(notification.WorkTaskId, TaskHistoryActionType.AttachmentAdded, notification.OccurredAtUtc, cancellationToken);
}

public class AttachmentRemovedRealtimeHandler(IRealtimeNotifier notifier) : INotificationHandler<AttachmentRemovedEvent>
{
    public Task Handle(AttachmentRemovedEvent notification, CancellationToken cancellationToken) =>
        notifier.NotifyTaskUpdatedAsync(notification.WorkTaskId, TaskHistoryActionType.AttachmentRemoved, notification.OccurredAtUtc, cancellationToken);
}
