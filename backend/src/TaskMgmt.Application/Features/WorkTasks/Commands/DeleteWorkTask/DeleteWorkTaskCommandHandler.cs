using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Caching;
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Domain.Entities;
using TaskMgmt.Domain.Events;

namespace TaskMgmt.Application.Features.WorkTasks.Commands.DeleteWorkTask;

public class DeleteWorkTaskCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, ICacheService cache)
    : IRequestHandler<DeleteWorkTaskCommand>
{
    public async Task Handle(DeleteWorkTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await context.WorkTasks
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(WorkTask), request.Id);

        var now = DateTimeOffset.UtcNow;
        task.IsActive = false;
        task.UpdatedAtUtc = now;
        task.UpdatedByUserId = currentUser.UserId;
        task.AddDomainEvent(new WorkTaskDeletedEvent(task.Id, currentUser.UserId, now));

        await context.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync(CacheKeys.WorkTaskDetail(task.Id), cancellationToken);
        await cache.RemoveByPrefixAsync(CacheKeys.WorkTaskListPrefix, cancellationToken);
    }
}
