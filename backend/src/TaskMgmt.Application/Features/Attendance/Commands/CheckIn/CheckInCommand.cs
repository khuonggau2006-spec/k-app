using MediatR;
using TaskMgmt.Application.Features.Attendance.Common;

namespace TaskMgmt.Application.Features.Attendance.Commands.CheckIn;

public record CheckInCommand(double Latitude, double Longitude) : IRequest<AttendanceRecordDto>;
