using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Caching;
using TaskMgmt.Application.Common.Extensions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Dashboard.Common;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.Features.Dashboard.Queries.GetDashboardStats;

public class GetDashboardStatsQueryHandler(IApplicationDbContext context, ICacheService cache)
    : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    private static readonly TimeSpan DueSoonWindow = TimeSpan.FromHours(24);

    public Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        return cache.GetOrSetAsync(
            CacheKeys.DashboardStats(request.LocationId),
            CacheKeys.DashboardStatsExpiration,
            () => QueryAsync(request, cancellationToken),
            cancellationToken);
    }

    private async Task<DashboardStatsDto> QueryAsync(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var query = context.WorkTasks.Where(t => t.IsActive);

        if (request.LocationId is not null)
        {
            query = query.Where(t => t.LocationId == request.LocationId);
        }

        var now = DateTimeOffset.UtcNow;
        var dueSoonThreshold = now.Add(DueSoonWindow);

        var counts = await query
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                ToDo = g.Count(t => t.Status == WorkTaskStatus.ToDo),
                InProgress = g.Count(t => t.Status == WorkTaskStatus.InProgress),
                InReview = g.Count(t => t.Status == WorkTaskStatus.InReview),
                Done = g.Count(t => t.Status == WorkTaskStatus.Done),
                Cancelled = g.Count(t => t.Status == WorkTaskStatus.Cancelled),
                Overdue = g.Count(t => t.DueDateUtc != null && t.DueDateUtc < now
                    && t.Status != WorkTaskStatus.Done && t.Status != WorkTaskStatus.Cancelled),
                DueSoon = g.Count(t => t.DueDateUtc != null && t.DueDateUtc >= now && t.DueDateUtc <= dueSoonThreshold
                    && t.Status != WorkTaskStatus.Done && t.Status != WorkTaskStatus.Cancelled),
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (counts is null)
        {
            return new DashboardStatsDto(0, 0, 0, 0, 0, 0, 0, 0);
        }

        return new DashboardStatsDto(
            counts.Total, counts.ToDo, counts.InProgress, counts.InReview, counts.Done, counts.Cancelled,
            counts.Overdue, counts.DueSoon);
    }
}
