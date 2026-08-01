using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Comments.Common;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.Comments.Queries.GetComments;

public class GetCommentsQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetCommentsQuery, List<CommentDto>>
{
    public async Task<List<CommentDto>> Handle(GetCommentsQuery request, CancellationToken cancellationToken)
    {
        var taskExists = await context.WorkTasks.AnyAsync(t => t.Id == request.WorkTaskId, cancellationToken);
        if (!taskExists)
        {
            throw new NotFoundException(nameof(WorkTask), request.WorkTaskId);
        }

        var comments = await context.Comments
            .Where(c => c.WorkTaskId == request.WorkTaskId)
            .Include(c => c.Author)
            .Include(c => c.Mentions).ThenInclude(m => m.MentionedUser)
            .OrderBy(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return comments.Select(CommentDto.FromEntity).ToList();
    }
}
