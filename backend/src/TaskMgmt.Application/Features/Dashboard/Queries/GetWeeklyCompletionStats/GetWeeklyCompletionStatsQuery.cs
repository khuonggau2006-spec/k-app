using MediatR;
using TaskMgmt.Application.Features.Dashboard.Common;

namespace TaskMgmt.Application.Features.Dashboard.Queries.GetWeeklyCompletionStats;

public record GetWeeklyCompletionStatsQuery(Guid? LocationId = null) : IRequest<List<WeeklyCompletionDto>>;
