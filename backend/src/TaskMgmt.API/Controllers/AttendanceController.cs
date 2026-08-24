using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskMgmt.Application.Features.Attendance.Commands.CheckIn;
using TaskMgmt.Application.Features.Attendance.Commands.CheckOut;
using TaskMgmt.Application.Features.Attendance.Common;
using TaskMgmt.Application.Features.Attendance.Queries.GetAttendanceHistory;
using TaskMgmt.Application.Features.Attendance.Queries.GetAttendanceStats;
using TaskMgmt.Application.Features.Attendance.Queries.GetTodayAttendance;

namespace TaskMgmt.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/attendance")]
public class AttendanceController(ISender sender) : ControllerBase
{
    [HttpPost("check-in")]
    public async Task<ActionResult<AttendanceRecordDto>> CheckIn(CheckInCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("check-out")]
    public async Task<ActionResult<AttendanceRecordDto>> CheckOut(CheckOutCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet("today")]
    public async Task<ActionResult<AttendanceRecordDto?>> GetToday(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTodayAttendanceQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("history")]
    public async Task<ActionResult<List<AttendanceRecordDto>>> GetHistory(
        [FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAttendanceHistoryQuery(year, month), cancellationToken);
        return Ok(result);
    }

    [HttpGet("stats")]
    public async Task<ActionResult<AttendanceStatsDto>> GetStats(
        [FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAttendanceStatsQuery(year, month), cancellationToken);
        return Ok(result);
    }
}
