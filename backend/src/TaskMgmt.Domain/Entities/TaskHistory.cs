using TaskMgmt.Domain.Common;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Domain.Entities;

// Bảng chỉ-ghi (append-only): không kế thừa AuditableEntity vì lịch sử không bao giờ được sửa
// sau khi tạo - không có UpdatedAtUtc/UpdatedByUserId vì khái niệm "sửa lịch sử" không tồn tại.
public class TaskHistory : BaseEntity
{
    public required Guid WorkTaskId { get; set; }
    public WorkTask? WorkTask { get; set; }

    public TaskHistoryActionType ActionType { get; set; }
    public required string Description { get; set; }

    public string? FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }

    // Dùng cho sự kiện liên quan tới người tham gia (assignee added/removed/role changed).
    public Guid? TargetUserId { get; set; }
    public User? Target { get; set; }

    public Guid? ActorUserId { get; set; }
    public User? Actor { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
