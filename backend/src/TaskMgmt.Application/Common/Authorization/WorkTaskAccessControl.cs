using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.Common.Authorization;

// Quy tắc quyền dùng chung cho mọi thao tác thay đổi 1 WorkTask cụ thể (sửa/xoá task, xoá
// attachment...): hệ thống Manager/Admin luôn được phép; Member chỉ được phép nếu là Owner
// (TaskAssigneeRole.Owner) của chính task đó - không được đụng vào task người khác quản lý.
internal static class WorkTaskAccessControl
{
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
            throw new ForbiddenException("Chỉ Owner của công việc hoặc Admin/Manager mới có quyền thực hiện thao tác này.");
        }
    }
}
