using MediatR;
using TaskMgmt.Application.Features.Attendance.Common;

namespace TaskMgmt.Application.Features.Attendance.Commands.CheckOut;

public record CheckOutCommand(double Latitude, double Longitude) : IRequest<AttendanceRecordDto>;
