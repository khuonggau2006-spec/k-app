using MediatR;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.TaskHistories.Common;
using TaskMgmt.Domain.Entities;
using TaskMgmt.Domain.Enums;
using TaskMgmt.Domain.Events;

namespace TaskMgmt.Application.Features.TaskHistories.EventHandlers;

// Các handler này CHỈ Add() entity vào context, KHÔNG gọi SaveChangesAsync - việc ghi DB thật
// do DispatchDomainEventsInterceptor điều phối, gộp chung transaction với thay đổi gốc.

public class WorkTaskCreatedEventHandler(IApplicationDbContext context) : INotificationHandler<WorkTaskCreatedEvent>
{
    public Task Handle(WorkTaskCreatedEvent notification, CancellationToken cancellationToken)
    {
        context.TaskHistories.Add(new TaskHistory
        {
            WorkTaskId = notification.WorkTaskId,
            ActionType = TaskHistoryActionType.Created,
            Description = notification.ParentTaskId is null ? "Công việc được tạo." : "Công việc con được tạo.",
            ActorUserId = notification.ActorUserId,
            CreatedAtUtc = notification.OccurredAtUtc,
        });

        return Task.CompletedTask;
    }
}

public class WorkTaskFieldChangedEventHandler(IApplicationDbContext context) : INotificationHandler<WorkTaskFieldChangedEvent>
{
    public Task Handle(WorkTaskFieldChangedEvent notification, CancellationToken cancellationToken)
    {
        context.TaskHistories.Add(new TaskHistory
        {
            WorkTaskId = notification.WorkTaskId,
            ActionType = TaskHistoryActionType.FieldChanged,
            Description = $"Đã thay đổi {TaskHistoryFieldLabels.GetLabel(notification.FieldName)}.",
            FieldName = notification.FieldName,
            OldValue = notification.OldValue,
            NewValue = notification.NewValue,
            ActorUserId = notification.ActorUserId,
            CreatedAtUtc = notification.OccurredAtUtc,
        });

        return Task.CompletedTask;
    }
}

public class WorkTaskStatusChangedEventHandler(IApplicationDbContext context) : INotificationHandler<WorkTaskStatusChangedEvent>
{
    public Task Handle(WorkTaskStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        context.TaskHistories.Add(new TaskHistory
        {
            WorkTaskId = notification.WorkTaskId,
            ActionType = TaskHistoryActionType.StatusChanged,
            Description = $"Đã đổi trạng thái từ {notification.OldStatus} sang {notification.NewStatus}.",
            FieldName = nameof(WorkTask.Status),
            OldValue = notification.OldStatus.ToString(),
            NewValue = notification.NewStatus.ToString(),
            ActorUserId = notification.ActorUserId,
            CreatedAtUtc = notification.OccurredAtUtc,
        });

        return Task.CompletedTask;
    }
}

public class WorkTaskDeletedEventHandler(IApplicationDbContext context) : INotificationHandler<WorkTaskDeletedEvent>
{
    public Task Handle(WorkTaskDeletedEvent notification, CancellationToken cancellationToken)
    {
        context.TaskHistories.Add(new TaskHistory
        {
            WorkTaskId = notification.WorkTaskId,
            ActionType = TaskHistoryActionType.Deleted,
            Description = "Công việc đã bị xoá.",
            ActorUserId = notification.ActorUserId,
            CreatedAtUtc = notification.OccurredAtUtc,
        });

        return Task.CompletedTask;
    }
}
