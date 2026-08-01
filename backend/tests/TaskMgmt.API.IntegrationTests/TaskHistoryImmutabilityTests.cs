using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskMgmt.Domain.Entities;
using TaskMgmt.Domain.Enums;
using TaskMgmt.Infrastructure.Persistence;

namespace TaskMgmt.API.IntegrationTests;

// Mục 2.8 kế hoạch: đảm bảo lịch sử (TaskHistory) không thể sửa/xoá qua API, kể cả Admin.
// Test chạy qua pipeline HTTP thật (routing + [Authorize] + JWT), không chỉ gọi thẳng handler,
// để chứng minh không tồn tại route nào cho phép sửa/xoá - kể cả khi caller có quyền Admin.
public class TaskHistoryImmutabilityTests : IClassFixture<TaskMgmtApiFactory>
{
    private readonly TaskMgmtApiFactory _factory;

    public TaskHistoryImmutabilityTests(TaskMgmtApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient Client, Guid TaskId, Guid HistoryId)> CreateAuthenticatedClientWithSeededHistoryAsync(
        SystemRole callerRole)
    {
        Guid taskId;
        Guid historyId;

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var task = new WorkTask
            {
                Title = "Task cho test immutability",
                Status = WorkTaskStatus.ToDo,
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            };
            context.WorkTasks.Add(task);

            var history = new TaskHistory
            {
                WorkTaskId = task.Id,
                ActionType = TaskHistoryActionType.Created,
                Description = "Công việc được tạo.",
                CreatedAtUtc = DateTimeOffset.UtcNow,
            };
            context.TaskHistories.Add(history);

            await context.SaveChangesAsync();
            taskId = task.Id;
            historyId = history.Id;
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _factory.GenerateToken(Guid.NewGuid(), callerRole));

        return (client, taskId, historyId);
    }

    [Fact]
    public async Task GetHistory_ReturnsSeededEntry()
    {
        var (client, taskId, _) = await CreateAuthenticatedClientWithSeededHistoryAsync(SystemRole.Member);

        var response = await client.GetAsync($"/api/v1/worktasks/{taskId}/history");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Công việc được tạo.", body);
    }

    [Theory]
    [InlineData(SystemRole.Member)]
    [InlineData(SystemRole.Manager)]
    [InlineData(SystemRole.Admin)]
    public async Task PutHistory_NoSuchEndpoint_EvenForAdmin(SystemRole callerRole)
    {
        var (client, taskId, historyId) = await CreateAuthenticatedClientWithSeededHistoryAsync(callerRole);

        var response = await client.PutAsJsonAsync(
            $"/api/v1/worktasks/{taskId}/history/{historyId}", new { description = "Đã bị sửa" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(SystemRole.Member)]
    [InlineData(SystemRole.Manager)]
    [InlineData(SystemRole.Admin)]
    public async Task DeleteHistory_NoSuchEndpoint_EvenForAdmin(SystemRole callerRole)
    {
        var (client, taskId, historyId) = await CreateAuthenticatedClientWithSeededHistoryAsync(callerRole);

        var response = await client.DeleteAsync($"/api/v1/worktasks/{taskId}/history/{historyId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PatchHistory_NoSuchEndpoint_EvenForAdmin()
    {
        var (client, taskId, historyId) = await CreateAuthenticatedClientWithSeededHistoryAsync(SystemRole.Admin);

        var response = await client.PatchAsync($"/api/v1/worktasks/{taskId}/history/{historyId}", JsonContent.Create(new { }));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostHistory_NoSuchEndpoint_EvenForAdmin()
    {
        var (client, taskId, _) = await CreateAuthenticatedClientWithSeededHistoryAsync(SystemRole.Admin);

        // Không có API tạo TaskHistory thủ công - chỉ được ghi tự động qua domain event.
        // Route "/history" tồn tại (cho GET) nên POST vào đúng path này trả 405 (method not
        // allowed) chứ không phải 404 - đúng ngữ nghĩa HTTP, khác các route con /{id} không tồn tại.
        var response = await client.PostAsJsonAsync($"/api/v1/worktasks/{taskId}/history", new { description = "Giả mạo" });

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task GetHistory_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/worktasks/{Guid.NewGuid()}/history");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DirectDbSeededHistory_SurvivesAfterDeleteAttempt()
    {
        var (client, taskId, historyId) = await CreateAuthenticatedClientWithSeededHistoryAsync(SystemRole.Admin);

        await client.DeleteAsync($"/api/v1/worktasks/{taskId}/history/{historyId}");

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stillExists = await context.TaskHistories.AnyAsync(h => h.Id == historyId);

        Assert.True(stillExists, "Dòng lịch sử phải còn nguyên trong DB sau khi gọi DELETE (route không tồn tại).");
    }
}
