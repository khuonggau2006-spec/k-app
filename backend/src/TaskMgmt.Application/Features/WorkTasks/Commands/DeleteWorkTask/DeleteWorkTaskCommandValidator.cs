using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;

namespace TaskMgmt.Application.Features.WorkTasks.Commands.DeleteWorkTask;

public class DeleteWorkTaskCommandValidator : AbstractValidator<DeleteWorkTaskCommand>
{
    public DeleteWorkTaskCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.Id)
            .MustAsync(async (id, cancellationToken) =>
                !await context.WorkTasks.AnyAsync(t => t.ParentTaskId == id && t.IsActive, cancellationToken))
            .WithMessage("Không thể xoá công việc đang có công việc con.");
    }
}
