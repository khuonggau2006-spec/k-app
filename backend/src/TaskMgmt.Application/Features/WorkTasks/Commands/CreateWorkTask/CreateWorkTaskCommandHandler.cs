using MediatR;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.WorkTasks.Common;
using TaskMgmt.Domain.Entities;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.Features.WorkTasks.Commands.CreateWorkTask;

public class CreateWorkTaskCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<CreateWorkTaskCommand, WorkTaskDto>
{
    public async Task<WorkTaskDto> Handle(CreateWorkTaskCommand request, CancellationToken cancellationToken)
    {
        var task = new WorkTask
        {
            Title = request.Title,
            Description = request.Description,
            Status = WorkTaskStatus.ToDo,
            DueDateUtc = request.DueDateUtc,
            ParentTaskId = request.ParentTaskId,
            LocationId = request.LocationId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedByUserId = currentUser.UserId,
        };

        context.WorkTasks.Add(task);

        if (currentUser.UserId is { } userId)
        {
            context.TaskAssignees.Add(new TaskAssignee
            {
                WorkTaskId = task.Id,
                UserId = userId,
                Role = TaskAssigneeRole.Owner,
                AssignedAtUtc = DateTimeOffset.UtcNow,
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        return WorkTaskDto.FromEntity(task);
    }
}
