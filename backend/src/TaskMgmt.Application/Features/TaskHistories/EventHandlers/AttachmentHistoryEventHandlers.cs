using MediatR;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Domain.Entities;
using TaskMgmt.Domain.Enums;
using TaskMgmt.Domain.Events;

namespace TaskMgmt.Application.Features.TaskHistories.EventHandlers;

public class AttachmentAddedEventHandler(IApplicationDbContext context) : INotificationHandler<AttachmentAddedEvent>
{
    public Task Handle(AttachmentAddedEvent notification, CancellationToken cancellationToken)
    {
        context.TaskHistories.Add(new TaskHistory
        {
            WorkTaskId = notification.WorkTaskId,
            ActionType = TaskHistoryActionType.AttachmentAdded,
            Description = $"Đã thêm tệp đính kèm: {notification.FileName}.",
            ActorUserId = notification.ActorUserId,
            CreatedAtUtc = notification.OccurredAtUtc,
        });

        return Task.CompletedTask;
    }
}

public class AttachmentRemovedEventHandler(IApplicationDbContext context) : INotificationHandler<AttachmentRemovedEvent>
{
    public Task Handle(AttachmentRemovedEvent notification, CancellationToken cancellationToken)
    {
        context.TaskHistories.Add(new TaskHistory
        {
            WorkTaskId = notification.WorkTaskId,
            ActionType = TaskHistoryActionType.AttachmentRemoved,
            Description = $"Đã xoá tệp đính kèm: {notification.FileName}.",
            ActorUserId = notification.ActorUserId,
            CreatedAtUtc = notification.OccurredAtUtc,
        });

        return Task.CompletedTask;
    }
}
