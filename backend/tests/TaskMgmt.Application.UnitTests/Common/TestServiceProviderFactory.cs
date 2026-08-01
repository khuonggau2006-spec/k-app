using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Common.Models;
using TaskMgmt.Domain.Enums;
using TaskMgmt.Infrastructure.Persistence;
using TaskMgmt.Infrastructure.Persistence.Interceptors;

namespace TaskMgmt.Application.UnitTests.Common;

// Dựng lại đúng dây chuyền DI thật (AddApplication + DbContext có gắn DispatchDomainEventsInterceptor)
// để test domain event -> TaskHistory theo kiểu end-to-end, thay vì chỉ test hàm thuần.
internal static class TestServiceProviderFactory
{
    public static ServiceProvider Create(Guid? currentUserId = null, SystemRole? currentUserRole = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();

        services.AddScoped<DispatchDomainEventsInterceptor>();
        var dbName = Guid.NewGuid().ToString();
        services.AddDbContext<AppDbContext>((provider, options) => options
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(provider.GetRequiredService<DispatchDomainEventsInterceptor>()));
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        services.AddScoped<ICurrentUserService>(_ => new FakeCurrentUserService(currentUserId, currentUserRole));

        services.AddSingleton(new StorageSettings
        {
            Endpoint = "http://localhost:9000",
            AccessKey = "test",
            SecretKey = "test",
            BucketName = "test-bucket",
        });
        services.AddSingleton<IFileStorageService, FakeFileStorageService>();

        services.AddSingleton<IRealtimeNotifier, FakeRealtimeNotifier>();
        services.AddSingleton<IBackgroundJobScheduler, FakeBackgroundJobScheduler>();
        services.AddSingleton<ICacheService, FakeCacheService>();

        return services.BuildServiceProvider();
    }
}
