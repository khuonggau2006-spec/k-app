using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Features.TaskAssignees.Commands.AddTaskAssignee;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.UnitTests.Features.TaskAssignees;

public class AddTaskAssigneeCommandHandlerTests
{
    [Fact]
    public async Task Handle_TaskOwner_CanAddAssignee()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        var owner = TestDataFactory.CreateUser("owner@example.com");
        var newAssignee = TestDataFactory.CreateUser("newassignee@example.com");
        context.WorkTasks.Add(task);
        context.Users.AddRange(owner, newAssignee);
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, owner.Id, TaskAssigneeRole.Owner));
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(owner.Id, SystemRole.Member);
        var handler = new AddTaskAssigneeCommandHandler(context, currentUser);
        var command = new AddTaskAssigneeCommand(task.Id, newAssignee.Id, TaskAssigneeRole.Assignee);

        var result = await handler.Handle(command, default);

        Assert.Equal(newAssignee.Id, result.UserId);
    }

    [Fact]
    public async Task Handle_MemberWithNoRoleOnTask_Throws()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        var newAssignee = TestDataFactory.CreateUser("newassignee@example.com");
        context.WorkTasks.Add(task);
        context.Users.Add(newAssignee);
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(Guid.NewGuid(), SystemRole.Member);
        var handler = new AddTaskAssigneeCommandHandler(context, currentUser);
        var command = new AddTaskAssigneeCommand(task.Id, newAssignee.Id, TaskAssigneeRole.Assignee);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(command, default));

        Assert.Empty(context.TaskAssignees.Where(a => a.WorkTaskId == task.Id));
    }
}
