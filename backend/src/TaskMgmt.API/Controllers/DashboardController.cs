using MediatR;
using Microsoft.AspNetCore.Mvc;
using TaskMgmt.Application.Features.Dashboard.Common;
using TaskMgmt.Application.Features.Dashboard.Queries.GetDashboardStats;
using TaskMgmt.Application.Features.Dashboard.Queries.GetWeeklyCompletionStats;

namespace TaskMgmt.API.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
public class DashboardController(ISender sender) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetStats(
        [FromQuery] GetDashboardStatsQuery query, CancellationToken cancellationToken)
    {
        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("weekly-completion")]
    public async Task<ActionResult<List<WeeklyCompletionDto>>> GetWeeklyCompletion(
        [FromQuery] GetWeeklyCompletionStatsQuery query, CancellationToken cancellationToken)
    {
        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }
}
