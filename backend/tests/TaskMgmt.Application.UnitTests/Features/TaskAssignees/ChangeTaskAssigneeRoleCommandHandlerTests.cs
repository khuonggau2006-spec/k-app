using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Features.TaskAssignees.Commands.ChangeTaskAssigneeRole;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.UnitTests.Features.TaskAssignees;

public class ChangeTaskAssigneeRoleCommandHandlerTests
{
    [Fact]
    public async Task Handle_TaskOwner_CanChangeRole()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        var owner = TestDataFactory.CreateUser("owner@example.com");
        var member = TestDataFactory.CreateUser("member@example.com");
        context.WorkTasks.Add(task);
        context.Users.AddRange(owner, member);
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, owner.Id, TaskAssigneeRole.Owner));
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, member.Id, TaskAssigneeRole.Watcher));
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(owner.Id, SystemRole.Member);
        var handler = new ChangeTaskAssigneeRoleCommandHandler(context, currentUser);
        var command = new ChangeTaskAssigneeRoleCommand(task.Id, member.Id, TaskAssigneeRole.Reviewer);

        var result = await handler.Handle(command, default);

        Assert.Equal(TaskAssigneeRole.Reviewer, result.Role);
    }

    [Fact]
    public async Task Handle_PlainAssignee_CannotPromoteSelfToOwner()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        var user = TestDataFactory.CreateUser();
        context.WorkTasks.Add(task);
        context.Users.Add(user);
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, user.Id, TaskAssigneeRole.Assignee));
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(user.Id, SystemRole.Member);
        var handler = new ChangeTaskAssigneeRoleCommandHandler(context, currentUser);
        var command = new ChangeTaskAssigneeRoleCommand(task.Id, user.Id, TaskAssigneeRole.Owner);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(command, default));
    }
}
