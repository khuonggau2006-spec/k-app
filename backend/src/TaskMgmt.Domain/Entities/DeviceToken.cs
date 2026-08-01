using TaskMgmt.Domain.Common;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Domain.Entities;

// Token đăng ký FCM của 1 thiết bị. Token là duy nhất toàn hệ thống - nếu cùng một thiết bị
// đăng nhập bằng tài khoản khác, token được gán lại (upsert) thay vì tạo bản ghi trùng.
public class DeviceToken : BaseEntity
{
    public required Guid UserId { get; set; }
    public User? User { get; set; }

    public required string Token { get; set; }
    public DevicePlatform Platform { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset LastUsedAtUtc { get; set; }
}
