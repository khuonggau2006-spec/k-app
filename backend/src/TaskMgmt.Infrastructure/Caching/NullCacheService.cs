using TaskMgmt.Application.Common.Interfaces;

namespace TaskMgmt.Infrastructure.Caching;

// Dùng khi chưa cấu hình ConnectionStrings:Redis (dev sớm/test) - luôn "cache miss" nên mọi query
// vẫn chạy đúng, chỉ không được tăng tốc, tương tự cách Firebase/Hangfire graceful-degrade.
public class NullCacheService : ICacheService
{
    public Task<(bool Found, T? Value)> TryGetAsync<T>(string key, CancellationToken cancellationToken) =>
        Task.FromResult<(bool, T?)>((false, default));

    public Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task RemoveAsync(string key, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken) => Task.CompletedTask;
}
