using System.Text.Json;
using StackExchange.Redis;
using TaskMgmt.Application.Common.Interfaces;

namespace TaskMgmt.Infrastructure.Caching;

public class RedisCacheService(IConnectionMultiplexer connectionMultiplexer) : ICacheService
{
    private IDatabase Database => connectionMultiplexer.GetDatabase();

    public async Task<(bool Found, T? Value)> TryGetAsync<T>(string key, CancellationToken cancellationToken)
    {
        var value = await Database.StringGetAsync(key);
        return value.IsNullOrEmpty ? (false, default) : (true, JsonSerializer.Deserialize<T>((string)value!));
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken) =>
        Database.StringSetAsync(key, JsonSerializer.Serialize(value), expiration);

    public Task RemoveAsync(string key, CancellationToken cancellationToken) => Database.KeyDeleteAsync(key);

    // Không có lệnh Redis nào xoá theo pattern trực tiếp - phải SCAN ra key rồi DEL hàng loạt.
    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken)
    {
        var server = connectionMultiplexer.GetServer(connectionMultiplexer.GetEndPoints()[0]);
        var keys = server.Keys(pattern: $"{prefix}*").ToArray();
        if (keys.Length > 0)
        {
            await Database.KeyDeleteAsync(keys);
        }
    }
}
