using MediatR;
using TaskMgmt.Application.Features.Attendance.Common;

namespace TaskMgmt.Application.Features.Attendance.Queries.GetAttendanceHistory;

public record GetAttendanceHistoryQuery(int Year, int Month) : IRequest<List<AttendanceRecordDto>>;
