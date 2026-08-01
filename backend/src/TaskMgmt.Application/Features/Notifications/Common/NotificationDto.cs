using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.Notifications.Common;

public record NotificationDto(
    Guid Id,
    string Title,
    string Body,
    string Type,
    Guid? WorkTaskId,
    bool IsRead,
    DateTimeOffset? ReadAtUtc,
    DateTimeOffset CreatedAtUtc)
{
    public static NotificationDto FromEntity(Notification notification) => new(
        notification.Id,
        notification.Title,
        notification.Body,
        notification.Type,
        notification.WorkTaskId,
        notification.IsRead,
        notification.ReadAtUtc,
        notification.CreatedAtUtc);
}
