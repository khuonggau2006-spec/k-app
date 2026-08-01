using Hangfire.Dashboard;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.API.Authorization;

// Dashboard chỉ dùng đúng JWT hiện có của hệ thống (không thêm cookie/basic auth riêng) -
// HttpContext.User đã được JwtBearer middleware điền sẵn nếu request mang Bearer token hợp lệ
// (qua header hoặc query string ?access_token=, xem Program.cs). Chỉ Admin mới xem được.
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.IsInRole(SystemRole.Admin.ToString());
    }
}
