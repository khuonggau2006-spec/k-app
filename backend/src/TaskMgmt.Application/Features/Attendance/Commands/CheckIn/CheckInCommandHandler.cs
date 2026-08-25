using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common;
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Attendance.Common;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.Attendance.Commands.CheckIn;

public class CheckInCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<CheckInCommand, AttendanceRecordDto>
{
    public async Task<AttendanceRecordDto> Handle(CheckInCommand request, CancellationToken cancellationToken)
    {
        var activeLocations = await context.Locations
            .Where(l => l.IsActive)
            .ToListAsync(cancellationToken);

        var matchedLocation = activeLocations.FirstOrDefault(l =>
            GeoDistance.CalculateMeters(request.Latitude, request.Longitude, l.Latitude, l.Longitude) <= l.CheckInRadiusMeters);

        if (matchedLocation is null)
        {
            throw new ValidationException(
                [new ValidationFailure(nameof(CheckInCommand.Latitude), "Ngoài phạm vi cho phép của mọi vị trí đã đăng ký.")]);
        }

        var now = DateTimeOffset.UtcNow;
        var workDate = DateOnly.FromDateTime(now.ToOffset(TimeSpan.FromHours(7)).DateTime);

        var record = new AttendanceRecord
        {
            UserId = currentUser.UserId!.Value,
            WorkDate = workDate,
            CheckInAtUtc = now,
            CheckInLatitude = request.Latitude,
            CheckInLongitude = request.Longitude,
            CheckInLocationId = matchedLocation.Id,
            CreatedAtUtc = now,
            CreatedByUserId = currentUser.UserId,
        };

        context.AttendanceRecords.Add(record);
        await context.SaveChangesAsync(cancellationToken);

        return new AttendanceRecordDto(record.Id, record.WorkDate, record.CheckInAtUtc, matchedLocation.Name, null, null);
    }
}
