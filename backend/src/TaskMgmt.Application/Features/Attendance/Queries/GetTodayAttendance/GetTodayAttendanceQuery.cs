using MediatR;
using TaskMgmt.Application.Features.Attendance.Common;

namespace TaskMgmt.Application.Features.Attendance.Queries.GetTodayAttendance;

public record GetTodayAttendanceQuery : IRequest<AttendanceRecordDto?>;
