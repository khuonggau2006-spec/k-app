using MediatR;
using TaskMgmt.Application.Common.Models;
using TaskMgmt.Application.Features.TaskHistories.Common;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.Features.TaskHistories.Queries.GetTaskHistory;

public record GetTaskHistoryQuery(
    Guid WorkTaskId,
    TaskHistoryActionType? ActionType = null,
    Guid? ActorUserId = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<TaskHistoryDto>>;
