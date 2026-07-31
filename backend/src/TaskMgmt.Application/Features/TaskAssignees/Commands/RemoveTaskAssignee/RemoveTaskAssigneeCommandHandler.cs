using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.TaskAssignees.Common;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.TaskAssignees.Commands.RemoveTaskAssignee;

public class RemoveTaskAssigneeCommandHandler(IApplicationDbContext context)
    : IRequestHandler<RemoveTaskAssigneeCommand>
{
    public async Task Handle(RemoveTaskAssigneeCommand request, CancellationToken cancellationToken)
    {
        var assignee = await context.TaskAssignees
            .FirstOrDefaultAsync(a => a.WorkTaskId == request.WorkTaskId && a.UserId == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(TaskAssignee), $"{request.WorkTaskId}/{request.UserId}");

        context.TaskAssignees.Remove(assignee);
        await context.SaveChangesAsync(cancellationToken);
    }
}
