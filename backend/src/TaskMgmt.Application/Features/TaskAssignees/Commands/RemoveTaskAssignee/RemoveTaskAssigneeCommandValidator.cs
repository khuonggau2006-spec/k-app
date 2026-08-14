using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.TaskAssignees.Common;

namespace TaskMgmt.Application.Features.TaskAssignees.Commands.RemoveTaskAssignee;

public class RemoveTaskAssigneeCommandValidator : AbstractValidator<RemoveTaskAssigneeCommand>
{
    public RemoveTaskAssigneeCommandValidator(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        // Tự rời khỏi task thì không cần là Owner - chỉ cần là Owner/Manager/Admin khi
        // gỡ MỘT NGƯỜI KHÁC ra khỏi task.
        RuleFor(x => x)
            .MustAsync(async (x, cancellationToken) =>
            {
                if (currentUser.UserId != x.UserId)
                {
                    await TaskAssigneeAuthorization.EnsureCanManageAsync(context, currentUser, x.WorkTaskId, cancellationToken);
                }

                return true;
            })
            .WithName("WorkTaskId");

        RuleFor(x => x)
            .MustAsync(async (x, cancellationToken) =>
                await context.TaskAssignees.AnyAsync(
                    a => a.WorkTaskId == x.WorkTaskId && a.UserId == x.UserId, cancellationToken))
            .WithMessage("Người dùng chưa được gán vào công việc này.")
            .WithName("UserId");
    }
}
