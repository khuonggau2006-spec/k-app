using TaskMgmt.Domain.Common;

namespace TaskMgmt.Domain.Entities;

public class Attachment : AuditableEntity
{
    public required Guid WorkTaskId { get; set; }
    public WorkTask? WorkTask { get; set; }

    // Tên file gốc do người dùng upload, chỉ dùng để hiển thị/tải xuống.
    public required string FileName { get; set; }

    // Khoá lưu trữ ngẫu nhiên trong bucket (không dựa vào FileName) để tránh path traversal/đè file.
    public required string StorageKey { get; set; }

    public required string ContentType { get; set; }
    public required long SizeBytes { get; set; }

    // Ánh xạ tới CreatedByUserId (kế thừa từ AuditableEntity) để hiển thị người upload.
    public User? Uploader { get; set; }
}
