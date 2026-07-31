using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Features.TaskAssignees.Commands.ChangeTaskAssigneeRole;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.UnitTests.Features.TaskAssignees;

public class ChangeTaskAssigneeRoleCommandValidatorTests
{
    [Fact]
    public async Task Validate_OwnerReassertsOwnRole_IsValid()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        var owner = TestDataFactory.CreateUser();
        context.WorkTasks.Add(task);
        context.Users.Add(owner);
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, owner.Id, TaskAssigneeRole.Owner));
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(owner.Id, SystemRole.Member);
        var validator = new ChangeTaskAssigneeRoleCommandValidator(context, currentUser);
        var command = new ChangeTaskAssigneeRoleCommand(task.Id, owner.Id, TaskAssigneeRole.Owner);

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_PromotingAnotherUserToOwner_WhileOwnerExists_IsInvalid()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        var owner = TestDataFactory.CreateUser("owner@example.com");
        var assignee = TestDataFactory.CreateUser("assignee@example.com");
        context.WorkTasks.Add(task);
        context.Users.AddRange(owner, assignee);
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, owner.Id, TaskAssigneeRole.Owner));
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, assignee.Id, TaskAssigneeRole.Assignee));
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(owner.Id, SystemRole.Member);
        var validator = new ChangeTaskAssigneeRoleCommandValidator(context, currentUser);
        var command = new ChangeTaskAssigneeRoleCommand(task.Id, assignee.Id, TaskAssigneeRole.Owner);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ChangeTaskAssigneeRoleCommand.Role)
            && e.ErrorMessage.Contains("Owner"));
    }

    [Fact]
    public async Task Validate_UserNotAssignedToTask_IsInvalid()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        var owner = TestDataFactory.CreateUser("owner@example.com");
        var stranger = TestDataFactory.CreateUser("stranger@example.com");
        context.WorkTasks.Add(task);
        context.Users.AddRange(owner, stranger);
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, owner.Id, TaskAssigneeRole.Owner));
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(owner.Id, SystemRole.Member);
        var validator = new ChangeTaskAssigneeRoleCommandValidator(context, currentUser);
        var command = new ChangeTaskAssigneeRoleCommand(task.Id, stranger.Id, TaskAssigneeRole.Watcher);

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ChangeTaskAssigneeRoleCommand.UserId));
    }

    [Fact]
    public async Task Validate_NonOwnerCaller_ThrowsForbidden()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        var owner = TestDataFactory.CreateUser("owner@example.com");
        var assignee = TestDataFactory.CreateUser("assignee@example.com");
        context.WorkTasks.Add(task);
        context.Users.AddRange(owner, assignee);
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, owner.Id, TaskAssigneeRole.Owner));
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, assignee.Id, TaskAssigneeRole.Assignee));
        await context.SaveChangesAsync(default);

        // The assignee (not owner, not elevated) tries to change their own role.
        var currentUser = new FakeCurrentUserService(assignee.Id, SystemRole.Member);
        var validator = new ChangeTaskAssigneeRoleCommandValidator(context, currentUser);
        var command = new ChangeTaskAssigneeRoleCommand(task.Id, assignee.Id, TaskAssigneeRole.Watcher);

        await Assert.ThrowsAsync<ForbiddenException>(() => validator.ValidateAsync(command));
    }
}
