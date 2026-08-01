using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Common.Models;
using TaskMgmt.Application.Features.Attachments.Common;

namespace TaskMgmt.Application.Features.Attachments.Commands.UploadAttachment;

public class UploadAttachmentCommandValidator : AbstractValidator<UploadAttachmentCommand>
{
    public UploadAttachmentCommandValidator(IApplicationDbContext context, StorageSettings storageSettings)
    {
        RuleFor(x => x.WorkTaskId)
            .MustAsync(async (id, cancellationToken) =>
                await context.WorkTasks.AnyAsync(t => t.Id == id && t.IsActive, cancellationToken))
            .WithMessage("Công việc không tồn tại.");

        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);

        RuleFor(x => x.FileName)
            .Must(fileName => AttachmentFileValidator.TryGetAllowedContentType(fileName, out _))
            .WithMessage("Định dạng file không được hỗ trợ.");

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0)
            .WithMessage("File rỗng.")
            .LessThanOrEqualTo(storageSettings.MaxFileSizeBytes)
            .WithMessage($"Dung lượng file vượt quá giới hạn cho phép ({storageSettings.MaxFileSizeBytes / 1024 / 1024}MB).");

        // Đối chiếu magic bytes với định dạng khai báo qua đuôi file để chặn file giả mạo đuôi.
        RuleFor(x => x)
            .Must(x =>
            {
                if (!AttachmentFileValidator.TryGetAllowedContentType(x.FileName, out var contentType))
                {
                    return true; // đã báo lỗi ở rule FileName phía trên, tránh trùng lỗi.
                }

                Span<byte> header = stackalloc byte[16];
                var bytesRead = x.Content.Read(header);
                x.Content.Position = 0;
                return AttachmentFileValidator.MatchesSignature(contentType, header[..bytesRead]);
            })
            .WithMessage("Nội dung file không khớp với định dạng khai báo.")
            .WithName(nameof(UploadAttachmentCommand.FileName));
    }
}
