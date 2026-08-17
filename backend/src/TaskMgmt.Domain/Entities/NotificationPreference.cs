using TaskMgmt.Domain.Common;

namespace TaskMgmt.Domain.Entities;

// Bảng THƯA - chỉ có row khi user TẮT 1 loại thông báo cụ thể; vắng mặt = mặc định bật.
// Chỉ ảnh hưởng kênh push (xem TaskNotificationHelper.NotifyAsync), không ảnh hưởng việc ghi
// Notification trong-app.
public class NotificationPreference : BaseEntity
{
    public required Guid UserId { get; set; }
    public User? User { get; set; }

    public required string Type { get; set; }
}
