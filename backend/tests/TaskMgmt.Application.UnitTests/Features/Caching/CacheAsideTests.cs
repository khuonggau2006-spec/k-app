using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TaskMgmt.Application.Common.Caching;
using TaskMgmt.Application.Features.Locations.Commands.CreateLocation;
using TaskMgmt.Application.Features.Locations.Commands.UpdateLocation;
using TaskMgmt.Application.Features.Locations.Queries.GetLocationById;
using TaskMgmt.Application.Features.Locations.Queries.GetLocations;
using TaskMgmt.Application.Features.WorkTasks.Commands.CreateWorkTask;
using TaskMgmt.Application.Features.WorkTasks.Commands.DeleteWorkTask;
using TaskMgmt.Application.Features.WorkTasks.Commands.UpdateWorkTask;
using TaskMgmt.Application.Features.WorkTasks.Queries.GetWorkTaskById;
using TaskMgmt.Application.Features.WorkTasks.Queries.GetWorkTasks;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Enums;
using TaskMgmt.Infrastructure.Persistence;

namespace TaskMgmt.Application.UnitTests.Features.Caching;

// Test end-to-end (MediatR thật + FakeCacheService hoạt động thật, không phải no-op) để xác nhận
// cache-aside: đọc lần 2 lấy từ cache (kể cả khi DB đã đổi "sau lưng" cache), và bị invalidate
// đúng key/prefix ngay sau khi có lệnh ghi tương ứng.
public class CacheAsideTests
{
    [Fact]
    public async Task GetWorkTaskById_ReturnsStaleCacheUntilUpdateInvalidatesIt()
    {
        await using var provider = TestServiceProviderFactory.Create(Guid.NewGuid(), SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();
        var context = provider.GetRequiredService<AppDbContext>();

        var task = await sender.Send(new CreateWorkTaskCommand("Original", null, null, null, null));
        var first = await sender.Send(new GetWorkTaskByIdQuery(task.Id));
        Assert.Equal("Original", first.Title);

        // Đổi thẳng DB, KHÔNG qua command handler -> cache không bị invalidate.
        var entity = context.WorkTasks.Single(t => t.Id == task.Id);
        entity.Title = "Changed behind the cache";
        await context.SaveChangesAsync(default);

        var stale = await sender.Send(new GetWorkTaskByIdQuery(task.Id));
        Assert.Equal("Original", stale.Title);

        await sender.Send(new UpdateWorkTaskCommand(task.Id, "Updated via command", null, WorkTaskStatus.ToDo, null, null, null));

        var fresh = await sender.Send(new GetWorkTaskByIdQuery(task.Id));
        Assert.Equal("Updated via command", fresh.Title);
    }

    [Fact]
    public async Task DeleteWorkTask_InvalidatesDetailAndListCache()
    {
        await using var provider = TestServiceProviderFactory.Create(Guid.NewGuid(), SystemRole.Admin);
        var sender = provider.GetRequiredService<ISender>();
        var cache = (FakeCacheService)provider.GetRequiredService<Application.Common.Interfaces.ICacheService>();

        var task = await sender.Send(new CreateWorkTaskCommand("To delete", null, null, null, null));
        await sender.Send(new GetWorkTaskByIdQuery(task.Id));
        await sender.Send(new GetWorkTasksQuery());

        Assert.Contains(CacheKeys.WorkTaskDetail(task.Id), cache.Keys);
        Assert.Contains(cache.Keys, k => k.StartsWith(CacheKeys.WorkTaskListPrefix, StringComparison.Ordinal));

        await sender.Send(new DeleteWorkTaskCommand(task.Id));

        Assert.DoesNotContain(CacheKeys.WorkTaskDetail(task.Id), cache.Keys);
        Assert.DoesNotContain(cache.Keys, k => k.StartsWith(CacheKeys.WorkTaskListPrefix, StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetLocationById_ReturnsStaleCacheUntilUpdateInvalidatesIt()
    {
        await using var provider = TestServiceProviderFactory.Create(Guid.NewGuid(), SystemRole.Admin);
        var sender = provider.GetRequiredService<ISender>();
        var context = provider.GetRequiredService<AppDbContext>();

        var location = await sender.Send(new CreateLocationCommand("Original", null, 10.0, 20.0, null));
        var first = await sender.Send(new GetLocationByIdQuery(location.Id));
        Assert.Equal("Original", first.Name);

        var entity = context.Locations.Single(l => l.Id == location.Id);
        entity.Name = "Changed behind the cache";
        await context.SaveChangesAsync(default);

        var stale = await sender.Send(new GetLocationByIdQuery(location.Id));
        Assert.Equal("Original", stale.Name);

        await sender.Send(new UpdateLocationCommand(location.Id, "Updated via command", null, 10.0, 20.0, 100, true, null));

        var fresh = await sender.Send(new GetLocationByIdQuery(location.Id));
        Assert.Equal("Updated via command", fresh.Name);
    }

    [Fact]
    public async Task CreateLocation_InvalidatesLocationListCache()
    {
        await using var provider = TestServiceProviderFactory.Create(Guid.NewGuid(), SystemRole.Admin);
        var sender = provider.GetRequiredService<ISender>();
        var cache = (FakeCacheService)provider.GetRequiredService<Application.Common.Interfaces.ICacheService>();

        await sender.Send(new GetLocationsQuery());
        Assert.Contains(CacheKeys.LocationListKey, cache.Keys);

        await sender.Send(new CreateLocationCommand("New location", null, 1.0, 2.0, null));

        Assert.DoesNotContain(CacheKeys.LocationListKey, cache.Keys);
    }
}
