using TaskMgmt.Application.Features.Comments.Commands.CreateComment;
using TaskMgmt.Application.UnitTests.Common;

namespace TaskMgmt.Application.UnitTests.Features.Comments;

public class CreateCommentCommandValidatorTests
{
    [Fact]
    public async Task Validate_ValidComment_IsValid()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        context.WorkTasks.Add(task);
        await context.SaveChangesAsync(default);

        var validator = new CreateCommentCommandValidator(context);
        var command = new CreateCommentCommand(task.Id, "Nội dung bình luận.", []);

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_EmptyContent_IsInvalid()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        context.WorkTasks.Add(task);
        await context.SaveChangesAsync(default);

        var validator = new CreateCommentCommandValidator(context);
        var command = new CreateCommentCommand(task.Id, "", []);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCommentCommand.Content));
    }

    [Fact]
    public async Task Validate_NonExistentWorkTask_IsInvalid()
    {
        using var context = TestDbContextFactory.Create();

        var validator = new CreateCommentCommandValidator(context);
        var command = new CreateCommentCommand(Guid.NewGuid(), "Nội dung.", []);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCommentCommand.WorkTaskId));
    }

    [Fact]
    public async Task Validate_InactiveWorkTask_IsInvalid()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        task.IsActive = false;
        context.WorkTasks.Add(task);
        await context.SaveChangesAsync(default);

        var validator = new CreateCommentCommandValidator(context);
        var command = new CreateCommentCommand(task.Id, "Nội dung.", []);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCommentCommand.WorkTaskId));
    }

    [Fact]
    public async Task Validate_ExistingMentionedUser_IsValid()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        var mentioned = TestDataFactory.CreateUser("mentioned@example.com");
        context.WorkTasks.Add(task);
        context.Users.Add(mentioned);
        await context.SaveChangesAsync(default);

        var validator = new CreateCommentCommandValidator(context);
        var command = new CreateCommentCommand(task.Id, "Nhắc @mentioned nhé.", [mentioned.Id]);

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_NonExistentMentionedUser_IsInvalid()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        context.WorkTasks.Add(task);
        await context.SaveChangesAsync(default);

        var validator = new CreateCommentCommandValidator(context);
        var command = new CreateCommentCommand(task.Id, "Nội dung.", [Guid.NewGuid()]);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        // RuleForEach lồng chỉ số vào PropertyName, ví dụ "MentionedUserIds[0]".
        Assert.Contains(result.Errors, e => e.PropertyName.StartsWith(nameof(CreateCommentCommand.MentionedUserIds)));
    }
}
