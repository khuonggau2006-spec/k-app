using TaskMgmt.Domain.Common;

namespace TaskMgmt.Domain.Entities;

// Lưu lại user được @mention trong một Comment để phục vụ thông báo ở G3;
// bản thân G2 chỉ lưu dữ liệu, chưa xử lý gửi notification.
public class CommentMention : BaseEntity
{
    public required Guid CommentId { get; set; }
    public Comment? Comment { get; set; }

    public required Guid MentionedUserId { get; set; }
    public User? MentionedUser { get; set; }
}
