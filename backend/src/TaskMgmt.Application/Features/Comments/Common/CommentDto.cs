using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.Comments.Common;

public record CommentMentionDto(Guid UserId, string FullName, string Email);

public record CommentDto(
    Guid Id,
    Guid WorkTaskId,
    string Content,
    Guid? AuthorUserId,
    string AuthorFullName,
    string AuthorEmail,
    bool AuthorHasAvatar,
    DateTimeOffset CreatedAtUtc,
    List<CommentMentionDto> Mentions)
{
    // Yêu cầu Comment đã được load kèm Author và Mentions.MentionedUser (Include/ThenInclude).
    public static CommentDto FromEntity(Comment comment) => new(
        comment.Id,
        comment.WorkTaskId,
        comment.Content,
        comment.CreatedByUserId,
        comment.Author?.FullName ?? string.Empty,
        comment.Author?.Email ?? string.Empty,
        comment.Author?.AvatarStorageKey != null,
        comment.CreatedAtUtc,
        comment.Mentions
            .Select(m => new CommentMentionDto(m.MentionedUserId, m.MentionedUser?.FullName ?? string.Empty, m.MentionedUser?.Email ?? string.Empty))
            .ToList());
}
