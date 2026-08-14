using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Authorization;
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Domain.Entities;
using TaskMgmt.Domain.Events;

namespace TaskMgmt.Application.Features.Attachments.Commands.DeleteAttachment;

public class DeleteAttachmentCommandHandler(IApplicationDbContext context, IFileStorageService storage, ICurrentUserService currentUser)
    : IRequestHandler<DeleteAttachmentCommand>
{
    public async Task Handle(DeleteAttachmentCommand request, CancellationToken cancellationToken)
    {
        var attachment = await context.Attachments
            .FirstOrDefaultAsync(a => a.Id == request.AttachmentId && a.WorkTaskId == request.WorkTaskId, cancellationToken)
            ?? throw new NotFoundException(nameof(Attachment), request.AttachmentId);

        // Người upload luôn được xoá file của chính mình; ngoài ra chỉ Owner của task hoặc
        // Manager/Admin mới được xoá file người khác upload.
        if (attachment.CreatedByUserId != currentUser.UserId)
        {
            await WorkTaskAccessControl.EnsureCanManageAsync(context, currentUser, request.WorkTaskId, cancellationToken);
        }

        // Xoá object trong storage trước, xoá bản ghi DB sau - nếu bước xoá storage lỗi thì
        // dừng lại, tránh để lại bản ghi DB trỏ tới file đã không còn tồn tại.
        await storage.DeleteAsync(attachment.StorageKey, cancellationToken);

        attachment.AddDomainEvent(new AttachmentRemovedEvent(
            request.WorkTaskId, attachment.Id, attachment.FileName, currentUser.UserId, DateTimeOffset.UtcNow));

        context.Attachments.Remove(attachment);
        await context.SaveChangesAsync(cancellationToken);
    }
}
