using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.TaskAssignees.Common;
using TaskMgmt.Domain.Entities;
using TaskMgmt.Domain.Events;

namespace TaskMgmt.Application.Features.TaskAssignees.Commands.AddTaskAssignee;

public class AddTaskAssigneeCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<AddTaskAssigneeCommand, TaskAssigneeDto>
{
    public async Task<TaskAssigneeDto> Handle(AddTaskAssigneeCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Users.FirstAsync(u => u.Id == request.UserId, cancellationToken);

        var assignee = new TaskAssignee
        {
            WorkTaskId = request.WorkTaskId,
            UserId = request.UserId,
            Role = request.Role,
            AssignedAtUtc = DateTimeOffset.UtcNow,
        };
        assignee.AddDomainEvent(new TaskAssigneeAddedEvent(
            request.WorkTaskId, request.UserId, request.Role, currentUser.UserId, assignee.AssignedAtUtc));

        context.TaskAssignees.Add(assignee);
        await context.SaveChangesAsync(cancellationToken);

        return new TaskAssigneeDto(assignee.Id, assignee.WorkTaskId, assignee.UserId, user.FullName, user.Email, assignee.Role, assignee.AssignedAtUtc);
    }
}
