using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Features.WorkTasks.Commands.DeleteWorkTask;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.UnitTests.Features.WorkTasks;

public class DeleteWorkTaskCommandHandlerTests
{
    [Fact]
    public async Task Handle_TaskOwner_CanDelete()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        var user = TestDataFactory.CreateUser();
        context.WorkTasks.Add(task);
        context.Users.Add(user);
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, user.Id, TaskAssigneeRole.Owner));
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(user.Id, SystemRole.Member);
        var handler = new DeleteWorkTaskCommandHandler(context, currentUser, new FakeCacheService());

        await handler.Handle(new DeleteWorkTaskCommand(task.Id), default);

        var updated = await context.WorkTasks.FindAsync(task.Id);
        Assert.False(updated!.IsActive);
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
        var handler = new DeleteWorkTaskCommandHandler(context, currentUser, new FakeCacheService());

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new DeleteWorkTaskCommand(task.Id), default));

        var unchanged = await context.WorkTasks.FindAsync(task.Id);
        Assert.True(unchanged!.IsActive);
    }
}
