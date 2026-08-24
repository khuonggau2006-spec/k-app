using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Attendance.Common;

namespace TaskMgmt.Application.Features.Attendance.Queries.GetTodayAttendance;

public class GetTodayAttendanceQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<GetTodayAttendanceQuery, AttendanceRecordDto?>
{
    public async Task<AttendanceRecordDto?> Handle(GetTodayAttendanceQuery request, CancellationToken cancellationToken)
    {
        var workDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);

        var record = await context.AttendanceRecords
            .Where(a => a.UserId == currentUser.UserId && a.WorkDate == workDate)
            .Select(a => new AttendanceRecordDto(
                a.Id, a.WorkDate, a.CheckInAtUtc, a.CheckInLocation!.Name, a.CheckOutAtUtc, a.CheckOutLocation!.Name))
            .FirstOrDefaultAsync(cancellationToken);

        return record;
    }
}
