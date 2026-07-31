using MediatR;
using TaskMgmt.Application.Features.WorkTasks.Common;

namespace TaskMgmt.Application.Features.WorkTasks.Commands.CreateWorkTask;

public record CreateWorkTaskCommand(
    string Title,
    string? Description,
    DateTimeOffset? DueDateUtc,
    Guid? ParentTaskId,
    Guid? LocationId) : IRequest<WorkTaskDto>;
