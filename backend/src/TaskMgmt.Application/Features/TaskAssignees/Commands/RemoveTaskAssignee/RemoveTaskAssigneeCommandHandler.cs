using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.TaskAssignees.Common;
using TaskMgmt.Domain.Entities;
using TaskMgmt.Domain.Events;

namespace TaskMgmt.Application.Features.TaskAssignees.Commands.RemoveTaskAssignee;

public class RemoveTaskAssigneeCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<RemoveTaskAssigneeCommand>
{
    public async Task Handle(RemoveTaskAssigneeCommand request, CancellationToken cancellationToken)
    {
        // Tự rời khỏi task thì không cần là Owner - chỉ cần là Owner/Manager/Admin khi
        // gỡ MỘT NGƯỜI KHÁC ra khỏi task.
        if (currentUser.UserId != request.UserId)
        {
            await TaskAssigneeAuthorization.EnsureCanManageAsync(context, currentUser, request.WorkTaskId, cancellationToken);
        }

        var assignee = await context.TaskAssignees
            .FirstOrDefaultAsync(a => a.WorkTaskId == request.WorkTaskId && a.UserId == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(TaskAssignee), $"{request.WorkTaskId}/{request.UserId}");

        assignee.AddDomainEvent(new TaskAssigneeRemovedEvent(
            request.WorkTaskId, request.UserId, currentUser.UserId, DateTimeOffset.UtcNow));

        context.TaskAssignees.Remove(assignee);
        await context.SaveChangesAsync(cancellationToken);
    }
}
