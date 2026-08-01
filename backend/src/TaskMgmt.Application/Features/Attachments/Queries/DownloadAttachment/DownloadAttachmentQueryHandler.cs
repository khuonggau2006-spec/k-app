using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.Attachments.Queries.DownloadAttachment;

public class DownloadAttachmentQueryHandler(IApplicationDbContext context, IFileStorageService storage)
    : IRequestHandler<DownloadAttachmentQuery, AttachmentDownloadResult>
{
    public async Task<AttachmentDownloadResult> Handle(DownloadAttachmentQuery request, CancellationToken cancellationToken)
    {
        var attachment = await context.Attachments
            .FirstOrDefaultAsync(a => a.Id == request.AttachmentId && a.WorkTaskId == request.WorkTaskId, cancellationToken)
            ?? throw new NotFoundException(nameof(Attachment), request.AttachmentId);

        var stream = await storage.DownloadAsync(attachment.StorageKey, cancellationToken);
        return new AttachmentDownloadResult(stream, attachment.FileName, attachment.ContentType);
    }
}
