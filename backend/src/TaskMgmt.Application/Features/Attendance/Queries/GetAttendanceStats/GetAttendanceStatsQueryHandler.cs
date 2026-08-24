using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Attendance.Common;

namespace TaskMgmt.Application.Features.Attendance.Queries.GetAttendanceStats;

public class GetAttendanceStatsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<GetAttendanceStatsQuery, AttendanceStatsDto>
{
    public async Task<AttendanceStatsDto> Handle(GetAttendanceStatsQuery request, CancellationToken cancellationToken)
    {
        var records = await context.AttendanceRecords
            .Where(a => a.UserId == currentUser.UserId
                        && a.WorkDate.Year == request.Year
                        && a.WorkDate.Month == request.Month
                        && a.CheckInAtUtc != null)
            .Select(a => new { a.CheckInAtUtc, a.CheckOutAtUtc })
            .ToListAsync(cancellationToken);

        var daysCheckedIn = records.Count;
        var totalHours = records
            .Where(a => a.CheckOutAtUtc != null)
            .Sum(a => (a.CheckOutAtUtc!.Value - a.CheckInAtUtc!.Value).TotalHours);

        return new AttendanceStatsDto(daysCheckedIn, totalHours);
    }
}
