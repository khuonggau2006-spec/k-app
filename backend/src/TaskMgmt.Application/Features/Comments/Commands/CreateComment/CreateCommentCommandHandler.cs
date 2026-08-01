using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Comments.Common;
using TaskMgmt.Domain.Entities;
using TaskMgmt.Domain.Events;

namespace TaskMgmt.Application.Features.Comments.Commands.CreateComment;

public class CreateCommentCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<CreateCommentCommand, CommentDto>
{
    public async Task<CommentDto> Handle(CreateCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = new Comment
        {
            WorkTaskId = request.WorkTaskId,
            Content = request.Content,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedByUserId = currentUser.UserId,
        };
        comment.AddDomainEvent(new CommentAddedEvent(request.WorkTaskId, comment.Id, currentUser.UserId, comment.CreatedAtUtc));

        foreach (var userId in request.MentionedUserIds.Distinct())
        {
            comment.Mentions.Add(new CommentMention { CommentId = comment.Id, MentionedUserId = userId });
        }

        context.Comments.Add(comment);
        await context.SaveChangesAsync(cancellationToken);

        comment.Author = currentUser.UserId is { } authorId
            ? await context.Users.FirstOrDefaultAsync(u => u.Id == authorId, cancellationToken)
            : null;

        var mentionedUsers = await context.Users
            .Where(u => request.MentionedUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        foreach (var mention in comment.Mentions)
        {
            mentionedUsers.TryGetValue(mention.MentionedUserId, out var mentionedUser);
            mention.MentionedUser = mentionedUser;
        }

        return CommentDto.FromEntity(comment);
    }
}
