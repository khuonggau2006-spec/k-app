using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;

namespace TaskMgmt.Application.Features.Attendance.Commands.CheckOut;

public class CheckOutCommandValidator : AbstractValidator<CheckOutCommand>
{
    public CheckOutCommandValidator(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);

        RuleFor(x => x)
            .MustAsync(async (_, cancellationToken) =>
            {
                var workDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
                return await context.AttendanceRecords.AnyAsync(
                    a => a.UserId == currentUser.UserId && a.WorkDate == workDate
                         && a.CheckInAtUtc != null && a.CheckOutAtUtc == null,
                    cancellationToken);
            })
            .WithMessage("Bạn chưa check-in hôm nay.")
            .WithName("CheckOut");
    }
}
