using MediatR;

namespace TaskMgmt.Application.Features.Attachments.Commands.DeleteAttachment;

public record DeleteAttachmentCommand(Guid WorkTaskId, Guid AttachmentId) : IRequest;
