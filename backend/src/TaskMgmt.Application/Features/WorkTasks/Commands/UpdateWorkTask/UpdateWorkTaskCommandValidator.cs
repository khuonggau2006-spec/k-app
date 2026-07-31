using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.WorkTasks.Common;

namespace TaskMgmt.Application.Features.WorkTasks.Commands.UpdateWorkTask;

public class UpdateWorkTaskCommandValidator : AbstractValidator<UpdateWorkTaskCommand>
{
    public UpdateWorkTaskCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(4000);

        RuleFor(x => x.LocationId)
            .MustAsync(async (id, cancellationToken) =>
                id is null || await context.Locations.AnyAsync(l => l.Id == id, cancellationToken))
            .WithMessage("Vị trí không tồn tại.");

        RuleFor(x => x)
            .Must(x => x.ParentTaskId != x.Id)
            .WithMessage("Một công việc không thể là công việc cha của chính nó.")
            .WithName("ParentTaskId");

        RuleFor(x => x.ParentTaskId)
            .MustAsync(async (id, cancellationToken) =>
                id is null || await context.WorkTasks.AnyAsync(t => t.Id == id, cancellationToken))
            .WithMessage("Công việc cha không tồn tại.");

        RuleFor(x => x)
            .MustAsync(async (x, cancellationToken) =>
            {
                if (x.ParentTaskId is null)
                {
                    return true;
                }

                return !await WorkTaskHierarchy.WouldCreateCycleAsync(context, x.Id, x.ParentTaskId.Value, cancellationToken);
            })
            .WithMessage("Không thể chuyển công việc vào chính công việc con của nó.")
            .WithName("ParentTaskId");

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
