using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.Attachments.Common;

public record AttachmentDto(
    Guid Id,
    Guid WorkTaskId,
    string FileName,
    string ContentType,
    long SizeBytes,
    Guid? UploadedByUserId,
    string UploadedByFullName,
    string UploadedByEmail,
    DateTimeOffset CreatedAtUtc)
{
    // Yêu cầu Attachment đã được load kèm Uploader (Include).
    public static AttachmentDto FromEntity(Attachment attachment) => new(
        attachment.Id,
        attachment.WorkTaskId,
        attachment.FileName,
        attachment.ContentType,
        attachment.SizeBytes,
        attachment.CreatedByUserId,
        attachment.Uploader?.FullName ?? string.Empty,
        attachment.Uploader?.Email ?? string.Empty,
        attachment.CreatedAtUtc);
}
