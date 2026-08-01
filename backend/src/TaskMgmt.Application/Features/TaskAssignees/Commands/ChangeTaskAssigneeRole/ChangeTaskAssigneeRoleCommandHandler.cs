using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.TaskAssignees.Common;
using TaskMgmt.Domain.Entities;
using TaskMgmt.Domain.Events;

namespace TaskMgmt.Application.Features.TaskAssignees.Commands.ChangeTaskAssigneeRole;

public class ChangeTaskAssigneeRoleCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<ChangeTaskAssigneeRoleCommand, TaskAssigneeDto>
{
    public async Task<TaskAssigneeDto> Handle(ChangeTaskAssigneeRoleCommand request, CancellationToken cancellationToken)
    {
        var assignee = await context.TaskAssignees
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.WorkTaskId == request.WorkTaskId && a.UserId == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(TaskAssignee), $"{request.WorkTaskId}/{request.UserId}");

        var oldRole = assignee.Role;
        assignee.Role = request.Role;

        if (oldRole != request.Role)
        {
            assignee.AddDomainEvent(new TaskAssigneeRoleChangedEvent(
                request.WorkTaskId, request.UserId, oldRole, request.Role, currentUser.UserId, DateTimeOffset.UtcNow));
        }

        await context.SaveChangesAsync(cancellationToken);

        return new TaskAssigneeDto(
            assignee.Id, assignee.WorkTaskId, assignee.UserId, assignee.User!.FullName, assignee.User!.Email, assignee.Role, assignee.AssignedAtUtc);
    }
}
