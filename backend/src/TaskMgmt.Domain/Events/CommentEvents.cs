namespace TaskMgmt.Domain.Events;

public record CommentAddedEvent(Guid WorkTaskId, Guid CommentId, Guid? ActorUserId, DateTimeOffset OccurredAtUtc) : IDomainEvent;
