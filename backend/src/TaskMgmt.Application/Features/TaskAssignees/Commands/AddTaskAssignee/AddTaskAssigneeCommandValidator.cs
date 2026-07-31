using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.TaskAssignees.Common;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.Features.TaskAssignees.Commands.AddTaskAssignee;

public class AddTaskAssigneeCommandValidator : AbstractValidator<AddTaskAssigneeCommand>
{
    public AddTaskAssigneeCommandValidator(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        // Authorization must run before any other rule so an unauthorized caller
        // gets a 403 instead of a 400 that leaks the task's current assignment state.
        RuleFor(x => x.WorkTaskId)
            .MustAsync(async (id, cancellationToken) =>
            {
                await TaskAssigneeAuthorization.EnsureCanManageAsync(context, currentUser, id, cancellationToken);
                return true;
            });

        RuleFor(x => x.WorkTaskId)
            .MustAsync(async (id, cancellationToken) =>
                await context.WorkTasks.AnyAsync(t => t.Id == id && t.IsActive, cancellationToken))
            .WithMessage("Công việc không tồn tại.");

        RuleFor(x => x.UserId)
            .MustAsync(async (id, cancellationToken) =>
                await context.Users.AnyAsync(u => u.Id == id && u.IsActive, cancellationToken))
            .WithMessage("Người dùng không tồn tại.");

        RuleFor(x => x)
            .MustAsync(async (x, cancellationToken) =>
                !await context.TaskAssignees.AnyAsync(
                    a => a.WorkTaskId == x.WorkTaskId && a.UserId == x.UserId, cancellationToken))
            .WithMessage("Người dùng đã được gán vào công việc này.")
            .WithName("UserId");

        RuleFor(x => x)
            .MustAsync(async (x, cancellationToken) =>
                x.Role != TaskAssigneeRole.Owner ||
                !await context.TaskAssignees.AnyAsync(
                    a => a.WorkTaskId == x.WorkTaskId && a.Role == TaskAssigneeRole.Owner, cancellationToken))
            .WithMessage("Công việc đã có Owner.")
            .WithName("Role");
    }
}
