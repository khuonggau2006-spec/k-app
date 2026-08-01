using MediatR;
using TaskMgmt.Application.Features.Comments.Common;

namespace TaskMgmt.Application.Features.Comments.Commands.CreateComment;

public record CreateCommentCommand(Guid WorkTaskId, string Content, List<Guid> MentionedUserIds) : IRequest<CommentDto>;
