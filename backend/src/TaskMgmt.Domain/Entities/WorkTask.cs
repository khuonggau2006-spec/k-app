using TaskMgmt.Domain.Common;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Domain.Entities;

public class WorkTask : AuditableEntity
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public WorkTaskStatus Status { get; set; } = WorkTaskStatus.ToDo;
    public DateTimeOffset? DueDateUtc { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid? ParentTaskId { get; set; }
    public WorkTask? ParentTask { get; set; }
    public ICollection<WorkTask> SubTasks { get; set; } = [];

    public Guid? LocationId { get; set; }
    public Location? Location { get; set; }

    public ICollection<TaskAssignee> Assignees { get; set; } = [];
}
