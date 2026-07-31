using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;

namespace TaskMgmt.Application.Features.Locations.Commands.UpdateLocation;

public class UpdateLocationCommandValidator : AbstractValidator<UpdateLocationCommand>
{
    public UpdateLocationCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Address)
            .MaximumLength(500);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180);

        RuleFor(x => x)
            .Must(x => x.ParentLocationId != x.Id)
            .WithMessage("Một vị trí không thể là vị trí cha của chính nó.")
            .WithName("ParentLocationId");

        RuleFor(x => x.ParentLocationId)
            .MustAsync(async (id, cancellationToken) =>
                id is null || await context.Locations.AnyAsync(l => l.Id == id, cancellationToken))
            .WithMessage("Vị trí cha không tồn tại.");
    }
}
