using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaskMgmt.Application.Features.Auth.Commands.Login;
using TaskMgmt.Application.Features.Auth.Commands.Logout;
using TaskMgmt.Application.Features.Auth.Commands.RefreshAccessToken;
using TaskMgmt.Application.Features.Auth.Commands.Register;
using TaskMgmt.Application.Features.Auth.Common;

namespace TaskMgmt.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
[AllowAnonymous]
[EnableRateLimiting("auth")]
public class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResultDto>> Register(RegisterCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResultDto>> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<AuthResultDto>> RefreshToken(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return NoContent();
    }
}
