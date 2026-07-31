using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.TaskAssignees.Common;

namespace TaskMgmt.Application.Features.TaskAssignees.Commands.RemoveTaskAssignee;

public class RemoveTaskAssigneeCommandValidator : AbstractValidator<RemoveTaskAssigneeCommand>
{
    public RemoveTaskAssigneeCommandValidator(IApplicationDbContext context, ICurrentUserService currentUser)
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
    }
}
