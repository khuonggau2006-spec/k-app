using MediatR;

namespace TaskMgmt.Application.Features.TaskAssignees.Commands.RemoveTaskAssignee;

public record RemoveTaskAssigneeCommand(Guid WorkTaskId, Guid UserId) : IRequest;
