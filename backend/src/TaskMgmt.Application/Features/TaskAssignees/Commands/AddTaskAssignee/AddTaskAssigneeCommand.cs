using MediatR;
using TaskMgmt.Application.Features.TaskAssignees.Common;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.Features.TaskAssignees.Commands.AddTaskAssignee;

public record AddTaskAssigneeCommand(Guid WorkTaskId, Guid UserId, TaskAssigneeRole Role) : IRequest<TaskAssigneeDto>;
