using TaskMgmt.Domain.Common;

namespace TaskMgmt.Domain.Entities;

// Bản ghi thông báo trong-app, tạo song song với việc enqueue push (mục 3.5) - cùng 1 sự kiện,
// hai kênh độc lập: Notification để hiển thị trong app (danh sách + đếm chưa đọc), còn
// SendPushNotificationJob lo đẩy ra thiết bị qua FCM. Type khớp với "type" gửi kèm data push
// (FieldChanged, StatusChanged, Deleted, AssigneeAdded, AssigneeRemoved, AssigneeRoleChanged,
// CommentAdded, AttachmentAdded) để client có thể xử lý deep-link nhất quán giữa 2 kênh.
public class Notification : BaseEntity
{
    public required Guid UserId { get; set; }
    public User? User { get; set; }

    public required string Title { get; set; }
    public required string Body { get; set; }
    public required string Type { get; set; }

    public Guid? WorkTaskId { get; set; }
    public WorkTask? WorkTask { get; set; }

    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
