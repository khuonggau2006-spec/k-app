using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.WorkTasks.Common;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.WorkTasks.Commands.UpdateWorkTask;

public class UpdateWorkTaskCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<UpdateWorkTaskCommand, WorkTaskDto>
{
    public async Task<WorkTaskDto> Handle(UpdateWorkTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await context.WorkTasks
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(WorkTask), request.Id);

        task.Title = request.Title;
        task.Description = request.Description;
        task.Status = request.Status;
        task.DueDateUtc = request.DueDateUtc;
        task.ParentTaskId = request.ParentTaskId;
        task.LocationId = request.LocationId;
        task.UpdatedAtUtc = DateTimeOffset.UtcNow;
        task.UpdatedByUserId = currentUser.UserId;

        await context.SaveChangesAsync(cancellationToken);

        return WorkTaskDto.FromEntity(task);
    }
}
