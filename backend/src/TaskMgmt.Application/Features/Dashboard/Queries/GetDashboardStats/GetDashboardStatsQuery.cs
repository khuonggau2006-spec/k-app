using MediatR;
using TaskMgmt.Application.Features.Dashboard.Common;

namespace TaskMgmt.Application.Features.Dashboard.Queries.GetDashboardStats;

public record GetDashboardStatsQuery(Guid? LocationId = null) : IRequest<DashboardStatsDto>;
