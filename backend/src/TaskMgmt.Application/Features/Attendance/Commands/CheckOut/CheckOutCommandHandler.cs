using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Attendance.Common;

namespace TaskMgmt.Application.Features.Attendance.Commands.CheckOut;

public class CheckOutCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<CheckOutCommand, AttendanceRecordDto>
{
    public async Task<AttendanceRecordDto> Handle(CheckOutCommand request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var workDate = DateOnly.FromDateTime(now.ToOffset(TimeSpan.FromHours(7)).DateTime);

        var record = await context.AttendanceRecords
            .Include(a => a.CheckInLocation)
            .FirstAsync(a => a.UserId == currentUser.UserId && a.WorkDate == workDate, cancellationToken);

        var activeLocations = await context.Locations.Where(l => l.IsActive).ToListAsync(cancellationToken);
        var matchedLocation = activeLocations.FirstOrDefault(l =>
            GeoDistance.CalculateMeters(request.Latitude, request.Longitude, l.Latitude, l.Longitude) <= l.CheckInRadiusMeters);

        record.CheckOutAtUtc = now;
        record.CheckOutLatitude = request.Latitude;
        record.CheckOutLongitude = request.Longitude;
        record.CheckOutLocationId = matchedLocation?.Id;
        record.UpdatedAtUtc = now;
        record.UpdatedByUserId = currentUser.UserId;

        await context.SaveChangesAsync(cancellationToken);

        return new AttendanceRecordDto(
            record.Id, record.WorkDate, record.CheckInAtUtc, record.CheckInLocation?.Name,
            record.CheckOutAtUtc, matchedLocation?.Name);
    }
}
