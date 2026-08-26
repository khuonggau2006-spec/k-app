using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.TaskAssignees.Common;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.TaskAssignees.Queries.GetTaskAssignees;

public class GetTaskAssigneesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetTaskAssigneesQuery, List<TaskAssigneeDto>>
{
    public async Task<List<TaskAssigneeDto>> Handle(GetTaskAssigneesQuery request, CancellationToken cancellationToken)
    {
        var taskExists = await context.WorkTasks.AnyAsync(t => t.Id == request.WorkTaskId, cancellationToken);
        if (!taskExists)
        {
            throw new NotFoundException(nameof(WorkTask), request.WorkTaskId);
        }

        return await context.TaskAssignees
            .Where(a => a.WorkTaskId == request.WorkTaskId)
            .OrderBy(a => a.Role)
            .Select(a => new TaskAssigneeDto(
                a.Id, a.WorkTaskId, a.UserId, a.User!.FullName, a.User!.Email, a.User!.AvatarStorageKey != null, a.Role, a.AssignedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
