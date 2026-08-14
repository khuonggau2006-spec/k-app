using TaskMgmt.Application.Features.Dashboard.Queries.GetDashboardStats;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Entities;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.UnitTests.Features.Dashboard;

public class GetDashboardStatsQueryHandlerTests
{
    [Fact]
    public async Task Handle_NoTasks_ReturnsAllZero()
    {
        using var context = TestDbContextFactory.Create();
        var handler = new GetDashboardStatsQueryHandler(context, new FakeCacheService());

        var result = await handler.Handle(new GetDashboardStatsQuery(), default);

        Assert.Equal(0, result.TotalActive);
        Assert.Equal(0, result.OverdueCount);
        Assert.Equal(0, result.DueSoonCount);
    }

    [Fact]
    public async Task Handle_CountsPerStatus()
    {
        using var context = TestDbContextFactory.Create();
        var toDo = TestDataFactory.CreateWorkTask();
        var inProgress = TestDataFactory.CreateWorkTask();
        inProgress.Status = WorkTaskStatus.InProgress;
        var done = TestDataFactory.CreateWorkTask();
        done.Status = WorkTaskStatus.Done;
        context.WorkTasks.AddRange(toDo, inProgress, done);
        await context.SaveChangesAsync(default);

        var handler = new GetDashboardStatsQueryHandler(context, new FakeCacheService());
        var result = await handler.Handle(new GetDashboardStatsQuery(), default);

        Assert.Equal(3, result.TotalActive);
        Assert.Equal(1, result.ToDoCount);
        Assert.Equal(1, result.InProgressCount);
        Assert.Equal(1, result.DoneCount);
    }

    [Fact]
    public async Task Handle_ExcludesInactiveTasks()
    {
        using var context = TestDbContextFactory.Create();
        var active = TestDataFactory.CreateWorkTask();
        var inactive = TestDataFactory.CreateWorkTask();
        inactive.IsActive = false;
        context.WorkTasks.AddRange(active, inactive);
        await context.SaveChangesAsync(default);

        var handler = new GetDashboardStatsQueryHandler(context, new FakeCacheService());
        var result = await handler.Handle(new GetDashboardStatsQuery(), default);

        Assert.Equal(1, result.TotalActive);
    }

    [Fact]
    public async Task Handle_OverdueTask_CountedAsOverdueNotDueSoon()
    {
        using var context = TestDbContextFactory.Create();
        var overdue = TestDataFactory.CreateWorkTask();
        overdue.DueDateUtc = DateTimeOffset.UtcNow.AddDays(-1);
        context.WorkTasks.Add(overdue);
        await context.SaveChangesAsync(default);

        var handler = new GetDashboardStatsQueryHandler(context, new FakeCacheService());
        var result = await handler.Handle(new GetDashboardStatsQuery(), default);

        Assert.Equal(1, result.OverdueCount);
        Assert.Equal(0, result.DueSoonCount);
    }

    [Fact]
    public async Task Handle_DueWithin24Hours_CountedAsDueSoon()
    {
        using var context = TestDbContextFactory.Create();
        var dueSoon = TestDataFactory.CreateWorkTask();
        dueSoon.DueDateUtc = DateTimeOffset.UtcNow.AddHours(2);
        context.WorkTasks.Add(dueSoon);
        await context.SaveChangesAsync(default);

        var handler = new GetDashboardStatsQueryHandler(context, new FakeCacheService());
        var result = await handler.Handle(new GetDashboardStatsQuery(), default);

        Assert.Equal(1, result.DueSoonCount);
        Assert.Equal(0, result.OverdueCount);
    }

    [Fact]
    public async Task Handle_DoneTask_NotCountedAsOverdueEvenIfPastDue()
    {
        using var context = TestDbContextFactory.Create();
        var doneOverdue = TestDataFactory.CreateWorkTask();
        doneOverdue.DueDateUtc = DateTimeOffset.UtcNow.AddDays(-1);
        doneOverdue.Status = WorkTaskStatus.Done;
        context.WorkTasks.Add(doneOverdue);
        await context.SaveChangesAsync(default);

        var handler = new GetDashboardStatsQueryHandler(context, new FakeCacheService());
        var result = await handler.Handle(new GetDashboardStatsQuery(), default);

        Assert.Equal(0, result.OverdueCount);
    }

    [Fact]
    public async Task Handle_FiltersByLocationId()
    {
        using var context = TestDbContextFactory.Create();
        var location = TestDataFactory.CreateLocation();
        context.Locations.Add(location);

        var inLocation = TestDataFactory.CreateWorkTask();
        inLocation.LocationId = location.Id;
        var elsewhere = TestDataFactory.CreateWorkTask();
        context.WorkTasks.AddRange(inLocation, elsewhere);
        await context.SaveChangesAsync(default);

        var handler = new GetDashboardStatsQueryHandler(context, new FakeCacheService());
        var result = await handler.Handle(new GetDashboardStatsQuery(location.Id), default);

        Assert.Equal(1, result.TotalActive);
    }
}
