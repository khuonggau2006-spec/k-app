using MediatR;
using TaskMgmt.Application.Features.TaskAssignees.Common;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.Features.TaskAssignees.Commands.ChangeTaskAssigneeRole;

public record ChangeTaskAssigneeRoleCommand(Guid WorkTaskId, Guid UserId, TaskAssigneeRole Role) : IRequest<TaskAssigneeDto>;
