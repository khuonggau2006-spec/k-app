using MediatR;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Domain.Enums;
using TaskMgmt.Domain.Events;

namespace TaskMgmt.Application.Features.Realtime.EventHandlers;

public class CommentAddedRealtimeHandler(IRealtimeNotifier notifier) : INotificationHandler<CommentAddedEvent>
{
    public Task Handle(CommentAddedEvent notification, CancellationToken cancellationToken) =>
        notifier.NotifyTaskUpdatedAsync(notification.WorkTaskId, TaskHistoryActionType.CommentAdded, notification.OccurredAtUtc, cancellationToken);
}
