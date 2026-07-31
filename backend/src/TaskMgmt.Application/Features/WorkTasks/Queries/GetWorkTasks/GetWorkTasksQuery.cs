using MediatR;
using TaskMgmt.Application.Common.Models;
using TaskMgmt.Application.Features.WorkTasks.Common;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.Features.WorkTasks.Queries.GetWorkTasks;

public record GetWorkTasksQuery(
    WorkTaskStatus? Status = null,
    Guid? LocationId = null,
    Guid? ParentTaskId = null,
    int PageNumber = 1,
    int PageSize = 20,
    string? SortBy = null,
    bool SortDescending = true) : IRequest<PagedResult<WorkTaskDto>>;
