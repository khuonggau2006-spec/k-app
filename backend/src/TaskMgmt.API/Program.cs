using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using TaskMgmt.API.Authorization;
using TaskMgmt.API.Hubs;
using TaskMgmt.API.Middleware;
using TaskMgmt.API.Realtime;
using TaskMgmt.API.Services;
using TaskMgmt.Application;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Common.Models;
using TaskMgmt.Domain.Enums;
using TaskMgmt.Infrastructure;
using TaskMgmt.Infrastructure.BackgroundJobs;

var builder = WebApplication.CreateBuilder(args);

// Log tập trung: luôn ghi ra console; nếu có cấu hình Seq:ServerUrl (log tập trung, xem
// docker-compose.prod.yml service "seq") thì ghi thêm sang đó - không cấu hình thì bỏ qua êm ái,
// cùng cách graceful-degrade với Firebase/Storage khi thiếu config.
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console();

    var seqServerUrl = context.Configuration["Seq:ServerUrl"];
    if (!string.IsNullOrWhiteSpace(seqServerUrl))
    {
        configuration.WriteTo.Seq(seqServerUrl);
    }
});

// Cảnh báo lỗi (Sentry): chỉ bật khi có Sentry:Dsn thật trong config - SDK tự vô hiệu hoá êm ái
// khi Dsn rỗng (không throw, không có network call nào), cùng cách graceful-degrade với
// Firebase/Storage/Seq phía trên khi thiếu cấu hình.
var sentryDsn = builder.Configuration["Sentry:Dsn"];
if (!string.IsNullOrWhiteSpace(sentryDsn))
{
    builder.WebHost.UseSentry(options =>
    {
        options.Dsn = sentryDsn;
        options.Environment = builder.Environment.EnvironmentName;
        options.TracesSampleRate = 0.2; // 20% request được lấy mẫu để theo dõi hiệu năng, tránh gửi quá nhiều dữ liệu.
    });
}

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập access token (không cần tiền tố \"Bearer \").",
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", document), [] },
    });
});
const string DevCorsPolicy = "DevCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(DevCorsPolicy, policy => policy
        .SetIsOriginAllowed(_ => true)
        .AllowAnyMethod()
        .AllowAnyHeader());
});

// Chống brute-force cho các endpoint xác thực (login/register/refresh-token là [AllowAnonymous],
// không bị chặn bởi JWT nên cần giới hạn số lần thử theo IP). 10 request/phút/IP là đủ rộng cho
// người dùng thật gõ sai vài lần, nhưng chặn được script dò mật khẩu tự động.
const string AuthRateLimiterPolicy = "auth";
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(AuthRateLimiterPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 10,
                QueueLimit = 0,
            }));
});

builder.Services.AddHealthChecks();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();

// Redis backplane cho khả năng scale nhiều instance API - tuỳ chọn: nếu chưa cấu hình Redis
// (dev sớm/chưa cần scale) thì SignalR vẫn chạy bình thường, chỉ không fan-out được qua nhiều server.
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
var signalRBuilder = builder.Services.AddSignalR();
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    signalRBuilder.AddStackExchangeRedis(redisConnectionString, options => options.Configuration.ChannelPrefix =
        StackExchange.Redis.RedisChannel.Literal("TaskMgmt.SignalR"));
}

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Configuration section 'Jwt' not found.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
        options.Events = new JwtBearerEvents
        {
            // SignalR qua WebSocket và trình duyệt điều hướng thẳng tới Hangfire Dashboard đều
            // không gửi được header Authorization - theo đúng khuyến nghị chính thức của ASP.NET
            // Core, đọc token từ query string "access_token" nhưng CHỈ cho 2 path này, tránh mở
            // rộng cách xác thực này sang các API REST khác.
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken)
                    && (path.StartsWithSegments("/hubs") || path.StartsWithSegments("/hangfire")))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddSingleton<IAuthorizationHandler, SystemRoleAuthorizationHandler>();
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy("RequireManager", policy => policy.Requirements.Add(new SystemRoleRequirement(SystemRole.Manager)));
    options.AddPolicy("RequireAdmin", policy => policy.Requirements.Add(new SystemRoleRequirement(SystemRole.Admin)));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors(DevCorsPolicy);
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapHub<NotificationHub>("/hubs/notifications");

// Dashboard + lịch chạy đều cần service Hangfire đã đăng ký ở AddInfrastructure() - cùng điều
// kiện "Hangfire:Disabled" với bên đó, nếu không UseHangfireDashboard sẽ báo thiếu service khi
// AddHangfire() không chạy (đúng chỗ này từng bị lệch điều kiện, chỉ lộ ra khi test thật sự tắt
// được Hangfire).
if (!app.Configuration.GetValue<bool>("Hangfire:Disabled"))
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new HangfireDashboardAuthorizationFilter()],
    });

    // Đăng ký lịch chạy - AddOrUpdate là idempotent, gọi lại mỗi lần khởi động không tạo trùng job.
    RecurringJob.AddOrUpdate<SendDueSoonReminderJob>(
        "send-due-soon-reminders", job => job.ExecuteAsync(), Cron.Hourly);
    RecurringJob.AddOrUpdate<SendOverdueReminderJob>(
        "send-overdue-reminders", job => job.ExecuteAsync(), Cron.Hourly);
    RecurringJob.AddOrUpdate<CleanupExpiredTokensJob>(
        "cleanup-expired-tokens", job => job.ExecuteAsync(), Cron.Daily);
}

app.Run();
