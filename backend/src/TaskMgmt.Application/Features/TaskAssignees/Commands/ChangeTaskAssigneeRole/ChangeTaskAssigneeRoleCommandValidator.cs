using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.TaskAssignees.Common;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.Features.TaskAssignees.Commands.ChangeTaskAssigneeRole;

public class ChangeTaskAssigneeRoleCommandValidator : AbstractValidator<ChangeTaskAssigneeRoleCommand>
{
    public ChangeTaskAssigneeRoleCommandValidator(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        RuleFor(x => x.WorkTaskId)
            .MustAsync(async (id, cancellationToken) =>
            {
                await TaskAssigneeAuthorization.EnsureCanManageAsync(context, currentUser, id, cancellationToken);
                return true;
            });

        RuleFor(x => x)
            .MustAsync(async (x, cancellationToken) =>
                await context.TaskAssignees.AnyAsync(
                    a => a.WorkTaskId == x.WorkTaskId && a.UserId == x.UserId, cancellationToken))
            .WithMessage("Người dùng chưa được gán vào công việc này.")
            .WithName("UserId");

        RuleFor(x => x)
            .MustAsync(async (x, cancellationToken) =>
                x.Role != TaskAssigneeRole.Owner ||
                !await context.TaskAssignees.AnyAsync(
                    a => a.WorkTaskId == x.WorkTaskId && a.Role == TaskAssigneeRole.Owner && a.UserId != x.UserId,
                    cancellationToken))
            .WithMessage("Công việc đã có Owner khác.")
            .WithName("Role");
    }
}
