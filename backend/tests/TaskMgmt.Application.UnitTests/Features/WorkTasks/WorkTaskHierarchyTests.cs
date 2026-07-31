using TaskMgmt.Application.Features.WorkTasks.Common;
using TaskMgmt.Application.UnitTests.Common;

namespace TaskMgmt.Application.UnitTests.Features.WorkTasks;

public class WorkTaskHierarchyTests
{
    [Fact]
    public async Task GetLevelAsync_RootTask_ReturnsOne()
    {
        using var context = TestDbContextFactory.Create();
        var root = TestDataFactory.CreateWorkTask();
        context.WorkTasks.Add(root);
        await context.SaveChangesAsync(default);

        var level = await WorkTaskHierarchy.GetLevelAsync(context, root.Id, default);

        Assert.Equal(1, level);
    }

    [Fact]
    public async Task GetLevelAsync_TaskWithOneAncestor_ReturnsTwo()
    {
        using var context = TestDbContextFactory.Create();
        var root = TestDataFactory.CreateWorkTask("Root");
        var child = TestDataFactory.CreateWorkTask("Child", root.Id);
        context.WorkTasks.AddRange(root, child);
        await context.SaveChangesAsync(default);

        var level = await WorkTaskHierarchy.GetLevelAsync(context, child.Id, default);

        Assert.Equal(2, level);
    }

    [Fact]
    public async Task GetLevelAsync_TaskWithTwoAncestors_ReturnsThree()
    {
        using var context = TestDbContextFactory.Create();
        var root = TestDataFactory.CreateWorkTask("Root");
        var child = TestDataFactory.CreateWorkTask("Child", root.Id);
        var grandchild = TestDataFactory.CreateWorkTask("Grandchild", child.Id);
        context.WorkTasks.AddRange(root, child, grandchild);
        await context.SaveChangesAsync(default);

        var level = await WorkTaskHierarchy.GetLevelAsync(context, grandchild.Id, default);

        Assert.Equal(3, level);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, true)]
    public void ExceedsMaxDepth_ChecksAgainstMaxLevel(int parentLevel, bool expected)
    {
        Assert.Equal(expected, WorkTaskHierarchy.ExceedsMaxDepth(parentLevel));
    }

    [Fact]
    public async Task WouldCreateCycleAsync_DirectSelfReference_ReturnsTrue()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        context.WorkTasks.Add(task);
        await context.SaveChangesAsync(default);

        var result = await WorkTaskHierarchy.WouldCreateCycleAsync(context, task.Id, task.Id, default);

        Assert.True(result);
    }

    [Fact]
    public async Task WouldCreateCycleAsync_NewParentIsDescendant_ReturnsTrue()
    {
        using var context = TestDbContextFactory.Create();
        var root = TestDataFactory.CreateWorkTask("Root");
        var child = TestDataFactory.CreateWorkTask("Child", root.Id);
        context.WorkTasks.AddRange(root, child);
        await context.SaveChangesAsync(default);

        // Trying to make root's parent be its own child -> cycle.
        var result = await WorkTaskHierarchy.WouldCreateCycleAsync(context, root.Id, child.Id, default);

        Assert.True(result);
    }

    [Fact]
    public async Task WouldCreateCycleAsync_UnrelatedTask_ReturnsFalse()
    {
        using var context = TestDbContextFactory.Create();
        var taskA = TestDataFactory.CreateWorkTask("A");
        var taskB = TestDataFactory.CreateWorkTask("B");
        context.WorkTasks.AddRange(taskA, taskB);
        await context.SaveChangesAsync(default);

        var result = await WorkTaskHierarchy.WouldCreateCycleAsync(context, taskA.Id, taskB.Id, default);

        Assert.False(result);
    }
}
