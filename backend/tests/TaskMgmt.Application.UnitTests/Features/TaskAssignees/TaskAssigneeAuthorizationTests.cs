using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Features.TaskAssignees.Common;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.UnitTests.Features.TaskAssignees;

public class TaskAssigneeAuthorizationTests
{
    [Fact]
    public async Task EnsureCanManageAsync_Admin_DoesNotThrow_EvenWithoutTaskRole()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        context.WorkTasks.Add(task);
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(Guid.NewGuid(), SystemRole.Admin);

        await TaskAssigneeAuthorization.EnsureCanManageAsync(context, currentUser, task.Id, default);
    }

    [Fact]
    public async Task EnsureCanManageAsync_Manager_DoesNotThrow_EvenWithoutTaskRole()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        context.WorkTasks.Add(task);
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(Guid.NewGuid(), SystemRole.Manager);

        await TaskAssigneeAuthorization.EnsureCanManageAsync(context, currentUser, task.Id, default);
    }

    [Fact]
    public async Task EnsureCanManageAsync_MemberWhoIsTaskOwner_DoesNotThrow()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        var user = TestDataFactory.CreateUser();
        context.WorkTasks.Add(task);
        context.Users.Add(user);
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, user.Id, TaskAssigneeRole.Owner));
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(user.Id, SystemRole.Member);

        await TaskAssigneeAuthorization.EnsureCanManageAsync(context, currentUser, task.Id, default);
    }

    [Fact]
    public async Task EnsureCanManageAsync_MemberWhoIsOnlyAssignee_Throws()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        var user = TestDataFactory.CreateUser();
        context.WorkTasks.Add(task);
        context.Users.Add(user);
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, user.Id, TaskAssigneeRole.Assignee));
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(user.Id, SystemRole.Member);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => TaskAssigneeAuthorization.EnsureCanManageAsync(context, currentUser, task.Id, default));
    }

    [Fact]
    public async Task EnsureCanManageAsync_MemberNotOnTask_Throws()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        context.WorkTasks.Add(task);
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(Guid.NewGuid(), SystemRole.Member);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => TaskAssigneeAuthorization.EnsureCanManageAsync(context, currentUser, task.Id, default));
    }
}
