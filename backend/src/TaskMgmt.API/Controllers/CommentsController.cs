using MediatR;
using Microsoft.AspNetCore.Mvc;
using TaskMgmt.Application.Features.Comments.Commands.CreateComment;
using TaskMgmt.Application.Features.Comments.Common;
using TaskMgmt.Application.Features.Comments.Queries.GetComments;

namespace TaskMgmt.API.Controllers;

[ApiController]
[Route("api/v1/worktasks/{taskId:guid}/comments")]
public class CommentsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CommentDto>>> GetAll(Guid taskId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCommentsQuery(taskId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CommentDto>> Create(
        Guid taskId, CreateCommentRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateCommentCommand(taskId, request.Content, request.MentionedUserIds ?? []), cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { taskId }, result);
    }
}

public record CreateCommentRequest(string Content, List<Guid>? MentionedUserIds);
