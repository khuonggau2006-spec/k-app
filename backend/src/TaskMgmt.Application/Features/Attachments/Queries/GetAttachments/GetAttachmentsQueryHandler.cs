using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Attachments.Common;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.Attachments.Queries.GetAttachments;

public class GetAttachmentsQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetAttachmentsQuery, List<AttachmentDto>>
{
    public async Task<List<AttachmentDto>> Handle(GetAttachmentsQuery request, CancellationToken cancellationToken)
    {
        var taskExists = await context.WorkTasks.AnyAsync(t => t.Id == request.WorkTaskId, cancellationToken);
        if (!taskExists)
        {
            throw new NotFoundException(nameof(WorkTask), request.WorkTaskId);
        }

        var attachments = await context.Attachments
            .Where(a => a.WorkTaskId == request.WorkTaskId)
            .Include(a => a.Uploader)
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return attachments.Select(AttachmentDto.FromEntity).ToList();
    }
}
