using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Common.Models;
using TaskMgmt.Infrastructure.BackgroundJobs;
using TaskMgmt.Infrastructure.Caching;
using TaskMgmt.Infrastructure.Identity;
using TaskMgmt.Infrastructure.Notifications;
using TaskMgmt.Infrastructure.Persistence;
using TaskMgmt.Infrastructure.Persistence.Interceptors;
using TaskMgmt.Infrastructure.Storage;

namespace TaskMgmt.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' not found.");

        services.AddScoped<DispatchDomainEventsInterceptor>();
        services.AddDbContext<AppDbContext>((provider, options) => options
            .UseNpgsql(connectionString)
            .AddInterceptors(provider.GetRequiredService<DispatchDomainEventsInterceptor>()));
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("Configuration section 'Jwt' not found.");
        services.AddSingleton(jwtSettings);

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        var storageSettings = configuration.GetSection(StorageSettings.SectionName).Get<StorageSettings>()
            ?? throw new InvalidOperationException("Configuration section 'Storage' not found.");
        services.AddSingleton(storageSettings);
        services.AddSingleton<IFileStorageService, S3FileStorageService>();

        // Firebase là tuỳ chọn: nếu chưa cấu hình CredentialsPath (chưa có project Firebase thật)
        // thì tắt tính năng push một cách êm ái thay vì làm sập ứng dụng lúc khởi động.
        var firebaseSettings = configuration.GetSection(FirebaseSettings.SectionName).Get<FirebaseSettings>();
        FirebaseApp? firebaseApp = null;
        if (!string.IsNullOrWhiteSpace(firebaseSettings?.CredentialsPath) && File.Exists(firebaseSettings.CredentialsPath))
        {
            const string appName = "TaskMgmt";
            try
            {
                firebaseApp = FirebaseApp.GetInstance(appName);
            }
            catch (Exception)
            {
                firebaseApp = FirebaseApp.Create(
                    new AppOptions { Credential = GoogleCredential.FromFile(firebaseSettings.CredentialsPath) },
                    appName);
            }
        }

        services.AddSingleton(new FirebaseAppAccessor(firebaseApp));
        services.AddScoped<IPushNotificationService, FirebaseFcmPushNotificationService>();

        // Storage dùng chung PostgreSQL với dữ liệu nghiệp vụ (schema riêng "hangfire", tự tạo
        // bảng khi khởi động lần đầu) - không cần thêm hạ tầng nào khác.
        // "Hangfire:Disabled" chỉ dùng để tắt trong test (WebApplicationFactory + EF InMemory
        // không có Postgres thật) - ứng dụng chạy thật luôn bật, không đọc key này trong config gốc.
        if (!configuration.GetValue<bool>("Hangfire:Disabled"))
        {
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));
            services.AddHangfireServer();
            services.AddScoped<IBackgroundJobScheduler, HangfireBackgroundJobScheduler>();
        }
        else
        {
            services.AddScoped<IBackgroundJobScheduler, NoOpBackgroundJobScheduler>();
        }

        // Redis là tuỳ chọn: chưa cấu hình (dev sớm/test) thì cache-aside tắt êm ái (luôn cache
        // miss, query thẳng DB) thay vì làm sập ứng dụng - cùng triết lý với Firebase/Hangfire ở trên.
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
            services.AddSingleton<ICacheService, RedisCacheService>();
        }
        else
        {
            services.AddSingleton<ICacheService, NullCacheService>();
        }

        return services;
    }
}
