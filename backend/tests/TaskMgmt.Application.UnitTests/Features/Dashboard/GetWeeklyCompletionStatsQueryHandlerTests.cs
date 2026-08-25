using TaskMgmt.Application.Features.Dashboard.Queries.GetWeeklyCompletionStats;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Entities;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.UnitTests.Features.Dashboard;

public class GetWeeklyCompletionStatsQueryHandlerTests
{
    private static TaskHistory CreateHistory(Guid workTaskId, string fieldName, string newValue, DateTimeOffset createdAtUtc) => new()
    {
        WorkTaskId = workTaskId,
        ActionType = TaskHistoryActionType.StatusChanged,
        Description = "Test history entry",
        FieldName = fieldName,
        NewValue = newValue,
        CreatedAtUtc = createdAtUtc,
    };

    [Fact]
    public async Task Handle_NoHistory_ReturnsEightWeeksAllZero()
    {
        using var context = TestDbContextFactory.Create();
        var handler = new GetWeeklyCompletionStatsQueryHandler(context, new FakeCacheService());

        var result = await handler.Handle(new GetWeeklyCompletionStatsQuery(), default);

        Assert.Equal(8, result.Count);
        Assert.All(result, w => Assert.Equal(0, w.CompletedCount));
    }

    [Fact]
    public async Task Handle_CountsStatusDoneChangeInCurrentWeek()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        context.WorkTasks.Add(task);
        context.TaskHistories.Add(CreateHistory(task.Id, "Status", "Done", DateTimeOffset.UtcNow));
        await context.SaveChangesAsync(default);

        var handler = new GetWeeklyCompletionStatsQueryHandler(context, new FakeCacheService());
        var result = await handler.Handle(new GetWeeklyCompletionStatsQuery(), default);

        Assert.Equal(1, result[^1].CompletedCount);
        Assert.Equal(1, result.Sum(w => w.CompletedCount));
    }

    [Fact]
    public async Task Handle_IgnoresNonStatusFieldChanges()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        context.WorkTasks.Add(task);
        context.TaskHistories.Add(CreateHistory(task.Id, "Title", "Done", DateTimeOffset.UtcNow));
        await context.SaveChangesAsync(default);

        var handler = new GetWeeklyCompletionStatsQueryHandler(context, new FakeCacheService());
        var result = await handler.Handle(new GetWeeklyCompletionStatsQuery(), default);

        Assert.Equal(0, result.Sum(w => w.CompletedCount));
    }

    [Fact]
    public async Task Handle_IgnoresStatusChangesToNonDoneValues()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        context.WorkTasks.Add(task);
        context.TaskHistories.Add(CreateHistory(task.Id, "Status", "InProgress", DateTimeOffset.UtcNow));
        await context.SaveChangesAsync(default);

        var handler = new GetWeeklyCompletionStatsQueryHandler(context, new FakeCacheService());
        var result = await handler.Handle(new GetWeeklyCompletionStatsQuery(), default);

        Assert.Equal(0, result.Sum(w => w.CompletedCount));
    }

    [Fact]
    public async Task Handle_ExcludesInactiveTasks()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        task.IsActive = false;
        context.WorkTasks.Add(task);
        context.TaskHistories.Add(CreateHistory(task.Id, "Status", "Done", DateTimeOffset.UtcNow));
        await context.SaveChangesAsync(default);

        var handler = new GetWeeklyCompletionStatsQueryHandler(context, new FakeCacheService());
        var result = await handler.Handle(new GetWeeklyCompletionStatsQuery(), default);

        Assert.Equal(0, result.Sum(w => w.CompletedCount));
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
        context.TaskHistories.Add(CreateHistory(inLocation.Id, "Status", "Done", DateTimeOffset.UtcNow));
        context.TaskHistories.Add(CreateHistory(elsewhere.Id, "Status", "Done", DateTimeOffset.UtcNow));
        await context.SaveChangesAsync(default);

        var handler = new GetWeeklyCompletionStatsQueryHandler(context, new FakeCacheService());
        var result = await handler.Handle(new GetWeeklyCompletionStatsQuery(location.Id), default);

        Assert.Equal(1, result.Sum(w => w.CompletedCount));
    }

    [Fact]
    public async Task Handle_ExcludesCompletionsOlderThanEightWeekWindow()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        context.WorkTasks.Add(task);
        context.TaskHistories.Add(CreateHistory(task.Id, "Status", "Done", DateTimeOffset.UtcNow.AddDays(-90)));
        await context.SaveChangesAsync(default);

        var handler = new GetWeeklyCompletionStatsQueryHandler(context, new FakeCacheService());
        var result = await handler.Handle(new GetWeeklyCompletionStatsQuery(), default);

        Assert.Equal(8, result.Count);
        Assert.Equal(0, result.Sum(w => w.CompletedCount));
    }
}
