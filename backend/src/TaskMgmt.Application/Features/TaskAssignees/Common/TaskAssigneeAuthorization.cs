using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.Features.TaskAssignees.Common;

internal static class TaskAssigneeAuthorization
{
    // Allowed if the caller has an elevated system role, or is the Owner of this specific task.
    public static async Task EnsureCanManageAsync(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        Guid workTaskId,
        CancellationToken cancellationToken)
    {
        if (currentUser.Role is SystemRole.Admin or SystemRole.Manager)
        {
            return;
        }

        var isOwner = currentUser.UserId is not null && await context.TaskAssignees.AnyAsync(
            a => a.WorkTaskId == workTaskId && a.UserId == currentUser.UserId && a.Role == TaskAssigneeRole.Owner,
            cancellationToken);

        if (!isOwner)
        {
            throw new ForbiddenException("Chỉ Owner của công việc hoặc Admin/Manager mới có quyền quản lý người tham gia.");
        }
    }
}
