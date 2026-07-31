using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.WorkTasks.Commands.DeleteWorkTask;

public class DeleteWorkTaskCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<DeleteWorkTaskCommand>
{
    public async Task Handle(DeleteWorkTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await context.WorkTasks
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(WorkTask), request.Id);

        task.IsActive = false;
        task.UpdatedAtUtc = DateTimeOffset.UtcNow;
        task.UpdatedByUserId = currentUser.UserId;

        await context.SaveChangesAsync(cancellationToken);
    }
}
