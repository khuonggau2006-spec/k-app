using MediatR;
using Microsoft.AspNetCore.Mvc;
using TaskMgmt.Application.Features.Attachments.Commands.DeleteAttachment;
using TaskMgmt.Application.Features.Attachments.Commands.UploadAttachment;
using TaskMgmt.Application.Features.Attachments.Common;
using TaskMgmt.Application.Features.Attachments.Queries.DownloadAttachment;
using TaskMgmt.Application.Features.Attachments.Queries.GetAttachments;

namespace TaskMgmt.API.Controllers;

[ApiController]
[Route("api/v1/worktasks/{taskId:guid}/attachments")]
public class AttachmentsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AttachmentDto>>> GetAll(Guid taskId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAttachmentsQuery(taskId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<ActionResult<AttachmentDto>> Upload(Guid taskId, IFormFile file, CancellationToken cancellationToken)
    {
        // Đệm vào MemoryStream để đảm bảo seek được (dùng cho kiểm tra magic bytes trong validator
        // rồi upload lên storage), không phụ thuộc vào cách ASP.NET Core buffer multipart nội bộ.
        var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        var result = await sender.Send(
            new UploadAttachmentCommand(taskId, file.FileName, file.Length, buffer), cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { taskId }, result);
    }

    [HttpGet("{attachmentId:guid}/download")]
    public async Task<IActionResult> Download(Guid taskId, Guid attachmentId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DownloadAttachmentQuery(taskId, attachmentId), cancellationToken);
        return File(result.Content, result.ContentType, result.FileName);
    }

    [HttpDelete("{attachmentId:guid}")]
    public async Task<IActionResult> Delete(Guid taskId, Guid attachmentId, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteAttachmentCommand(taskId, attachmentId), cancellationToken);
        return NoContent();
    }
}
