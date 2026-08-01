using Microsoft.Extensions.Logging.Abstractions;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Enums;
using TaskMgmt.Infrastructure.BackgroundJobs;

namespace TaskMgmt.Application.UnitTests.BackgroundJobs;

public class SendOverdueReminderJobTests
{
    [Fact]
    public async Task Execute_OverdueTaskNeverReminded_Sends()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        var task = TestDataFactory.CreateWorkTask();
        task.DueDateUtc = DateTimeOffset.UtcNow.AddHours(-2);
        context.Users.Add(user);
        context.WorkTasks.Add(task);
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, user.Id, TaskAssigneeRole.Owner));
        await context.SaveChangesAsync(default);

        var push = new FakePushNotificationService();
        var job = new SendOverdueReminderJob(context, push, NullLogger<SendOverdueReminderJob>.Instance);

        await job.ExecuteAsync();

        Assert.Single(push.Sent);
        var updated = await context.WorkTasks.FindAsync(task.Id);
        Assert.NotNull(updated!.OverdueReminderSentAtUtc);
    }

    [Fact]
    public async Task Execute_RemindedRecently_DoesNotResendYet()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        var task = TestDataFactory.CreateWorkTask();
        task.DueDateUtc = DateTimeOffset.UtcNow.AddDays(-2);
        task.OverdueReminderSentAtUtc = DateTimeOffset.UtcNow.AddHours(-1);
        context.Users.Add(user);
        context.WorkTasks.Add(task);
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, user.Id, TaskAssigneeRole.Owner));
        await context.SaveChangesAsync(default);

        var push = new FakePushNotificationService();
        var job = new SendOverdueReminderJob(context, push, NullLogger<SendOverdueReminderJob>.Instance);

        await job.ExecuteAsync();

        Assert.Empty(push.Sent);
    }

    [Fact]
    public async Task Execute_RemindedLongAgo_ResendsAgain()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        var task = TestDataFactory.CreateWorkTask();
        task.DueDateUtc = DateTimeOffset.UtcNow.AddDays(-3);
        task.OverdueReminderSentAtUtc = DateTimeOffset.UtcNow.AddHours(-30);
        context.Users.Add(user);
        context.WorkTasks.Add(task);
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, user.Id, TaskAssigneeRole.Owner));
        await context.SaveChangesAsync(default);

        var push = new FakePushNotificationService();
        var job = new SendOverdueReminderJob(context, push, NullLogger<SendOverdueReminderJob>.Instance);

        await job.ExecuteAsync();

        Assert.Single(push.Sent);
    }

    [Fact]
    public async Task Execute_NotYetDue_DoesNotSend()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        var task = TestDataFactory.CreateWorkTask();
        task.DueDateUtc = DateTimeOffset.UtcNow.AddHours(2);
        context.Users.Add(user);
        context.WorkTasks.Add(task);
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, user.Id, TaskAssigneeRole.Owner));
        await context.SaveChangesAsync(default);

        var push = new FakePushNotificationService();
        var job = new SendOverdueReminderJob(context, push, NullLogger<SendOverdueReminderJob>.Instance);

        await job.ExecuteAsync();

        Assert.Empty(push.Sent);
    }
}
