using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.TaskAssignees.Common;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.TaskAssignees.Commands.ChangeTaskAssigneeRole;

public class ChangeTaskAssigneeRoleCommandHandler(IApplicationDbContext context)
    : IRequestHandler<ChangeTaskAssigneeRoleCommand, TaskAssigneeDto>
{
    public async Task<TaskAssigneeDto> Handle(ChangeTaskAssigneeRoleCommand request, CancellationToken cancellationToken)
    {
        var assignee = await context.TaskAssignees
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.WorkTaskId == request.WorkTaskId && a.UserId == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(TaskAssignee), $"{request.WorkTaskId}/{request.UserId}");

        assignee.Role = request.Role;
        await context.SaveChangesAsync(cancellationToken);

        return new TaskAssigneeDto(
            assignee.Id, assignee.WorkTaskId, assignee.UserId, assignee.User!.FullName, assignee.User!.Email, assignee.Role, assignee.AssignedAtUtc);
    }
}
