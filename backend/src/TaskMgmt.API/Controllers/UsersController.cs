using MediatR;
using Microsoft.AspNetCore.Mvc;
using TaskMgmt.Application.Features.Auth.Common;
using TaskMgmt.Application.Features.Users.Commands.DeleteAvatar;
using TaskMgmt.Application.Features.Users.Commands.UploadAvatar;
using TaskMgmt.Application.Features.Users.Queries.GetUserAvatar;
using TaskMgmt.Application.Features.Users.Queries.GetUsers;

namespace TaskMgmt.API.Controllers;

[ApiController]
[Route("api/v1/users")]
public class UsersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUsersQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("me/avatar")]
    [RequestSizeLimit(5 * 1024 * 1024 + 64 * 1024)]
    public async Task<ActionResult<UserDto>> UploadAvatar(IFormFile file, CancellationToken cancellationToken)
    {
        var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        var result = await sender.Send(new UploadAvatarCommand(file.FileName, file.Length, buffer), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("me/avatar")]
    public async Task<ActionResult<UserDto>> DeleteAvatar(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteAvatarCommand(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/avatar")]
    public async Task<IActionResult> GetAvatar(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUserAvatarQuery(id), cancellationToken);
        return File(result.Content, result.ContentType);
    }
}
