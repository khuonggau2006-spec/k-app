using TaskMgmt.Application.Features.TaskAssignees.Queries.GetTaskAssignees;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.UnitTests.Features.TaskAssignees;

public class GetTaskAssigneesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsUserHasAvatarTrue_WhenAssigneeHasAvatarStorageKey()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        var user = TestDataFactory.CreateUser();
        user.AvatarStorageKey = "avatars/x/y.jpg";
        context.WorkTasks.Add(task);
        context.Users.Add(user);
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, user.Id, TaskAssigneeRole.Assignee));
        await context.SaveChangesAsync(default);

        var handler = new GetTaskAssigneesQueryHandler(context);
        var result = await handler.Handle(new GetTaskAssigneesQuery(task.Id), default);

        Assert.True(result.Single().UserHasAvatar);
    }
}
