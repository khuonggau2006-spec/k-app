using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TaskMgmt.Application.Features.Attachments.Commands.UploadAttachment;
using TaskMgmt.Application.Features.Comments.Commands.CreateComment;
using TaskMgmt.Application.Features.TaskAssignees.Commands.AddTaskAssignee;
using TaskMgmt.Application.Features.TaskAssignees.Commands.ChangeTaskAssigneeRole;
using TaskMgmt.Application.Features.TaskAssignees.Commands.RemoveTaskAssignee;
using TaskMgmt.Application.Features.WorkTasks.Commands.CreateWorkTask;
using TaskMgmt.Application.Features.WorkTasks.Commands.DeleteWorkTask;
using TaskMgmt.Application.Features.WorkTasks.Commands.UpdateWorkTask;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Enums;
using TaskMgmt.Infrastructure.Persistence;

namespace TaskMgmt.Application.UnitTests.Features.Notifications;

// Test theo kiểu end-to-end (MediatR thật + DispatchDomainEventsInterceptor thật) để xác nhận
// domain event -> Notification Event Handler -> IBackgroundJobScheduler.EnqueuePushNotification
// được gọi đúng người nhận, đúng nội dung, và KHÔNG tự thông báo cho actor vừa thực hiện hành động.
public class DomainEventNotificationTests
{
    [Fact]
    public async Task CreateWorkTask_DoesNotEnqueueAnyPush()
    {
        await using var provider = TestServiceProviderFactory.Create(Guid.NewGuid(), SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();
        var scheduler = (FakeBackgroundJobScheduler)provider.GetRequiredService<Application.Common.Interfaces.IBackgroundJobScheduler>();

        await sender.Send(new CreateWorkTaskCommand("Task A", null, null, null, null));

        Assert.Empty(scheduler.Enqueued);
    }

    [Fact]
    public async Task UpdateWorkTask_TitleAndStatusChange_EnqueuesPushToOtherAssigneeOnly()
    {
        var actorId = Guid.NewGuid();
        await using var provider = TestServiceProviderFactory.Create(actorId, SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();
        var context = provider.GetRequiredService<AppDbContext>();
        var scheduler = (FakeBackgroundJobScheduler)provider.GetRequiredService<Application.Common.Interfaces.IBackgroundJobScheduler>();

        var task = await sender.Send(new CreateWorkTaskCommand("Original Title", null, null, null, null));
        var otherUser = TestDataFactory.CreateUser("other@example.com");
        context.Users.Add(otherUser);
        await context.SaveChangesAsync(default);
        await sender.Send(new AddTaskAssigneeCommand(task.Id, otherUser.Id, TaskAssigneeRole.Assignee));
        scheduler.Enqueued.Clear();

        await sender.Send(new UpdateWorkTaskCommand(
            task.Id, "New Title", null, WorkTaskStatus.InProgress, null, null, null));

        Assert.All(scheduler.Enqueued, e => Assert.Equal(otherUser.Id, e.UserId));
        Assert.Contains(scheduler.Enqueued, e => e.Data!["type"] == "FieldChanged");
        Assert.Contains(scheduler.Enqueued, e => e.Data!["type"] == "StatusChanged");
        Assert.DoesNotContain(scheduler.Enqueued, e => e.UserId == actorId);
    }

    [Fact]
    public async Task DeleteWorkTask_EnqueuesPushToOtherAssignee()
    {
        var actorId = Guid.NewGuid();
        await using var provider = TestServiceProviderFactory.Create(actorId, SystemRole.Admin);
        var sender = provider.GetRequiredService<ISender>();
        var context = provider.GetRequiredService<AppDbContext>();
        var scheduler = (FakeBackgroundJobScheduler)provider.GetRequiredService<Application.Common.Interfaces.IBackgroundJobScheduler>();

        var task = await sender.Send(new CreateWorkTaskCommand("To delete", null, null, null, null));
        var otherUser = TestDataFactory.CreateUser("other2@example.com");
        context.Users.Add(otherUser);
        await context.SaveChangesAsync(default);
        await sender.Send(new AddTaskAssigneeCommand(task.Id, otherUser.Id, TaskAssigneeRole.Watcher));
        scheduler.Enqueued.Clear();

        await sender.Send(new DeleteWorkTaskCommand(task.Id));

        Assert.Contains(scheduler.Enqueued, e => e.UserId == otherUser.Id && e.Data!["type"] == "Deleted");
    }

    [Fact]
    public async Task AssigneeAddedRoleChangedAndRemoved_EnqueuesPushToTargetUser()
    {
        var actorId = Guid.NewGuid();
        await using var provider = TestServiceProviderFactory.Create(actorId, SystemRole.Admin);
        var sender = provider.GetRequiredService<ISender>();
        var context = provider.GetRequiredService<AppDbContext>();
        var scheduler = (FakeBackgroundJobScheduler)provider.GetRequiredService<Application.Common.Interfaces.IBackgroundJobScheduler>();

        var task = await sender.Send(new CreateWorkTaskCommand("Task with assignee", null, null, null, null));
        var assignee = TestDataFactory.CreateUser("assignee3@example.com");
        context.Users.Add(assignee);
        await context.SaveChangesAsync(default);

        await sender.Send(new AddTaskAssigneeCommand(task.Id, assignee.Id, TaskAssigneeRole.Reviewer));
        await sender.Send(new ChangeTaskAssigneeRoleCommand(task.Id, assignee.Id, TaskAssigneeRole.Assignee));
        await sender.Send(new RemoveTaskAssigneeCommand(task.Id, assignee.Id));

        Assert.Contains(scheduler.Enqueued, e => e.UserId == assignee.Id && e.Data!["type"] == "AssigneeAdded");
        Assert.Contains(scheduler.Enqueued, e => e.UserId == assignee.Id && e.Data!["type"] == "AssigneeRoleChanged");
        Assert.Contains(scheduler.Enqueued, e => e.UserId == assignee.Id && e.Data!["type"] == "AssigneeRemoved");
        Assert.DoesNotContain(scheduler.Enqueued, e => e.UserId == actorId);
    }

    [Fact]
    public async Task CreateComment_EnqueuesPushToOtherAssigneeOnly()
    {
        var actorId = Guid.NewGuid();
        await using var provider = TestServiceProviderFactory.Create(actorId, SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();
        var context = provider.GetRequiredService<AppDbContext>();
        var scheduler = (FakeBackgroundJobScheduler)provider.GetRequiredService<Application.Common.Interfaces.IBackgroundJobScheduler>();

        var task = await sender.Send(new CreateWorkTaskCommand("Task with comment", null, null, null, null));
        var otherUser = TestDataFactory.CreateUser("other4@example.com");
        context.Users.Add(otherUser);
        await context.SaveChangesAsync(default);
        await sender.Send(new AddTaskAssigneeCommand(task.Id, otherUser.Id, TaskAssigneeRole.Watcher));
        scheduler.Enqueued.Clear();

        await sender.Send(new CreateCommentCommand(task.Id, "Bình luận test.", []));

        var notification = Assert.Single(scheduler.Enqueued);
        Assert.Equal(otherUser.Id, notification.UserId);
        Assert.Equal("CommentAdded", notification.Data!["type"]);
    }

    [Fact]
    public async Task UploadAttachment_EnqueuesPushToOtherAssigneeOnly()
    {
        var actorId = Guid.NewGuid();
        await using var provider = TestServiceProviderFactory.Create(actorId, SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();
        var context = provider.GetRequiredService<AppDbContext>();
        var scheduler = (FakeBackgroundJobScheduler)provider.GetRequiredService<Application.Common.Interfaces.IBackgroundJobScheduler>();

        var task = await sender.Send(new CreateWorkTaskCommand("Task with attachment", null, null, null, null));
        var otherUser = TestDataFactory.CreateUser("other5@example.com");
        context.Users.Add(otherUser);
        await context.SaveChangesAsync(default);
        await sender.Send(new AddTaskAssigneeCommand(task.Id, otherUser.Id, TaskAssigneeRole.Watcher));
        scheduler.Enqueued.Clear();

        byte[] pngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];
        await sender.Send(new UploadAttachmentCommand(task.Id, "photo.png", pngHeader.Length, new MemoryStream(pngHeader)));

        var notification = Assert.Single(scheduler.Enqueued);
        Assert.Equal(otherUser.Id, notification.UserId);
        Assert.Equal("AttachmentAdded", notification.Data!["type"]);
    }
}
