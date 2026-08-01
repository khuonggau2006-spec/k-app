using TaskMgmt.Domain.Common;

namespace TaskMgmt.Domain.Entities;

public class Comment : AuditableEntity
{
    public required Guid WorkTaskId { get; set; }
    public WorkTask? WorkTask { get; set; }

    public required string Content { get; set; }

    // Ánh xạ tới CreatedByUserId (kế thừa từ AuditableEntity) để hiển thị người viết bình luận.
    public User? Author { get; set; }

    public ICollection<CommentMention> Mentions { get; set; } = [];
}
