using MediatR;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Notifications.Common;
using TaskMgmt.Domain.Events;

namespace TaskMgmt.Application.Features.Notifications.EventHandlers;

public class CommentAddedNotificationHandler(IApplicationDbContext context, IBackgroundJobScheduler jobScheduler, ICacheService cache)
    : INotificationHandler<CommentAddedEvent>
{
    public async Task Handle(CommentAddedEvent notification, CancellationToken cancellationToken)
    {
        var (task, recipients) = await TaskNotificationHelper.GetTaskAndOtherAssigneesAsync(
            context, notification.WorkTaskId, notification.ActorUserId, cancellationToken);
        if (task is null)
        {
            return;
        }

        foreach (var userId in recipients)
        {
            await TaskNotificationHelper.NotifyAsync(
                context, jobScheduler, cache, userId,
                "Bình luận mới", $"Có bình luận mới trong \"{task.Title}\".",
                notification.WorkTaskId, "CommentAdded", notification.OccurredAtUtc, cancellationToken);
        }
    }
}
