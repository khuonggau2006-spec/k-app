using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Features.Attachments.Commands.DeleteAttachment;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Entities;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.UnitTests.Features.Attachments;

public class DeleteAttachmentCommandHandlerTests
{
    private static Attachment CreateAttachment(Guid workTaskId, Guid uploaderId) => new()
    {
        WorkTaskId = workTaskId,
        FileName = "file.txt",
        StorageKey = Guid.NewGuid().ToString(),
        ContentType = "text/plain",
        SizeBytes = 10,
        CreatedByUserId = uploaderId,
        CreatedAtUtc = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task Handle_Uploader_CanDeleteOwnAttachment()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        var uploader = TestDataFactory.CreateUser();
        var attachment = CreateAttachment(task.Id, uploader.Id);
        context.WorkTasks.Add(task);
        context.Users.Add(uploader);
        context.Attachments.Add(attachment);
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(uploader.Id, SystemRole.Member);
        var handler = new DeleteAttachmentCommandHandler(context, new FakeFileStorageService(), currentUser);

        await handler.Handle(new DeleteAttachmentCommand(task.Id, attachment.Id), default);

        Assert.Null(await context.Attachments.FindAsync(attachment.Id));
    }

    [Fact]
    public async Task Handle_TaskOwner_CanDeleteOthersAttachment()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        var uploader = TestDataFactory.CreateUser("uploader@example.com");
        var owner = TestDataFactory.CreateUser("owner@example.com");
        var attachment = CreateAttachment(task.Id, uploader.Id);
        context.WorkTasks.Add(task);
        context.Users.AddRange(uploader, owner);
        context.Attachments.Add(attachment);
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, owner.Id, TaskAssigneeRole.Owner));
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(owner.Id, SystemRole.Member);
        var handler = new DeleteAttachmentCommandHandler(context, new FakeFileStorageService(), currentUser);

        await handler.Handle(new DeleteAttachmentCommand(task.Id, attachment.Id), default);

        Assert.Null(await context.Attachments.FindAsync(attachment.Id));
    }

    [Fact]
    public async Task Handle_UnrelatedMember_Throws()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        var uploader = TestDataFactory.CreateUser();
        var attachment = CreateAttachment(task.Id, uploader.Id);
        context.WorkTasks.Add(task);
        context.Users.Add(uploader);
        context.Attachments.Add(attachment);
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(Guid.NewGuid(), SystemRole.Member);
        var handler = new DeleteAttachmentCommandHandler(context, new FakeFileStorageService(), currentUser);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => handler.Handle(new DeleteAttachmentCommand(task.Id, attachment.Id), default));

        Assert.NotNull(await context.Attachments.FindAsync(attachment.Id));
    }
}
