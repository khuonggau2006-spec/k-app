using MediatR;
using TaskMgmt.Application.Features.Attachments.Common;

namespace TaskMgmt.Application.Features.Attachments.Commands.UploadAttachment;

public record UploadAttachmentCommand(Guid WorkTaskId, string FileName, long SizeBytes, Stream Content) : IRequest<AttachmentDto>;
