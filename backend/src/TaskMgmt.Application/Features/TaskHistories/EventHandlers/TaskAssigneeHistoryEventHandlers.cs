using MediatR;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Domain.Entities;
using TaskMgmt.Domain.Enums;
using TaskMgmt.Domain.Events;

namespace TaskMgmt.Application.Features.TaskHistories.EventHandlers;

public class TaskAssigneeAddedEventHandler(IApplicationDbContext context) : INotificationHandler<TaskAssigneeAddedEvent>
{
    public Task Handle(TaskAssigneeAddedEvent notification, CancellationToken cancellationToken)
    {
        context.TaskHistories.Add(new TaskHistory
        {
            WorkTaskId = notification.WorkTaskId,
            ActionType = TaskHistoryActionType.AssigneeAdded,
            Description = $"Đã thêm người tham gia (vai trò {notification.Role}).",
            TargetUserId = notification.AssigneeUserId,
            ActorUserId = notification.ActorUserId,
            CreatedAtUtc = notification.OccurredAtUtc,
        });

        return Task.CompletedTask;
    }
}

public class TaskAssigneeRemovedEventHandler(IApplicationDbContext context) : INotificationHandler<TaskAssigneeRemovedEvent>
{
    public Task Handle(TaskAssigneeRemovedEvent notification, CancellationToken cancellationToken)
    {
        context.TaskHistories.Add(new TaskHistory
        {
            WorkTaskId = notification.WorkTaskId,
            ActionType = TaskHistoryActionType.AssigneeRemoved,
            Description = "Đã gỡ người tham gia.",
            TargetUserId = notification.AssigneeUserId,
            ActorUserId = notification.ActorUserId,
            CreatedAtUtc = notification.OccurredAtUtc,
        });

        return Task.CompletedTask;
    }
}

public class TaskAssigneeRoleChangedEventHandler(IApplicationDbContext context) : INotificationHandler<TaskAssigneeRoleChangedEvent>
{
    public Task Handle(TaskAssigneeRoleChangedEvent notification, CancellationToken cancellationToken)
    {
        context.TaskHistories.Add(new TaskHistory
        {
            WorkTaskId = notification.WorkTaskId,
            ActionType = TaskHistoryActionType.AssigneeRoleChanged,
            Description = $"Đã đổi vai trò người tham gia từ {notification.OldRole} sang {notification.NewRole}.",
            FieldName = nameof(TaskAssignee.Role),
            OldValue = notification.OldRole.ToString(),
            NewValue = notification.NewRole.ToString(),
            TargetUserId = notification.AssigneeUserId,
            ActorUserId = notification.ActorUserId,
            CreatedAtUtc = notification.OccurredAtUtc,
        });

        return Task.CompletedTask;
    }
}
