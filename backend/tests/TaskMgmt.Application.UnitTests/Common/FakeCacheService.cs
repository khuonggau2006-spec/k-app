using System.Collections.Concurrent;
using TaskMgmt.Application.Common.Interfaces;

namespace TaskMgmt.Application.UnitTests.Common;

// Cache in-memory THẬT SỰ hoạt động (không phải no-op) để test có thể xác nhận đúng hành vi
// cache-aside: lần đọc thứ 2 lấy từ cache, và bị xoá đúng key/prefix sau khi ghi.
internal class FakeCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, (object? Value, DateTimeOffset ExpiresAtUtc)> _store = new();

    public IReadOnlyCollection<string> Keys => _store.Keys.ToList();

    public Task<(bool Found, T? Value)> TryGetAsync<T>(string key, CancellationToken cancellationToken)
    {
        if (_store.TryGetValue(key, out var entry) && entry.ExpiresAtUtc > DateTimeOffset.UtcNow)
        {
            return Task.FromResult<(bool, T?)>((true, (T?)entry.Value));
        }

        _store.TryRemove(key, out _);
        return Task.FromResult<(bool, T?)>((false, default));
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken)
    {
        _store[key] = (value, DateTimeOffset.UtcNow.Add(expiration));
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken)
    {
        foreach (var key in _store.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)))
        {
            _store.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }
}
