using TaskMgmt.Application.Features.WorkTasks.Commands.CreateWorkTask;
using TaskMgmt.Application.UnitTests.Common;

namespace TaskMgmt.Application.UnitTests.Features.WorkTasks;

public class CreateWorkTaskCommandValidatorTests
{
    [Fact]
    public async Task Validate_RootTaskWithNoParent_IsValid()
    {
        using var context = TestDbContextFactory.Create();
        var validator = new CreateWorkTaskCommandValidator(context);
        var command = new CreateWorkTaskCommand("New Task", null, null, null, null);

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_EmptyTitle_IsInvalid()
    {
        using var context = TestDbContextFactory.Create();
        var validator = new CreateWorkTaskCommandValidator(context);
        var command = new CreateWorkTaskCommand("", null, null, null, null);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateWorkTaskCommand.Title));
    }

    [Fact]
    public async Task Validate_NonExistentParentTaskId_IsInvalid()
    {
        using var context = TestDbContextFactory.Create();
        var validator = new CreateWorkTaskCommandValidator(context);
        var command = new CreateWorkTaskCommand("Sub", null, null, Guid.NewGuid(), null);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateWorkTaskCommand.ParentTaskId));
    }

    [Fact]
    public async Task Validate_NonExistentLocationId_IsInvalid()
    {
        using var context = TestDbContextFactory.Create();
        var validator = new CreateWorkTaskCommandValidator(context);
        var command = new CreateWorkTaskCommand("Task", null, null, null, Guid.NewGuid());

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateWorkTaskCommand.LocationId));
    }

    [Fact]
    public async Task Validate_ThirdLevelSubtask_IsValid()
    {
        using var context = TestDbContextFactory.Create();
        var root = TestDataFactory.CreateWorkTask("Root");
        var level2 = TestDataFactory.CreateWorkTask("Level2", root.Id);
        context.WorkTasks.AddRange(root, level2);
        await context.SaveChangesAsync(default);

        var validator = new CreateWorkTaskCommandValidator(context);
        var command = new CreateWorkTaskCommand("Level3", null, null, level2.Id, null);

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_FourthLevelSubtask_ExceedsMaxDepth_IsInvalid()
    {
        using var context = TestDbContextFactory.Create();
        var root = TestDataFactory.CreateWorkTask("Root");
        var level2 = TestDataFactory.CreateWorkTask("Level2", root.Id);
        var level3 = TestDataFactory.CreateWorkTask("Level3", level2.Id);
        context.WorkTasks.AddRange(root, level2, level3);
        await context.SaveChangesAsync(default);

        var validator = new CreateWorkTaskCommandValidator(context);
        var command = new CreateWorkTaskCommand("Level4", null, null, level3.Id, null);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateWorkTaskCommand.ParentTaskId)
            && e.ErrorMessage.Contains("giới hạn"));
    }
}
