using MediatR;

namespace TaskMgmt.Domain.Events;

public interface IDomainEvent : INotification
{
    DateTimeOffset OccurredAtUtc { get; }
}
