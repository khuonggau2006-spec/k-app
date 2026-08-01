using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TaskMgmt.Application.Features.Attachments.Commands.DeleteAttachment;
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

namespace TaskMgmt.Application.UnitTests.Features.TaskHistories;

// Test theo kiểu end-to-end (MediatR thật + DispatchDomainEventsInterceptor thật + DbContext
// InMemory) để xác nhận cơ chế domain event -> TaskHistory hoạt động đúng cho từng loại thay
// đổi, không chỉ test logic thuần của từng handler riêng lẻ.
public class DomainEventHistoryTests
{
    [Fact]
    public async Task CreateWorkTask_RecordsCreatedHistory()
    {
        var actorId = Guid.NewGuid();
        await using var provider = TestServiceProviderFactory.Create(actorId, SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();
        var context = provider.GetRequiredService<AppDbContext>();

        var task = await sender.Send(new CreateWorkTaskCommand("Task A", null, null, null, null));

        var history = Assert.Single(context.TaskHistories.Where(h => h.WorkTaskId == task.Id));
        Assert.Equal(TaskHistoryActionType.Created, history.ActionType);
        Assert.Equal(actorId, history.ActorUserId);
    }

    [Fact]
    public async Task UpdateWorkTask_TitleAndStatusChange_RecordsFieldChangedAndStatusChangedHistory()
    {
        var actorId = Guid.NewGuid();
        await using var provider = TestServiceProviderFactory.Create(actorId, SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();
        var context = provider.GetRequiredService<AppDbContext>();

        var task = await sender.Send(new CreateWorkTaskCommand("Original Title", null, null, null, null));

        await sender.Send(new UpdateWorkTaskCommand(
            task.Id, "New Title", null, WorkTaskStatus.InProgress, null, null, null));

        var histories = context.TaskHistories.Where(h => h.WorkTaskId == task.Id).ToList();

        Assert.Contains(histories, h =>
            h.ActionType == TaskHistoryActionType.FieldChanged
            && h.FieldName == "Title"
            && h.OldValue == "Original Title"
            && h.NewValue == "New Title");

        Assert.Contains(histories, h =>
            h.ActionType == TaskHistoryActionType.StatusChanged
            && h.OldValue == "ToDo"
            && h.NewValue == "InProgress");
    }

    [Fact]
    public async Task UpdateWorkTask_NoActualChange_DoesNotRecordExtraHistory()
    {
        await using var provider = TestServiceProviderFactory.Create(Guid.NewGuid(), SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();
        var context = provider.GetRequiredService<AppDbContext>();

        var task = await sender.Send(new CreateWorkTaskCommand("Same Title", "Same Description", null, null, null));

        await sender.Send(new UpdateWorkTaskCommand(
            task.Id, "Same Title", "Same Description", WorkTaskStatus.ToDo, null, null, null));

        // Chỉ có đúng 1 dòng lịch sử "Created" từ bước tạo, update không đổi gì thì không sinh thêm dòng nào.
        var history = Assert.Single(context.TaskHistories.Where(h => h.WorkTaskId == task.Id));
        Assert.Equal(TaskHistoryActionType.Created, history.ActionType);
    }

    [Fact]
    public async Task DeleteWorkTask_RecordsDeletedHistory()
    {
        await using var provider = TestServiceProviderFactory.Create(Guid.NewGuid(), SystemRole.Admin);
        var sender = provider.GetRequiredService<ISender>();
        var context = provider.GetRequiredService<AppDbContext>();

        var task = await sender.Send(new CreateWorkTaskCommand("To be deleted", null, null, null, null));
        await sender.Send(new DeleteWorkTaskCommand(task.Id));

        var histories = context.TaskHistories.Where(h => h.WorkTaskId == task.Id).ToList();
        Assert.Contains(histories, h => h.ActionType == TaskHistoryActionType.Deleted);
    }

    [Fact]
    public async Task AssigneeAddedRemovedAndRoleChanged_RecordsHistory()
    {
        var actorId = Guid.NewGuid();
        await using var provider = TestServiceProviderFactory.Create(actorId, SystemRole.Admin);
        var sender = provider.GetRequiredService<ISender>();
        var context = provider.GetRequiredService<AppDbContext>();

        var task = await sender.Send(new CreateWorkTaskCommand("Task with assignee", null, null, null, null));
        var assignee = TestDataFactory.CreateUser("assignee@example.com");
        context.Users.Add(assignee);
        await context.SaveChangesAsync(default);

        await sender.Send(new AddTaskAssigneeCommand(task.Id, assignee.Id, TaskAssigneeRole.Reviewer));
        await sender.Send(new ChangeTaskAssigneeRoleCommand(task.Id, assignee.Id, TaskAssigneeRole.Assignee));
        await sender.Send(new RemoveTaskAssigneeCommand(task.Id, assignee.Id));

        var histories = context.TaskHistories.Where(h => h.WorkTaskId == task.Id).ToList();

        Assert.Contains(histories, h => h.ActionType == TaskHistoryActionType.AssigneeAdded && h.TargetUserId == assignee.Id);
        Assert.Contains(histories, h =>
            h.ActionType == TaskHistoryActionType.AssigneeRoleChanged
            && h.TargetUserId == assignee.Id
            && h.OldValue == "Reviewer"
            && h.NewValue == "Assignee");
        Assert.Contains(histories, h => h.ActionType == TaskHistoryActionType.AssigneeRemoved && h.TargetUserId == assignee.Id);
    }

    [Fact]
    public async Task CreateComment_RecordsCommentAddedHistory()
    {
        await using var provider = TestServiceProviderFactory.Create(Guid.NewGuid(), SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();
        var context = provider.GetRequiredService<AppDbContext>();

        var task = await sender.Send(new CreateWorkTaskCommand("Task with comment", null, null, null, null));
        await sender.Send(new CreateCommentCommand(task.Id, "Bình luận test.", []));

        var histories = context.TaskHistories.Where(h => h.WorkTaskId == task.Id).ToList();
        Assert.Contains(histories, h => h.ActionType == TaskHistoryActionType.CommentAdded);
    }

    [Fact]
    public async Task UploadAndDeleteAttachment_RecordsHistory()
    {
        await using var provider = TestServiceProviderFactory.Create(Guid.NewGuid(), SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();
        var context = provider.GetRequiredService<AppDbContext>();

        var task = await sender.Send(new CreateWorkTaskCommand("Task with attachment", null, null, null, null));

        byte[] pngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];
        var attachment = await sender.Send(new UploadAttachmentCommand(task.Id, "photo.png", pngHeader.Length, new MemoryStream(pngHeader)));
        await sender.Send(new DeleteAttachmentCommand(task.Id, attachment.Id));

        var histories = context.TaskHistories.Where(h => h.WorkTaskId == task.Id).ToList();
        Assert.Contains(histories, h => h.ActionType == TaskHistoryActionType.AttachmentAdded);
        Assert.Contains(histories, h => h.ActionType == TaskHistoryActionType.AttachmentRemoved);
    }
}
