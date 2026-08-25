using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;

namespace TaskMgmt.Application.Features.Attendance.Commands.CheckIn;

public class CheckInCommandValidator : AbstractValidator<CheckInCommand>
{
    public CheckInCommandValidator(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);

        RuleFor(x => x)
            .MustAsync(async (_, cancellationToken) =>
            {
                var workDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
                var alreadyCheckedIn = await context.AttendanceRecords.AnyAsync(
                    a => a.UserId == currentUser.UserId && a.WorkDate == workDate && a.CheckInAtUtc != null,
                    cancellationToken);
                return !alreadyCheckedIn;
            })
            .WithMessage("Bạn đã check-in hôm nay rồi.")
            .WithName("CheckIn");
    }
}
