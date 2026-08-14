using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Features.TaskAssignees.Commands.RemoveTaskAssignee;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.UnitTests.Features.TaskAssignees;

public class RemoveTaskAssigneeCommandHandlerTests
{
    [Fact]
    public async Task Handle_PlainAssignee_CanRemoveSelf()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        var user = TestDataFactory.CreateUser();
        context.WorkTasks.Add(task);
        context.Users.Add(user);
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, user.Id, TaskAssigneeRole.Assignee));
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(user.Id, SystemRole.Member);
        var handler = new RemoveTaskAssigneeCommandHandler(context, currentUser);

        await handler.Handle(new RemoveTaskAssigneeCommand(task.Id, user.Id), default);

        Assert.Empty(context.TaskAssignees.Where(a => a.WorkTaskId == task.Id));
    }

    [Fact]
    public async Task Handle_PlainAssignee_CannotRemoveAnotherAssignee()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        var actor = TestDataFactory.CreateUser("actor@example.com");
        var victim = TestDataFactory.CreateUser("victim@example.com");
        context.WorkTasks.Add(task);
        context.Users.AddRange(actor, victim);
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, actor.Id, TaskAssigneeRole.Assignee));
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, victim.Id, TaskAssigneeRole.Assignee));
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(actor.Id, SystemRole.Member);
        var handler = new RemoveTaskAssigneeCommandHandler(context, currentUser);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => handler.Handle(new RemoveTaskAssigneeCommand(task.Id, victim.Id), default));

        Assert.Single(context.TaskAssignees.Where(a => a.WorkTaskId == task.Id && a.UserId == victim.Id));
    }

    [Fact]
    public async Task Handle_TaskOwner_CanRemoveAnotherAssignee()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        var owner = TestDataFactory.CreateUser("owner@example.com");
        var victim = TestDataFactory.CreateUser("victim@example.com");
        context.WorkTasks.Add(task);
        context.Users.AddRange(owner, victim);
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, owner.Id, TaskAssigneeRole.Owner));
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, victim.Id, TaskAssigneeRole.Assignee));
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(owner.Id, SystemRole.Member);
        var handler = new RemoveTaskAssigneeCommandHandler(context, currentUser);

        await handler.Handle(new RemoveTaskAssigneeCommand(task.Id, victim.Id), default);

        Assert.Empty(context.TaskAssignees.Where(a => a.WorkTaskId == task.Id && a.UserId == victim.Id));
    }
}
