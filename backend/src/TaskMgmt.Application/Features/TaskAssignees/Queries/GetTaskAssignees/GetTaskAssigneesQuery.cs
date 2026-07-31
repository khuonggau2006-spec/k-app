using MediatR;
using TaskMgmt.Application.Features.TaskAssignees.Common;

namespace TaskMgmt.Application.Features.TaskAssignees.Queries.GetTaskAssignees;

public record GetTaskAssigneesQuery(Guid WorkTaskId) : IRequest<List<TaskAssigneeDto>>;
