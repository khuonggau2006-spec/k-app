using TaskMgmt.Domain.Entities;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.UnitTests.Common;

internal static class TestDataFactory
{
    public static User CreateUser(string email = "user@example.com") => new()
    {
        Email = email,
        FullName = "Test User",
        PasswordHash = "hash",
        IsActive = true,
        CreatedAtUtc = DateTimeOffset.UtcNow,
    };

    public static Location CreateLocation(string name = "Test Location") => new()
    {
        Name = name,
        Latitude = 10,
        Longitude = 106,
        IsActive = true,
        CreatedAtUtc = DateTimeOffset.UtcNow,
    };

    public static WorkTask CreateWorkTask(string title = "Test Task", Guid? parentTaskId = null) => new()
    {
        Title = title,
        Status = WorkTaskStatus.ToDo,
        IsActive = true,
        ParentTaskId = parentTaskId,
        CreatedAtUtc = DateTimeOffset.UtcNow,
    };

    public static TaskAssignee CreateAssignee(Guid workTaskId, Guid userId, TaskAssigneeRole role) => new()
    {
        WorkTaskId = workTaskId,
        UserId = userId,
        Role = role,
        AssignedAtUtc = DateTimeOffset.UtcNow,
    };
}
