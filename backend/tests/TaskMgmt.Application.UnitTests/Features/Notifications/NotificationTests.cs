using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Comments.Commands.CreateComment;
using TaskMgmt.Application.Features.Notifications.Commands.MarkAllNotificationsAsRead;
using TaskMgmt.Application.Features.Notifications.Commands.MarkNotificationAsRead;
using TaskMgmt.Application.Features.Notifications.Queries.GetNotifications;
using TaskMgmt.Application.Features.Notifications.Queries.GetUnreadNotificationCount;
using TaskMgmt.Application.Features.TaskAssignees.Commands.AddTaskAssignee;
using TaskMgmt.Application.Features.WorkTasks.Commands.CreateWorkTask;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Enums;
using TaskMgmt.Infrastructure.Persistence;

namespace TaskMgmt.Application.UnitTests.Features.Notifications;

// Test end-to-end (MediatR thật + DispatchDomainEventsInterceptor thật) để xác nhận domain event ->
// Notification EventHandler ghi đúng bản ghi Notification (song song với việc enqueue push đã test
// ở mục 3.5), và các API list/đếm chưa đọc/đánh dấu đã đọc hoạt động đúng, có cache-aside.
public class NotificationTests
{
    [Fact]
    public async Task CommentAdded_CreatesNotificationForOtherAssigneeOnly()
    {
        var actorId = Guid.NewGuid();
        await using var provider = TestServiceProviderFactory.Create(actorId, SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();
        var context = provider.GetRequiredService<AppDbContext>();

        var task = await sender.Send(new CreateWorkTaskCommand("Task with comment", null, null, null, null));
        var otherUser = TestDataFactory.CreateUser("other@example.com");
        context.Users.Add(otherUser);
        await context.SaveChangesAsync(default);
        await sender.Send(new AddTaskAssigneeCommand(task.Id, otherUser.Id, TaskAssigneeRole.Watcher));

        await sender.Send(new CreateCommentCommand(task.Id, "Bình luận test.", []));

        var notification = Assert.Single(context.Notifications, n => n.Type == "CommentAdded");
        Assert.Equal(otherUser.Id, notification.UserId);
        Assert.Equal(task.Id, notification.WorkTaskId);
        Assert.False(notification.IsRead);
        Assert.Null(notification.ReadAtUtc);
        Assert.DoesNotContain(context.Notifications, n => n.UserId == actorId);
    }

    [Fact]
    public async Task GetNotifications_ScopedToCurrentUser_NewestFirst()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        await using var provider = TestServiceProviderFactory.Create(userId, SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();
        var context = provider.GetRequiredService<AppDbContext>();

        context.Notifications.AddRange(
            new Domain.Entities.Notification { UserId = userId, Title = "First", Body = "b", Type = "T", CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-10) },
            new Domain.Entities.Notification { UserId = userId, Title = "Second", Body = "b", Type = "T", CreatedAtUtc = DateTimeOffset.UtcNow },
            new Domain.Entities.Notification { UserId = otherUserId, Title = "NotMine", Body = "b", Type = "T", CreatedAtUtc = DateTimeOffset.UtcNow });
        await context.SaveChangesAsync(default);

        var result = await sender.Send(new GetNotificationsQuery());

        Assert.Equal(2, result.TotalCount);
        Assert.Equal("Second", result.Items[0].Title);
        Assert.Equal("First", result.Items[1].Title);
    }

    [Fact]
    public async Task GetNotifications_UnreadOnly_FiltersReadOut()
    {
        var userId = Guid.NewGuid();
        await using var provider = TestServiceProviderFactory.Create(userId, SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();
        var context = provider.GetRequiredService<AppDbContext>();

        context.Notifications.AddRange(
            new Domain.Entities.Notification { UserId = userId, Title = "Read", Body = "b", Type = "T", IsRead = true, CreatedAtUtc = DateTimeOffset.UtcNow },
            new Domain.Entities.Notification { UserId = userId, Title = "Unread", Body = "b", Type = "T", IsRead = false, CreatedAtUtc = DateTimeOffset.UtcNow });
        await context.SaveChangesAsync(default);

        var result = await sender.Send(new GetNotificationsQuery(UnreadOnly: true));

        var item = Assert.Single(result.Items);
        Assert.Equal("Unread", item.Title);
    }

    [Fact]
    public async Task GetUnreadCount_CachesResult_MarkAsReadInvalidatesIt()
    {
        var userId = Guid.NewGuid();
        await using var provider = TestServiceProviderFactory.Create(userId, SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();
        var context = provider.GetRequiredService<AppDbContext>();

        var n1 = new Domain.Entities.Notification { UserId = userId, Title = "A", Body = "b", Type = "T", CreatedAtUtc = DateTimeOffset.UtcNow };
        var n2 = new Domain.Entities.Notification { UserId = userId, Title = "B", Body = "b", Type = "T", CreatedAtUtc = DateTimeOffset.UtcNow };
        context.Notifications.AddRange(n1, n2);
        await context.SaveChangesAsync(default);

        var firstCount = await sender.Send(new GetUnreadNotificationCountQuery());
        Assert.Equal(2, firstCount);

        // Đổi thẳng DB, KHÔNG qua command handler -> cache vẫn giữ giá trị cũ.
        var thirdUnread = new Domain.Entities.Notification { UserId = userId, Title = "C", Body = "b", Type = "T", CreatedAtUtc = DateTimeOffset.UtcNow };
        context.Notifications.Add(thirdUnread);
        await context.SaveChangesAsync(default);

        var staleCount = await sender.Send(new GetUnreadNotificationCountQuery());
        Assert.Equal(2, staleCount);

        await sender.Send(new MarkNotificationAsReadCommand(n1.Id));

        var freshCount = await sender.Send(new GetUnreadNotificationCountQuery());
        Assert.Equal(2, freshCount);
    }

    [Fact]
    public async Task MarkNotificationAsRead_OtherUsersNotification_ThrowsNotFound()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        await using var provider = TestServiceProviderFactory.Create(userId, SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();
        var context = provider.GetRequiredService<AppDbContext>();

        var notification = new Domain.Entities.Notification
        {
            UserId = otherUserId, Title = "A", Body = "b", Type = "T", CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        context.Notifications.Add(notification);
        await context.SaveChangesAsync(default);

        await Assert.ThrowsAsync<NotFoundException>(() => sender.Send(new MarkNotificationAsReadCommand(notification.Id)));
    }

    [Fact]
    public async Task MarkAllNotificationsAsRead_OnlyAffectsCurrentUser()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        await using var provider = TestServiceProviderFactory.Create(userId, SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();
        var context = provider.GetRequiredService<AppDbContext>();

        var mine1 = new Domain.Entities.Notification { UserId = userId, Title = "A", Body = "b", Type = "T", CreatedAtUtc = DateTimeOffset.UtcNow };
        var mine2 = new Domain.Entities.Notification { UserId = userId, Title = "B", Body = "b", Type = "T", CreatedAtUtc = DateTimeOffset.UtcNow };
        var notMine = new Domain.Entities.Notification { UserId = otherUserId, Title = "C", Body = "b", Type = "T", CreatedAtUtc = DateTimeOffset.UtcNow };
        context.Notifications.AddRange(mine1, mine2, notMine);
        await context.SaveChangesAsync(default);

        await sender.Send(new MarkAllNotificationsAsReadCommand());

        Assert.True(context.Notifications.Single(n => n.Id == mine1.Id).IsRead);
        Assert.True(context.Notifications.Single(n => n.Id == mine2.Id).IsRead);
        Assert.False(context.Notifications.Single(n => n.Id == notMine.Id).IsRead);

        var unreadCount = await sender.Send(new GetUnreadNotificationCountQuery());
        Assert.Equal(0, unreadCount);
    }

    [Fact]
    public async Task NotifyAsync_TypeDisabled_StillCreatesNotification_ButSkipsPush()
    {
        var actorId = Guid.NewGuid();
        await using var provider = TestServiceProviderFactory.Create(actorId, SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();
        var context = provider.GetRequiredService<AppDbContext>();
        var jobScheduler = (FakeBackgroundJobScheduler)provider.GetRequiredService<IBackgroundJobScheduler>();

        var task = await sender.Send(new CreateWorkTaskCommand("Task with comment", null, null, null, null));
        var otherUser = TestDataFactory.CreateUser("other2@example.com");
        context.Users.Add(otherUser);
        await context.SaveChangesAsync(default);

        // otherUser tắt push cho CommentAdded - preference của actor không liên quan, phải đúng
        // preference của NGƯỜI NHẬN (otherUser), không phải người tạo bình luận (actor). Ghi
        // preference TRƯỚC khi AddTaskAssigneeCommand chạy: lệnh này cũng gọi NotifyAsync (báo
        // "AssigneeAdded") cho otherUser, và đó là lần đầu tiên cache "disabled types" của
        // otherUser được nạp (cache-aside, TTL 5 phút) - nếu ghi preference SAU đó, cache đã lỡ
        // nạp danh sách rỗng từ trước và sẽ không thấy preference mới thêm.
        context.NotificationPreferences.Add(
            new Domain.Entities.NotificationPreference { UserId = otherUser.Id, Type = "CommentAdded" });
        await context.SaveChangesAsync(default);

        await sender.Send(new AddTaskAssigneeCommand(task.Id, otherUser.Id, TaskAssigneeRole.Watcher));
        jobScheduler.Enqueued.Clear();

        await sender.Send(new CreateCommentCommand(task.Id, "Bình luận test.", []));

        var notification = Assert.Single(context.Notifications, n => n.Type == "CommentAdded");
        Assert.Equal(otherUser.Id, notification.UserId);
        Assert.DoesNotContain(jobScheduler.Enqueued, e => e.UserId == otherUser.Id);
    }

    [Fact]
    public async Task NotifyAsync_TypeNotDisabled_CreatesNotificationAndEnqueuesPush()
    {
        var actorId = Guid.NewGuid();
        await using var provider = TestServiceProviderFactory.Create(actorId, SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();
        var context = provider.GetRequiredService<AppDbContext>();
        var jobScheduler = (FakeBackgroundJobScheduler)provider.GetRequiredService<IBackgroundJobScheduler>();

        var task = await sender.Send(new CreateWorkTaskCommand("Task with comment", null, null, null, null));
        var otherUser = TestDataFactory.CreateUser("other3@example.com");
        context.Users.Add(otherUser);
        await context.SaveChangesAsync(default);
        await sender.Send(new AddTaskAssigneeCommand(task.Id, otherUser.Id, TaskAssigneeRole.Watcher));
        jobScheduler.Enqueued.Clear();

        await sender.Send(new CreateCommentCommand(task.Id, "Bình luận test.", []));

        Assert.Single(context.Notifications, n => n.Type == "CommentAdded" && n.UserId == otherUser.Id);
        Assert.Contains(jobScheduler.Enqueued, e => e.UserId == otherUser.Id);
    }
}
