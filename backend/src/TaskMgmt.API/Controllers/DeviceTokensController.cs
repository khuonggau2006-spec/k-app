using MediatR;
using Microsoft.AspNetCore.Mvc;
using TaskMgmt.Application.Features.DeviceTokens.Commands.RegisterDeviceToken;
using TaskMgmt.Application.Features.DeviceTokens.Commands.UnregisterDeviceToken;
using TaskMgmt.Application.Features.DeviceTokens.Common;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.API.Controllers;

[ApiController]
[Route("api/v1/device-tokens")]
public class DeviceTokensController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<DeviceTokenDto>> Register(RegisterDeviceTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RegisterDeviceTokenCommand(request.Token, request.Platform), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{token}")]
    public async Task<IActionResult> Unregister(string token, CancellationToken cancellationToken)
    {
        await sender.Send(new UnregisterDeviceTokenCommand(token), cancellationToken);
        return NoContent();
    }
}

public record RegisterDeviceTokenRequest(string Token, DevicePlatform Platform);
