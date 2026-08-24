using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Attendance.Common;

namespace TaskMgmt.Application.Features.Attendance.Queries.GetAttendanceHistory;

public class GetAttendanceHistoryQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<GetAttendanceHistoryQuery, List<AttendanceRecordDto>>
{
    public async Task<List<AttendanceRecordDto>> Handle(GetAttendanceHistoryQuery request, CancellationToken cancellationToken)
    {
        return await context.AttendanceRecords
            .Where(a => a.UserId == currentUser.UserId
                        && a.WorkDate.Year == request.Year
                        && a.WorkDate.Month == request.Month)
            .OrderByDescending(a => a.WorkDate)
            .Select(a => new AttendanceRecordDto(
                a.Id, a.WorkDate, a.CheckInAtUtc, a.CheckInLocation!.Name, a.CheckOutAtUtc, a.CheckOutLocation!.Name))
            .ToListAsync(cancellationToken);
    }
}
