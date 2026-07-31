using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.TaskAssignees.Common;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.TaskAssignees.Commands.AddTaskAssignee;

public class AddTaskAssigneeCommandHandler(IApplicationDbContext context)
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

        context.TaskAssignees.Add(assignee);
        await context.SaveChangesAsync(cancellationToken);

        return new TaskAssigneeDto(assignee.Id, assignee.WorkTaskId, assignee.UserId, user.FullName, user.Email, assignee.Role, assignee.AssignedAtUtc);
    }
}
