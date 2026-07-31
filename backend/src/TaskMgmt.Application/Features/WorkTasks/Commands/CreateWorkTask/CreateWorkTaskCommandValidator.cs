using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.WorkTasks.Common;

namespace TaskMgmt.Application.Features.WorkTasks.Commands.CreateWorkTask;

public class CreateWorkTaskCommandValidator : AbstractValidator<CreateWorkTaskCommand>
{
    public CreateWorkTaskCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(4000);

        RuleFor(x => x.LocationId)
            .MustAsync(async (id, cancellationToken) =>
                id is null || await context.Locations.AnyAsync(l => l.Id == id, cancellationToken))
            .WithMessage("Vị trí không tồn tại.");

        RuleFor(x => x.ParentTaskId)
            .MustAsync(async (id, cancellationToken) =>
                id is null || await context.WorkTasks.AnyAsync(t => t.Id == id, cancellationToken))
            .WithMessage("Công việc cha không tồn tại.");

        RuleFor(x => x.ParentTaskId)
            .MustAsync(async (id, cancellationToken) =>
            {
                if (id is null)
                {
                    return true;
                }

                var parentLevel = await WorkTaskHierarchy.GetLevelAsync(context, id.Value, cancellationToken);
                return !WorkTaskHierarchy.ExceedsMaxDepth(parentLevel);
            })
            .WithMessage($"Vượt quá giới hạn {WorkTaskHierarchy.MaxLevel} cấp công việc con.");
    }
}
