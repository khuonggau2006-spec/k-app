using Microsoft.EntityFrameworkCore;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Location> Locations { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<WorkTask> WorkTasks { get; }
    DbSet<TaskAssignee> TaskAssignees { get; }
    DbSet<Comment> Comments { get; }
    DbSet<CommentMention> CommentMentions { get; }
    DbSet<Attachment> Attachments { get; }
    DbSet<TaskHistory> TaskHistories { get; }
    DbSet<DeviceToken> DeviceTokens { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<NotificationPreference> NotificationPreferences { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
