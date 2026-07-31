using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.WorkTasks.Common;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.WorkTasks.Queries.GetWorkTaskById;

public class GetWorkTaskByIdQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetWorkTaskByIdQuery, WorkTaskDto>
{
    public async Task<WorkTaskDto> Handle(GetWorkTaskByIdQuery request, CancellationToken cancellationToken)
    {
        var task = await context.WorkTasks
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(WorkTask), request.Id);

        return WorkTaskDto.FromEntity(task);
    }
}
