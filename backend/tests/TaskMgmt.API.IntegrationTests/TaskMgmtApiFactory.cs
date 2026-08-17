using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using TaskMgmt.Application.Common.Models;
using TaskMgmt.Domain.Enums;
using TaskMgmt.Infrastructure.Persistence;

namespace TaskMgmt.API.IntegrationTests;

// Chạy toàn bộ pipeline HTTP thật (routing, [Authorize], JWT) trong bộ nhớ (TestServer), chỉ
// thay AppDbContext bằng EF InMemory để không cần PostgreSQL thật khi chạy test. Dùng cho các
// test HTTP request/response thuần tuý (không cần WebSocket/LongPolling như SignalR).
public class TaskMgmtApiFactory : WebApplicationFactory<Program>
{
    private const string JwtSecret = "integration-test-only-jwt-signing-key-do-not-use-in-prod";
    private const string JwtIssuer = "TaskMgmt.API";
    private const string JwtAudience = "TaskMgmt.Client";

    private readonly string _dbName = $"TaskMgmtApiTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = JwtSecret,
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Jwt:AccessTokenExpirationMinutes"] = "30",
                ["Jwt:RefreshTokenExpirationDays"] = "14",
                ["Storage:Endpoint"] = "http://localhost:9000",
                ["Storage:AccessKey"] = "test",
                ["Storage:SecretKey"] = "test",
                ["Storage:BucketName"] = "test-bucket",
                // Giá trị giả có chủ đích: AddInfrastructure() ném lỗi ngay nếu thiếu key này, TRƯỚC
                // khi ConfigureServices bên dưới kịp thay AppDbContext bằng EF InMemory - nên chỉ cần
                // một chuỗi hợp lệ về cú pháp để qua bước đăng ký UseNpgsql, không cần Postgres thật
                // đang chạy (không có bước nào thực sự kết nối tới đây).
                ["ConnectionStrings:Postgres"] = "Host=localhost;Database=taskmgmt-integration-tests-unused;Username=test;Password=test",
                // Rỗng có chủ đích: SignalR chạy in-process trong 1 TestServer duy nhất, backplane
                // Redis chỉ có ý nghĩa khi fan-out giữa nhiều server thật nên không cần cho test.
                ["ConnectionStrings:Redis"] = "",
                // Hangfire cần PostgreSQL thật (không có provider InMemory) - tắt hẳn khi test để
                // không âm thầm phụ thuộc Docker/Postgres đang chạy (đã từng làm test host crash
                // khi Postgres tắt).
                ["Hangfire:Disabled"] = "true",
            });
        });

        builder.ConfigureServices(services =>
        {
            // AddInfrastructure() đã đăng ký AppDbContext với UseNpgsql - phải gỡ cả
            // DbContextOptions lẫn IDbContextOptionsConfiguration (EF Core gộp config từ nhiều
            // nguồn), nếu không cả 2 provider (Npgsql + InMemory) cùng tồn tại và EF Core báo lỗi.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_dbName));
        });
    }

    // Ký token bằng đúng JwtSettings mà host test đang chạy thực sự dùng để validate (lấy từ
    // DI container), thay vì đoán config nào đã "thắng" - tránh sai lệch do thứ tự nguồn config.
    public string GenerateToken(Guid userId, SystemRole role)
    {
        var jwtSettings = Services.GetRequiredService<JwtSettings>();

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(ClaimTypes.Role, role.ToString()),
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings.Issuer,
            audience: jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
