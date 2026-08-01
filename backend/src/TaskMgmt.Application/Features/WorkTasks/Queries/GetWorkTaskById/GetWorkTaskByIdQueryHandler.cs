using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Caching;
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Common.Extensions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.WorkTasks.Common;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.WorkTasks.Queries.GetWorkTaskById;

public class GetWorkTaskByIdQueryHandler(IApplicationDbContext context, ICacheService cache)
    : IRequestHandler<GetWorkTaskByIdQuery, WorkTaskDto>
{
    public Task<WorkTaskDto> Handle(GetWorkTaskByIdQuery request, CancellationToken cancellationToken) =>
        cache.GetOrSetAsync(CacheKeys.WorkTaskDetail(request.Id), CacheKeys.WorkTaskDetailExpiration, async () =>
        {
            var task = await context.WorkTasks
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
                ?? throw new NotFoundException(nameof(WorkTask), request.Id);

            return WorkTaskDto.FromEntity(task);
        }, cancellationToken);
}
