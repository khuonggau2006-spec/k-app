using MediatR;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Domain.Entities;
using TaskMgmt.Domain.Enums;
using TaskMgmt.Domain.Events;

namespace TaskMgmt.Application.Features.TaskHistories.EventHandlers;

public class CommentAddedEventHandler(IApplicationDbContext context) : INotificationHandler<CommentAddedEvent>
{
    public Task Handle(CommentAddedEvent notification, CancellationToken cancellationToken)
    {
        context.TaskHistories.Add(new TaskHistory
        {
            WorkTaskId = notification.WorkTaskId,
            ActionType = TaskHistoryActionType.CommentAdded,
            Description = "Đã thêm bình luận.",
            ActorUserId = notification.ActorUserId,
            CreatedAtUtc = notification.OccurredAtUtc,
        });

        return Task.CompletedTask;
    }
}
