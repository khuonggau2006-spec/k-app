using MediatR;
using TaskMgmt.Application.Features.WorkTasks.Common;

namespace TaskMgmt.Application.Features.WorkTasks.Queries.GetWorkTaskById;

public record GetWorkTaskByIdQuery(Guid Id) : IRequest<WorkTaskDto>;
