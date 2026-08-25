using MediatR;
using TaskMgmt.Application.Features.Attendance.Common;

namespace TaskMgmt.Application.Features.Attendance.Queries.GetAttendanceStats;

public record GetAttendanceStatsQuery(int Year, int Month) : IRequest<AttendanceStatsDto>;
