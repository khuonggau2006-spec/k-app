namespace TaskMgmt.Domain.Events;

public record AttachmentAddedEvent(
    Guid WorkTaskId, Guid AttachmentId, string FileName, Guid? ActorUserId, DateTimeOffset OccurredAtUtc) : IDomainEvent;

public record AttachmentRemovedEvent(
    Guid WorkTaskId, Guid AttachmentId, string FileName, Guid? ActorUserId, DateTimeOffset OccurredAtUtc) : IDomainEvent;
