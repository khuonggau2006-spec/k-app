using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TaskMgmt.API.Hubs;

// Client tự JoinTaskGroup(taskId) khi mở màn hình chi tiết 1 công việc để nhận cập nhật
// realtime cho đúng task đó; LeaveTaskGroup khi rời màn hình. Bất kỳ user đã đăng nhập nào
// cũng join được task bất kỳ - khớp với mô hình quyền hiện tại (WorkTask không có ACL riêng).
[Authorize]
public class NotificationHub : Hub
{
    public Task JoinTaskGroup(Guid taskId) => Groups.AddToGroupAsync(Context.ConnectionId, TaskGroupName(taskId));

    public Task LeaveTaskGroup(Guid taskId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, TaskGroupName(taskId));

    public static string TaskGroupName(Guid taskId) => $"task:{taskId}";
}
