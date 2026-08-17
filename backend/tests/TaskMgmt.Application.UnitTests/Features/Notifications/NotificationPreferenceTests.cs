using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TaskMgmt.Application.Common.Caching;
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Notifications.Commands.UpdateNotificationPreference;
using TaskMgmt.Application.Features.Notifications.Common;
using TaskMgmt.Application.Features.Notifications.Queries.GetNotificationPreferences;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Enums;
using TaskMgmt.Infrastructure.Persistence;

namespace TaskMgmt.Application.UnitTests.Features.Notifications;

public class NotificationPreferenceTests
{
    [Fact]
    public async Task GetPreferences_NoRowsDisabled_AllTenEnabled()
    {
        var userId = Guid.NewGuid();
        await using var provider = TestServiceProviderFactory.Create(userId, SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new GetNotificationPreferencesQuery());

        Assert.Equal(NotificationTypes.All.Count, result.Count);
        Assert.All(result, p => Assert.True(p.IsEnabled));
        Assert.Equal(NotificationTypes.All.OrderBy(t => t), result.Select(p => p.Type).OrderBy(t => t));
    }

    [Fact]
    public async Task GetPreferences_SomeDisabled_ReflectsState()
    {
        var userId = Guid.NewGuid();
        await using var provider = TestServiceProviderFactory.Create(userId, SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();

        await sender.Send(new UpdateNotificationPreferenceCommand("CommentAdded", false));

        var result = await sender.Send(new GetNotificationPreferencesQuery());

        Assert.False(result.Single(p => p.Type == "CommentAdded").IsEnabled);
        Assert.True(result.Single(p => p.Type == "DueSoon").IsEnabled);
    }

    [Fact]
    public async Task UpdatePreference_DisableTwice_Idempotent()
    {
        var userId = Guid.NewGuid();
        await using var provider = TestServiceProviderFactory.Create(userId, SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();
        var context = provider.GetRequiredService<AppDbContext>();

        await sender.Send(new UpdateNotificationPreferenceCommand("Overdue", false));
        await sender.Send(new UpdateNotificationPreferenceCommand("Overdue", false));

        Assert.Single(context.NotificationPreferences, p => p.UserId == userId && p.Type == "Overdue");
    }

    [Fact]
    public async Task UpdatePreference_DisableThenEnable_RemovesRow()
    {
        var userId = Guid.NewGuid();
        await using var provider = TestServiceProviderFactory.Create(userId, SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();
        var context = provider.GetRequiredService<AppDbContext>();

        await sender.Send(new UpdateNotificationPreferenceCommand("Overdue", false));
        await sender.Send(new UpdateNotificationPreferenceCommand("Overdue", true));

        Assert.Empty(context.NotificationPreferences.Where(p => p.UserId == userId && p.Type == "Overdue"));
    }

    [Fact]
    public async Task UpdatePreference_EnableWithoutExistingRow_Idempotent()
    {
        var userId = Guid.NewGuid();
        await using var provider = TestServiceProviderFactory.Create(userId, SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();

        await sender.Send(new UpdateNotificationPreferenceCommand("Overdue", true));

        var result = await sender.Send(new GetNotificationPreferencesQuery());
        Assert.True(result.Single(p => p.Type == "Overdue").IsEnabled);
    }

    [Fact]
    public async Task UpdatePreference_InvalidType_ThrowsValidationException()
    {
        var userId = Guid.NewGuid();
        await using var provider = TestServiceProviderFactory.Create(userId, SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();

        await Assert.ThrowsAsync<ValidationException>(
            () => sender.Send(new UpdateNotificationPreferenceCommand("KhongTonTai", false)));
    }

    [Fact]
    public async Task UpdatePreference_InvalidatesDisabledTypesCache()
    {
        var userId = Guid.NewGuid();
        await using var provider = TestServiceProviderFactory.Create(userId, SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();
        var cache = provider.GetRequiredService<ICacheService>();

        // Simulate a stale cached "nothing disabled" result, as if an earlier
        // notification had already primed the cache before the user changed
        // their preference.
        await cache.SetAsync(
            CacheKeys.DisabledNotificationTypes(userId),
            new List<string>(),
            CacheKeys.DisabledNotificationTypesExpiration,
            default);

        await sender.Send(new UpdateNotificationPreferenceCommand("Overdue", false));

        var (found, _) = await cache.TryGetAsync<List<string>>(
            CacheKeys.DisabledNotificationTypes(userId), default);
        Assert.False(found);
    }
}
