using TaskMgmt.Domain.Entities;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.Features.TaskHistories.Common;

public record TaskHistoryDto(
    Guid Id,
    Guid WorkTaskId,
    TaskHistoryActionType ActionType,
    string Description,
    string? FieldName,
    string? OldValue,
    string? NewValue,
    Guid? ActorUserId,
    string ActorFullName,
    string ActorEmail,
    Guid? TargetUserId,
    string? TargetUserFullName,
    string? TargetUserEmail,
    DateTimeOffset CreatedAtUtc)
{
    // Yêu cầu TaskHistory đã được load kèm Actor và Target (Include).
    public static TaskHistoryDto FromEntity(TaskHistory history) => new(
        history.Id,
        history.WorkTaskId,
        history.ActionType,
        history.Description,
        history.FieldName,
        history.OldValue,
        history.NewValue,
        history.ActorUserId,
        history.Actor?.FullName ?? string.Empty,
        history.Actor?.Email ?? string.Empty,
        history.TargetUserId,
        history.Target?.FullName,
        history.Target?.Email,
        history.CreatedAtUtc);
}
