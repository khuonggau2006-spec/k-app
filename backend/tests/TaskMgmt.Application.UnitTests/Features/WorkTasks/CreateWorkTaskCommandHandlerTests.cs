using TaskMgmt.Application.Features.WorkTasks.Commands.CreateWorkTask;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.UnitTests.Features.WorkTasks;

public class CreateWorkTaskCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatorAutomaticallyBecomesOwner()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(user.Id, SystemRole.Member);
        var handler = new CreateWorkTaskCommandHandler(context, currentUser);
        var command = new CreateWorkTaskCommand("New Task", null, null, null, null);

        var result = await handler.Handle(command, default);

        var assignees = context.TaskAssignees.Where(a => a.WorkTaskId == result.Id).ToList();
        var owner = Assert.Single(assignees);
        Assert.Equal(user.Id, owner.UserId);
        Assert.Equal(TaskAssigneeRole.Owner, owner.Role);
    }

    [Fact]
    public async Task Handle_NoCurrentUser_DoesNotCreateAssignee()
    {
        using var context = TestDbContextFactory.Create();
        var currentUser = new FakeCurrentUserService(null, null);
        var handler = new CreateWorkTaskCommandHandler(context, currentUser);
        var command = new CreateWorkTaskCommand("New Task", null, null, null, null);

        var result = await handler.Handle(command, default);

        Assert.Empty(context.TaskAssignees.Where(a => a.WorkTaskId == result.Id));
    }
}
