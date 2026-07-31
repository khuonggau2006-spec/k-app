using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Features.TaskAssignees.Commands.AddTaskAssignee;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.UnitTests.Features.TaskAssignees;

public class AddTaskAssigneeCommandValidatorTests
{
    private static async Task<(TaskMgmt.Infrastructure.Persistence.AppDbContext Context, Guid TaskId, Guid OwnerId)>
        SeedTaskWithOwnerAsync()
    {
        var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        var owner = TestDataFactory.CreateUser("owner@example.com");
        context.WorkTasks.Add(task);
        context.Users.Add(owner);
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, owner.Id, TaskAssigneeRole.Owner));
        await context.SaveChangesAsync(default);

        return (context, task.Id, owner.Id);
    }

    [Fact]
    public async Task Validate_OwnerAddsNewAssignee_IsValid()
    {
        var (context, taskId, ownerId) = await SeedTaskWithOwnerAsync();
        using var _ = context;

        var newUser = TestDataFactory.CreateUser("newbie@example.com");
        context.Users.Add(newUser);
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(ownerId, SystemRole.Member);
        var validator = new AddTaskAssigneeCommandValidator(context, currentUser);
        var command = new AddTaskAssigneeCommand(taskId, newUser.Id, TaskAssigneeRole.Assignee);

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_AddingSecondOwner_IsInvalid()
    {
        var (context, taskId, ownerId) = await SeedTaskWithOwnerAsync();
        using var _ = context;

        var newUser = TestDataFactory.CreateUser("newbie@example.com");
        context.Users.Add(newUser);
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(ownerId, SystemRole.Member);
        var validator = new AddTaskAssigneeCommandValidator(context, currentUser);
        var command = new AddTaskAssigneeCommand(taskId, newUser.Id, TaskAssigneeRole.Owner);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddTaskAssigneeCommand.Role)
            && e.ErrorMessage.Contains("Owner"));
    }

    [Fact]
    public async Task Validate_AddingDuplicateUser_IsInvalid()
    {
        var (context, taskId, ownerId) = await SeedTaskWithOwnerAsync();
        using var _ = context;

        var currentUser = new FakeCurrentUserService(ownerId, SystemRole.Member);
        var validator = new AddTaskAssigneeCommandValidator(context, currentUser);
        // Owner is already assigned; trying to add them again with a different role.
        var command = new AddTaskAssigneeCommand(taskId, ownerId, TaskAssigneeRole.Watcher);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddTaskAssigneeCommand.UserId));
    }

    [Fact]
    public async Task Validate_NonOwnerNonElevatedCaller_ThrowsForbidden()
    {
        var (context, taskId, _) = await SeedTaskWithOwnerAsync();
        using var _ = context;

        var newUser = TestDataFactory.CreateUser("newbie@example.com");
        context.Users.Add(newUser);
        await context.SaveChangesAsync(default);

        // A random Member with no relation to the task at all.
        var currentUser = new FakeCurrentUserService(Guid.NewGuid(), SystemRole.Member);
        var validator = new AddTaskAssigneeCommandValidator(context, currentUser);
        var command = new AddTaskAssigneeCommand(taskId, newUser.Id, TaskAssigneeRole.Assignee);

        await Assert.ThrowsAsync<ForbiddenException>(() => validator.ValidateAsync(command));
    }

    [Fact]
    public async Task Validate_ManagerCaller_NotTaskOwner_IsAllowedToProceed()
    {
        var (context, taskId, _) = await SeedTaskWithOwnerAsync();
        using var _ = context;

        var newUser = TestDataFactory.CreateUser("newbie@example.com");
        context.Users.Add(newUser);
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(Guid.NewGuid(), SystemRole.Manager);
        var validator = new AddTaskAssigneeCommandValidator(context, currentUser);
        var command = new AddTaskAssigneeCommand(taskId, newUser.Id, TaskAssigneeRole.Reviewer);

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_NonExistentWorkTask_IsInvalid()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(Guid.NewGuid(), SystemRole.Admin);
        var validator = new AddTaskAssigneeCommandValidator(context, currentUser);
        var command = new AddTaskAssigneeCommand(Guid.NewGuid(), user.Id, TaskAssigneeRole.Assignee);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddTaskAssigneeCommand.WorkTaskId));
    }
}
