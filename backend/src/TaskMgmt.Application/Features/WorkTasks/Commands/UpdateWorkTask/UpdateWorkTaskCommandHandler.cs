using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Authorization;
using TaskMgmt.Application.Common.Caching;
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.WorkTasks.Common;
using TaskMgmt.Domain.Entities;
using TaskMgmt.Domain.Events;

namespace TaskMgmt.Application.Features.WorkTasks.Commands.UpdateWorkTask;

public class UpdateWorkTaskCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, ICacheService cache)
    : IRequestHandler<UpdateWorkTaskCommand, WorkTaskDto>
{
    public async Task<WorkTaskDto> Handle(UpdateWorkTaskCommand request, CancellationToken cancellationToken)
    {
        await WorkTaskAccessControl.EnsureCanManageAsync(context, currentUser, request.Id, cancellationToken);

        var task = await context.WorkTasks
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(WorkTask), request.Id);

        var now = DateTimeOffset.UtcNow;

        // So sánh giá trị cũ/mới TRƯỚC khi gán để phát đúng domain event field-changed.
        RaiseFieldChangedIfDifferent(task, nameof(WorkTask.Title), task.Title, request.Title, now);
        RaiseFieldChangedIfDifferent(task, nameof(WorkTask.Description), task.Description, request.Description, now);
        RaiseFieldChangedIfDifferent(task, nameof(WorkTask.DueDateUtc), task.DueDateUtc?.ToString("O"), request.DueDateUtc?.ToString("O"), now);
        RaiseFieldChangedIfDifferent(task, nameof(WorkTask.LocationId), task.LocationId?.ToString(), request.LocationId?.ToString(), now);
        RaiseFieldChangedIfDifferent(task, nameof(WorkTask.ParentTaskId), task.ParentTaskId?.ToString(), request.ParentTaskId?.ToString(), now);

        if (task.Status != request.Status)
        {
            task.AddDomainEvent(new WorkTaskStatusChangedEvent(task.Id, task.Status, request.Status, currentUser.UserId, now));
        }

        // Hạn đổi thì "đã nhắc hay chưa" không còn ý nghĩa với hạn mới - reset để job nhắc hạn
        // (SendDueSoonReminderJob/SendOverdueReminderJob) có thể nhắc lại đúng theo hạn mới.
        if (task.DueDateUtc != request.DueDateUtc)
        {
            task.DueSoonReminderSentAtUtc = null;
            task.OverdueReminderSentAtUtc = null;
        }

        task.Title = request.Title;
        task.Description = request.Description;
        task.Status = request.Status;
        task.DueDateUtc = request.DueDateUtc;
        task.ParentTaskId = request.ParentTaskId;
        task.LocationId = request.LocationId;
        task.UpdatedAtUtc = now;
        task.UpdatedByUserId = currentUser.UserId;

        await context.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync(CacheKeys.WorkTaskDetail(task.Id), cancellationToken);
        await cache.RemoveByPrefixAsync(CacheKeys.WorkTaskListPrefix, cancellationToken);
        await cache.RemoveByPrefixAsync(CacheKeys.DashboardStatsPrefix, cancellationToken);

        return WorkTaskDto.FromEntity(task);
    }

    private void RaiseFieldChangedIfDifferent(WorkTask task, string fieldName, string? oldValue, string? newValue, DateTimeOffset now)
    {
        if (oldValue == newValue)
        {
            return;
        }

        task.AddDomainEvent(new WorkTaskFieldChangedEvent(task.Id, fieldName, oldValue, newValue, currentUser.UserId, now));
    }
}
