using MediatR;
using TaskMgmt.Application.Features.WorkTasks.Common;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.Features.WorkTasks.Commands.UpdateWorkTask;

public record UpdateWorkTaskCommand(
    Guid Id,
    string Title,
    string? Description,
    WorkTaskStatus Status,
    DateTimeOffset? DueDateUtc,
    Guid? ParentTaskId,
    Guid? LocationId) : IRequest<WorkTaskDto>;
