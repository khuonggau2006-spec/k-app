using TaskMgmt.Domain.Entities;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.Features.WorkTasks.Common;

public record WorkTaskDto(
    Guid Id,
    string Title,
    string? Description,
    WorkTaskStatus Status,
    DateTimeOffset? DueDateUtc,
    bool IsActive,
    Guid? ParentTaskId,
    Guid? LocationId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc)
{
    public static WorkTaskDto FromEntity(WorkTask task) => new(
        task.Id,
        task.Title,
        task.Description,
        task.Status,
        task.DueDateUtc,
        task.IsActive,
        task.ParentTaskId,
        task.LocationId,
        task.CreatedAtUtc,
        task.UpdatedAtUtc);
}
