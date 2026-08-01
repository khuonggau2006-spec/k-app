using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.Common.Interfaces;

// Trừu tượng hoá SignalR để Application không phụ thuộc trực tiếp vào ASP.NET Core hosting -
// implementation thật (dùng IHubContext) đặt ở tầng API, giống cách ICurrentUserService làm.
public interface IRealtimeNotifier
{
    Task NotifyTaskUpdatedAsync(
        Guid workTaskId, TaskHistoryActionType actionType, DateTimeOffset occurredAtUtc, CancellationToken cancellationToken);
}
