using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Common.Models;
using TaskMgmt.Application.Features.TaskHistories.Common;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.TaskHistories.Queries.GetTaskHistory;

public class GetTaskHistoryQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetTaskHistoryQuery, PagedResult<TaskHistoryDto>>
{
    public async Task<PagedResult<TaskHistoryDto>> Handle(GetTaskHistoryQuery request, CancellationToken cancellationToken)
    {
        var taskExists = await context.WorkTasks.AnyAsync(t => t.Id == request.WorkTaskId, cancellationToken);
        if (!taskExists)
        {
            throw new NotFoundException(nameof(WorkTask), request.WorkTaskId);
        }

        var query = context.TaskHistories.Where(h => h.WorkTaskId == request.WorkTaskId);

        if (request.ActionType is not null)
        {
            query = query.Where(h => h.ActionType == request.ActionType);
        }

        if (request.ActorUserId is not null)
        {
            query = query.Where(h => h.ActorUserId == request.ActorUserId);
        }

        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(h => h.Actor)
            .Include(h => h.Target)
            .OrderByDescending(h => h.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TaskHistoryDto>(items.Select(TaskHistoryDto.FromEntity).ToList(), pageNumber, pageSize, totalCount);
    }
}
