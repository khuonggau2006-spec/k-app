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

    // Đánh dấu đã gửi nhắc hạn để SendDueSoonReminderJob/SendOverdueReminderJob không gửi
    // trùng lặp mỗi lần job chạy. Reset về null khi DueDateUtc đổi (xem UpdateWorkTaskCommandHandler).
    public DateTimeOffset? DueSoonReminderSentAtUtc { get; set; }
    public DateTimeOffset? OverdueReminderSentAtUtc { get; set; }

    public Guid? ParentTaskId { get; set; }
    public WorkTask? ParentTask { get; set; }
    public ICollection<WorkTask> SubTasks { get; set; } = [];

    public Guid? LocationId { get; set; }
    public Location? Location { get; set; }

    public ICollection<TaskAssignee> Assignees { get; set; } = [];
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<Attachment> Attachments { get; set; } = [];
    public ICollection<TaskHistory> Histories { get; set; } = [];
}
