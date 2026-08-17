using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.Common.Caching;

// Gom key + TTL vào 1 chỗ để phía đọc (query handler) và phía ghi (command handler invalidate)
// luôn khớp nhau, tránh lệch key rải rác nhiều file.
internal static class CacheKeys
{
    public const string WorkTaskListPrefix = "worktasks:list:";
    public static readonly TimeSpan WorkTaskListExpiration = TimeSpan.FromSeconds(60);

    public static readonly TimeSpan WorkTaskDetailExpiration = TimeSpan.FromMinutes(5);

    public const string LocationListKey = "locations:list";
    public static readonly TimeSpan LocationListExpiration = TimeSpan.FromMinutes(10);

    public static readonly TimeSpan LocationDetailExpiration = TimeSpan.FromMinutes(10);

    public static readonly TimeSpan UnreadNotificationCountExpiration = TimeSpan.FromSeconds(30);

    public static readonly TimeSpan DisabledNotificationTypesExpiration = TimeSpan.FromMinutes(5);

    public const string DashboardStatsPrefix = "dashboard:stats:";
    public static readonly TimeSpan DashboardStatsExpiration = TimeSpan.FromSeconds(60);

    public static string WorkTaskList(
        WorkTaskStatus? status, Guid? locationId, Guid? parentTaskId,
        int pageNumber, int pageSize, string? sortBy, bool sortDescending) =>
        $"{WorkTaskListPrefix}{status}:{locationId}:{parentTaskId}:{pageNumber}:{pageSize}:{sortBy}:{sortDescending}";

    public static string WorkTaskDetail(Guid id) => $"worktasks:detail:{id}";

    public static string LocationDetail(Guid id) => $"locations:detail:{id}";

    public static string UnreadNotificationCount(Guid userId) => $"notifications:unreadcount:{userId}";

    public static string DisabledNotificationTypes(Guid userId) => $"notifications:disabledtypes:{userId}";

    public static string DashboardStats(Guid? locationId) => $"{DashboardStatsPrefix}{locationId}";
}
