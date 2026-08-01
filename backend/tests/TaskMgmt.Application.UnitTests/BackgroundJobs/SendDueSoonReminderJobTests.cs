using Microsoft.Extensions.Logging.Abstractions;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Enums;
using TaskMgmt.Infrastructure.BackgroundJobs;

namespace TaskMgmt.Application.UnitTests.BackgroundJobs;

public class SendDueSoonReminderJobTests
{
    [Fact]
    public async Task Execute_TaskDueWithinWindow_SendsReminderAndMarksSent()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        var task = TestDataFactory.CreateWorkTask();
        task.DueDateUtc = DateTimeOffset.UtcNow.AddHours(12);
        context.Users.Add(user);
        context.WorkTasks.Add(task);
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, user.Id, TaskAssigneeRole.Owner));
        await context.SaveChangesAsync(default);

        var push = new FakePushNotificationService();
        var job = new SendDueSoonReminderJob(context, push, NullLogger<SendDueSoonReminderJob>.Instance);

        await job.ExecuteAsync();

        Assert.Single(push.Sent);
        Assert.Equal(user.Id, push.Sent[0].UserId);
        var updated = await context.WorkTasks.FindAsync(task.Id);
        Assert.NotNull(updated!.DueSoonReminderSentAtUtc);
    }

    [Fact]
    public async Task Execute_TaskDueFarInFuture_DoesNotSend()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        var task = TestDataFactory.CreateWorkTask();
        task.DueDateUtc = DateTimeOffset.UtcNow.AddDays(3);
        context.Users.Add(user);
        context.WorkTasks.Add(task);
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, user.Id, TaskAssigneeRole.Owner));
        await context.SaveChangesAsync(default);

        var push = new FakePushNotificationService();
        var job = new SendDueSoonReminderJob(context, push, NullLogger<SendDueSoonReminderJob>.Instance);

        await job.ExecuteAsync();

        Assert.Empty(push.Sent);
    }

    [Fact]
    public async Task Execute_AlreadyReminded_DoesNotSendAgain()
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

        var push = new FakePushNotificationService();
        var job = new SendDueSoonReminderJob(context, push, NullLogger<SendDueSoonReminderJob>.Instance);

        await job.ExecuteAsync();

        Assert.Empty(push.Sent);
    }

    [Fact]
    public async Task Execute_TaskAlreadyDone_DoesNotSend()
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

        var push = new FakePushNotificationService();
        var job = new SendDueSoonReminderJob(context, push, NullLogger<SendDueSoonReminderJob>.Instance);

        await job.ExecuteAsync();

        Assert.Empty(push.Sent);
    }
}
