using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Features.WorkTasks.Commands.UpdateWorkTask;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.UnitTests.Features.WorkTasks;

public class UpdateWorkTaskCommandHandlerTests
{
    [Fact]
    public async Task Handle_TaskOwner_CanUpdate()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        var user = TestDataFactory.CreateUser();
        context.WorkTasks.Add(task);
        context.Users.Add(user);
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, user.Id, TaskAssigneeRole.Owner));
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(user.Id, SystemRole.Member);
        var handler = new UpdateWorkTaskCommandHandler(context, currentUser, new FakeCacheService());
        var command = new UpdateWorkTaskCommand(task.Id, "Updated title", null, WorkTaskStatus.InProgress, null, null, null);

        var result = await handler.Handle(command, default);

        Assert.Equal("Updated title", result.Title);
    }

    [Fact]
    public async Task Handle_ManagerNotOnTask_CanUpdate()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        context.WorkTasks.Add(task);
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(Guid.NewGuid(), SystemRole.Manager);
        var handler = new UpdateWorkTaskCommandHandler(context, currentUser, new FakeCacheService());
        var command = new UpdateWorkTaskCommand(task.Id, "Updated title", null, WorkTaskStatus.InProgress, null, null, null);

        var result = await handler.Handle(command, default);

        Assert.Equal("Updated title", result.Title);
    }

    [Fact]
    public async Task Handle_MemberWhoIsOnlyAssignee_Throws()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        var user = TestDataFactory.CreateUser();
        context.WorkTasks.Add(task);
        context.Users.Add(user);
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, user.Id, TaskAssigneeRole.Assignee));
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(user.Id, SystemRole.Member);
        var handler = new UpdateWorkTaskCommandHandler(context, currentUser, new FakeCacheService());
        var command = new UpdateWorkTaskCommand(task.Id, "Updated title", null, WorkTaskStatus.InProgress, null, null, null);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(command, default));
    }

    [Fact]
    public async Task Handle_MemberNotOnTask_Throws()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        context.WorkTasks.Add(task);
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(Guid.NewGuid(), SystemRole.Member);
        var handler = new UpdateWorkTaskCommandHandler(context, currentUser, new FakeCacheService());
        var command = new UpdateWorkTaskCommand(task.Id, "Updated title", null, WorkTaskStatus.InProgress, null, null, null);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(command, default));
    }
}
