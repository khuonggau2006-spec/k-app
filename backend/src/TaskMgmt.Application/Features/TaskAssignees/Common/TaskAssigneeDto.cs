using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.Features.TaskAssignees.Common;

public record TaskAssigneeDto(
    Guid Id,
    Guid WorkTaskId,
    Guid UserId,
    string UserFullName,
    string UserEmail,
    bool UserHasAvatar,
    TaskAssigneeRole Role,
    DateTimeOffset AssignedAtUtc);
