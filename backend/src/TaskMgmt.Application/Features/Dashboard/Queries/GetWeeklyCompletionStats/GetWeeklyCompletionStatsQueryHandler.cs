using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Caching;
using TaskMgmt.Application.Common.Extensions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Dashboard.Common;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.Dashboard.Queries.GetWeeklyCompletionStats;

public class GetWeeklyCompletionStatsQueryHandler(IApplicationDbContext context, ICacheService cache)
    : IRequestHandler<GetWeeklyCompletionStatsQuery, List<WeeklyCompletionDto>>
{
    private const int WeeksCount = 8;
    private const string StatusFieldName = nameof(WorkTask.Status);
    private const string DoneStatusValue = "Done";

    public Task<List<WeeklyCompletionDto>> Handle(GetWeeklyCompletionStatsQuery request, CancellationToken cancellationToken)
    {
        return cache.GetOrSetAsync(
            CacheKeys.WeeklyCompletionStats(request.LocationId),
            CacheKeys.WeeklyCompletionStatsExpiration,
            () => QueryAsync(request, cancellationToken),
            cancellationToken);
    }

    private async Task<List<WeeklyCompletionDto>> QueryAsync(GetWeeklyCompletionStatsQuery request, CancellationToken cancellationToken)
    {
        var weekStarts = BuildWeekStarts(DateOnly.FromDateTime(DateTime.UtcNow));
        var earliestWeekStartUtc = new DateTimeOffset(weekStarts[0].ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var activeWorkTasks = context.WorkTasks.Where(t => t.IsActive);
        if (request.LocationId is not null)
        {
            activeWorkTasks = activeWorkTasks.Where(t => t.LocationId == request.LocationId);
        }

        var completionTimestamps = await context.TaskHistories
            .Where(h => h.FieldName == StatusFieldName && h.NewValue == DoneStatusValue && h.CreatedAtUtc >= earliestWeekStartUtc)
            .Join(activeWorkTasks, h => h.WorkTaskId, t => t.Id, (h, _) => h.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var countsByWeekStart = completionTimestamps
            .GroupBy(ts => WeekStartOf(DateOnly.FromDateTime(ts.UtcDateTime)))
            .ToDictionary(g => g.Key, g => g.Count());

        return weekStarts
            .Select(weekStart => new WeeklyCompletionDto(weekStart, countsByWeekStart.GetValueOrDefault(weekStart, 0)))
            .ToList();
    }

    private static List<DateOnly> BuildWeekStarts(DateOnly today)
    {
        var currentWeekStart = WeekStartOf(today);
        return Enumerable.Range(0, WeeksCount)
            .Select(i => currentWeekStart.AddDays(-7 * (WeeksCount - 1 - i)))
            .ToList();
    }

    // Tuần bắt đầu từ thứ Hai (DayOfWeek: Sunday=0..Saturday=6, nên +6 rồi mod 7 để Monday=0).
    private static DateOnly WeekStartOf(DateOnly date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-daysSinceMonday);
    }
}
