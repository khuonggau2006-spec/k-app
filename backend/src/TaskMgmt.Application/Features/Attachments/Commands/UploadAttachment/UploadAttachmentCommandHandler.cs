using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Attachments.Common;
using TaskMgmt.Domain.Entities;
using TaskMgmt.Domain.Events;

namespace TaskMgmt.Application.Features.Attachments.Commands.UploadAttachment;

public class UploadAttachmentCommandHandler(IApplicationDbContext context, IFileStorageService storage, ICurrentUserService currentUser)
    : IRequestHandler<UploadAttachmentCommand, AttachmentDto>
{
    public async Task<AttachmentDto> Handle(UploadAttachmentCommand request, CancellationToken cancellationToken)
    {
        AttachmentFileValidator.TryGetAllowedContentType(request.FileName, out var contentType);

        // Khoá lưu trữ ngẫu nhiên, không dựa vào FileName gốc, để tránh path traversal/đè file.
        var storageKey = $"{request.WorkTaskId}/{Guid.NewGuid()}{Path.GetExtension(request.FileName)}";

        request.Content.Position = 0;
        await storage.UploadAsync(storageKey, request.Content, contentType, cancellationToken);

        var attachment = new Attachment
        {
            WorkTaskId = request.WorkTaskId,
            FileName = request.FileName,
            StorageKey = storageKey,
            ContentType = contentType,
            SizeBytes = request.SizeBytes,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedByUserId = currentUser.UserId,
        };
        attachment.AddDomainEvent(new AttachmentAddedEvent(
            request.WorkTaskId, attachment.Id, attachment.FileName, currentUser.UserId, attachment.CreatedAtUtc));

        context.Attachments.Add(attachment);
        await context.SaveChangesAsync(cancellationToken);

        attachment.Uploader = currentUser.UserId is { } uploaderId
            ? await context.Users.FirstOrDefaultAsync(u => u.Id == uploaderId, cancellationToken)
            : null;

        return AttachmentDto.FromEntity(attachment);
    }
}
