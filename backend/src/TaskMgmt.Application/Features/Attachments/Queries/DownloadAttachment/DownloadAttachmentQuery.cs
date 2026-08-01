using MediatR;

namespace TaskMgmt.Application.Features.Attachments.Queries.DownloadAttachment;

public record DownloadAttachmentQuery(Guid WorkTaskId, Guid AttachmentId) : IRequest<AttachmentDownloadResult>;

public record AttachmentDownloadResult(Stream Content, string FileName, string ContentType);
