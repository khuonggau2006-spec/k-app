using TaskMgmt.Application.Common.Interfaces;

namespace TaskMgmt.Application.Common.Extensions;

public static class CacheServiceExtensions
{
    public static async Task<T> GetOrSetAsync<T>(
        this ICacheService cache, string key, TimeSpan expiration, Func<Task<T>> factory, CancellationToken cancellationToken)
    {
        var (found, cached) = await cache.TryGetAsync<T>(key, cancellationToken);
        if (found)
        {
            return cached!;
        }

        var value = await factory();
        await cache.SetAsync(key, value, expiration, cancellationToken);
        return value;
    }
}
