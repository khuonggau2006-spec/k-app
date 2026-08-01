using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Caching;
using TaskMgmt.Application.Common.Extensions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Common.Models;
using TaskMgmt.Application.Features.WorkTasks.Common;

namespace TaskMgmt.Application.Features.WorkTasks.Queries.GetWorkTasks;

public class GetWorkTasksQueryHandler(IApplicationDbContext context, ICacheService cache)
    : IRequestHandler<GetWorkTasksQuery, PagedResult<WorkTaskDto>>
{
    public Task<PagedResult<WorkTaskDto>> Handle(GetWorkTasksQuery request, CancellationToken cancellationToken)
    {
        var key = CacheKeys.WorkTaskList(
            request.Status, request.LocationId, request.ParentTaskId,
            request.PageNumber, request.PageSize, request.SortBy, request.SortDescending);

        return cache.GetOrSetAsync(key, CacheKeys.WorkTaskListExpiration, () => QueryAsync(request, cancellationToken), cancellationToken);
    }

    private async Task<PagedResult<WorkTaskDto>> QueryAsync(GetWorkTasksQuery request, CancellationToken cancellationToken)
    {
        var query = context.WorkTasks.Where(t => t.IsActive);

        if (request.Status is not null)
        {
            query = query.Where(t => t.Status == request.Status);
        }

        if (request.LocationId is not null)
        {
            query = query.Where(t => t.LocationId == request.LocationId);
        }

        if (request.ParentTaskId is not null)
        {
            query = query.Where(t => t.ParentTaskId == request.ParentTaskId);
        }

        query = (request.SortBy?.Trim().ToLowerInvariant()) switch
        {
            "title" => request.SortDescending ? query.OrderByDescending(t => t.Title) : query.OrderBy(t => t.Title),
            "duedate" => request.SortDescending ? query.OrderByDescending(t => t.DueDateUtc) : query.OrderBy(t => t.DueDateUtc),
            _ => request.SortDescending ? query.OrderByDescending(t => t.CreatedAtUtc) : query.OrderBy(t => t.CreatedAtUtc),
        };

        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new WorkTaskDto(
                t.Id,
                t.Title,
                t.Description,
                t.Status,
                t.DueDateUtc,
                t.IsActive,
                t.ParentTaskId,
                t.LocationId,
                t.CreatedAtUtc,
                t.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<WorkTaskDto>(items, pageNumber, pageSize, totalCount);
    }
}
