using MediatR;
using TaskMgmt.Application.Features.Attachments.Common;

namespace TaskMgmt.Application.Features.Attachments.Queries.GetAttachments;

public record GetAttachmentsQuery(Guid WorkTaskId) : IRequest<List<AttachmentDto>>;
