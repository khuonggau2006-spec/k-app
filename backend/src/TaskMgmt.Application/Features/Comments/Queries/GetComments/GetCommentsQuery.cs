using MediatR;
using TaskMgmt.Application.Features.Comments.Common;

namespace TaskMgmt.Application.Features.Comments.Queries.GetComments;

public record GetCommentsQuery(Guid WorkTaskId) : IRequest<List<CommentDto>>;
