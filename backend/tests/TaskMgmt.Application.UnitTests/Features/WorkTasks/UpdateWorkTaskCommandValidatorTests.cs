using TaskMgmt.Application.Features.WorkTasks.Commands.UpdateWorkTask;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.UnitTests.Features.WorkTasks;

public class UpdateWorkTaskCommandValidatorTests
{
    [Fact]
    public async Task Validate_SelfAsParent_IsInvalid()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        context.WorkTasks.Add(task);
        await context.SaveChangesAsync(default);

        var validator = new UpdateWorkTaskCommandValidator(context);
        var command = new UpdateWorkTaskCommand(task.Id, "Task", null, WorkTaskStatus.ToDo, null, task.Id, null);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateWorkTaskCommand.ParentTaskId)
            && e.ErrorMessage.Contains("chính nó"));
    }

    [Fact]
    public async Task Validate_ParentIsOwnDescendant_CreatesCycle_IsInvalid()
    {
        using var context = TestDbContextFactory.Create();
        var root = TestDataFactory.CreateWorkTask("Root");
        var child = TestDataFactory.CreateWorkTask("Child", root.Id);
        context.WorkTasks.AddRange(root, child);
        await context.SaveChangesAsync(default);

        var validator = new UpdateWorkTaskCommandValidator(context);
        // Attempt to move root under its own child.
        var command = new UpdateWorkTaskCommand(root.Id, "Root", null, WorkTaskStatus.ToDo, null, child.Id, null);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateWorkTaskCommand.ParentTaskId)
            && e.ErrorMessage.Contains("công việc con"));
    }

    [Fact]
    public async Task Validate_ReparentToUnrelatedTask_IsValid()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask("Task");
        var otherRoot = TestDataFactory.CreateWorkTask("OtherRoot");
        context.WorkTasks.AddRange(task, otherRoot);
        await context.SaveChangesAsync(default);

        var validator = new UpdateWorkTaskCommandValidator(context);
        var command = new UpdateWorkTaskCommand(task.Id, "Task", null, WorkTaskStatus.ToDo, null, otherRoot.Id, null);

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_ReparentExceedingMaxDepth_IsInvalid()
    {
        using var context = TestDbContextFactory.Create();
        var root = TestDataFactory.CreateWorkTask("Root");
        var level2 = TestDataFactory.CreateWorkTask("Level2", root.Id);
        var level3 = TestDataFactory.CreateWorkTask("Level3", level2.Id);
        var standalone = TestDataFactory.CreateWorkTask("Standalone");
        context.WorkTasks.AddRange(root, level2, level3, standalone);
        await context.SaveChangesAsync(default);

        var validator = new UpdateWorkTaskCommandValidator(context);
        // Moving a standalone task under level3 (which is already level 3) would make it level 4.
        var command = new UpdateWorkTaskCommand(standalone.Id, "Standalone", null, WorkTaskStatus.ToDo, null, level3.Id, null);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateWorkTaskCommand.ParentTaskId)
            && e.ErrorMessage.Contains("giới hạn"));
    }
}
