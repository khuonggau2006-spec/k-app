using MediatR;
using Microsoft.AspNetCore.Mvc;
using TaskMgmt.Application.Features.Auth.Common;
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
}
