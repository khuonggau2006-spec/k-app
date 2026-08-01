using System.Text;
using System.Text.Json.Serialization;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
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

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapHub<NotificationHub>("/hubs/notifications");
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireDashboardAuthorizationFilter()],
});

// Đăng ký lịch chạy - AddOrUpdate là idempotent, gọi lại mỗi lần khởi động không tạo trùng job.
if (!app.Configuration.GetValue<bool>("Hangfire:Disabled"))
{
    RecurringJob.AddOrUpdate<SendDueSoonReminderJob>(
        "send-due-soon-reminders", job => job.ExecuteAsync(), Cron.Hourly);
    RecurringJob.AddOrUpdate<SendOverdueReminderJob>(
        "send-overdue-reminders", job => job.ExecuteAsync(), Cron.Hourly);
    RecurringJob.AddOrUpdate<CleanupExpiredTokensJob>(
        "cleanup-expired-tokens", job => job.ExecuteAsync(), Cron.Daily);
}

app.Run();
