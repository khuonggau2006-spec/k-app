using TaskMgmt.Domain.Common;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Domain.Entities;

public class TaskAssignee : BaseEntity
{
    public required Guid WorkTaskId { get; set; }
    public WorkTask? WorkTask { get; set; }

    public required Guid UserId { get; set; }
    public User? User { get; set; }

    public TaskAssigneeRole Role { get; set; } = TaskAssigneeRole.Assignee;
    public DateTimeOffset AssignedAtUtc { get; set; }
}
