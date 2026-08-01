using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Domain.Events;

public record TaskAssigneeAddedEvent(
    Guid WorkTaskId, Guid AssigneeUserId, TaskAssigneeRole Role, Guid? ActorUserId, DateTimeOffset OccurredAtUtc) : IDomainEvent;

public record TaskAssigneeRemovedEvent(
    Guid WorkTaskId, Guid AssigneeUserId, Guid? ActorUserId, DateTimeOffset OccurredAtUtc) : IDomainEvent;

public record TaskAssigneeRoleChangedEvent(
    Guid WorkTaskId, Guid AssigneeUserId, TaskAssigneeRole OldRole, TaskAssigneeRole NewRole, Guid? ActorUserId, DateTimeOffset OccurredAtUtc)
    : IDomainEvent;
