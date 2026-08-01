using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Domain.Events;

public record WorkTaskCreatedEvent(Guid WorkTaskId, Guid? ParentTaskId, Guid? ActorUserId, DateTimeOffset OccurredAtUtc) : IDomainEvent;

// Sự kiện dùng chung cho các trường đơn giản (Title, Description, DueDateUtc, LocationId, ParentTaskId).
// Status tách riêng thành WorkTaskStatusChangedEvent vì kế hoạch nêu rõ đây là loại thay đổi cần lọc riêng.
public record WorkTaskFieldChangedEvent(
    Guid WorkTaskId, string FieldName, string? OldValue, string? NewValue, Guid? ActorUserId, DateTimeOffset OccurredAtUtc) : IDomainEvent;

public record WorkTaskStatusChangedEvent(
    Guid WorkTaskId, WorkTaskStatus OldStatus, WorkTaskStatus NewStatus, Guid? ActorUserId, DateTimeOffset OccurredAtUtc) : IDomainEvent;

public record WorkTaskDeletedEvent(Guid WorkTaskId, Guid? ActorUserId, DateTimeOffset OccurredAtUtc) : IDomainEvent;
