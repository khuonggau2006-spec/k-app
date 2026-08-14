using Microsoft.Extensions.Logging.Abstractions;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Enums;
using TaskMgmt.Infrastructure.BackgroundJobs;

namespace TaskMgmt.Application.UnitTests.BackgroundJobs;

public class SendDueSoonReminderJobTests
{
    [Fact]
    public async Task Execute_TaskDueWithinWindow_NotifiesAndMarksSent()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        var task = TestDataFactory.CreateWorkTask();
        task.DueDateUtc = DateTimeOffset.UtcNow.AddHours(12);
        context.Users.Add(user);
        context.WorkTasks.Add(task);
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, user.Id, TaskAssigneeRole.Owner));
        await context.SaveChangesAsync(default);

        var scheduler = new FakeBackgroundJobScheduler();
        var job = new SendDueSoonReminderJob(context, scheduler, new FakeCacheService(), NullLogger<SendDueSoonReminderJob>.Instance);

        await job.ExecuteAsync();

        // Phải hiện trong Trung tâm thông báo trong app, không chỉ mỗi push.
        var notification = Assert.Single(context.Notifications.Where(n => n.UserId == user.Id));
        Assert.Equal("DueSoon", notification.Type);
        Assert.Single(scheduler.Enqueued);
        var updated = await context.WorkTasks.FindAsync(task.Id);
        Assert.NotNull(updated!.DueSoonReminderSentAtUtc);
    }

    [Fact]
    public async Task Execute_TaskDueFarInFuture_DoesNotNotify()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        var task = TestDataFactory.CreateWorkTask();
        task.DueDateUtc = DateTimeOffset.UtcNow.AddDays(3);
        context.Users.Add(user);
        context.WorkTasks.Add(task);
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, user.Id, TaskAssigneeRole.Owner));
        await context.SaveChangesAsync(default);

        var scheduler = new FakeBackgroundJobScheduler();
        var job = new SendDueSoonReminderJob(context, scheduler, new FakeCacheService(), NullLogger<SendDueSoonReminderJob>.Instance);

        await job.ExecuteAsync();

        Assert.Empty(context.Notifications);
        Assert.Empty(scheduler.Enqueued);
    }

    [Fact]
    public async Task Execute_AlreadyReminded_DoesNotNotifyAgain()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        var task = TestDataFactory.CreateWorkTask();
        task.DueDateUtc = DateTimeOffset.UtcNow.AddHours(12);
        task.DueSoonReminderSentAtUtc = DateTimeOffset.UtcNow.AddHours(-1);
        context.Users.Add(user);
        context.WorkTasks.Add(task);
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, user.Id, TaskAssigneeRole.Owner));
        await context.SaveChangesAsync(default);

        var scheduler = new FakeBackgroundJobScheduler();
        var job = new SendDueSoonReminderJob(context, scheduler, new FakeCacheService(), NullLogger<SendDueSoonReminderJob>.Instance);

        await job.ExecuteAsync();

        Assert.Empty(context.Notifications);
        Assert.Empty(scheduler.Enqueued);
    }

    [Fact]
    public async Task Execute_TaskAlreadyDone_DoesNotNotify()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        var task = TestDataFactory.CreateWorkTask();
        task.DueDateUtc = DateTimeOffset.UtcNow.AddHours(12);
        task.Status = WorkTaskStatus.Done;
        context.Users.Add(user);
        context.WorkTasks.Add(task);
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, user.Id, TaskAssigneeRole.Owner));
        await context.SaveChangesAsync(default);

        var scheduler = new FakeBackgroundJobScheduler();
        var job = new SendDueSoonReminderJob(context, scheduler, new FakeCacheService(), NullLogger<SendDueSoonReminderJob>.Instance);

        await job.ExecuteAsync();

        Assert.Empty(context.Notifications);
        Assert.Empty(scheduler.Enqueued);
    }
}
