using MediatR;
using Microsoft.AspNetCore.Mvc;
using TaskMgmt.Application.Common.Models;
using TaskMgmt.Application.Features.Notifications.Commands.MarkAllNotificationsAsRead;
using TaskMgmt.Application.Features.Notifications.Commands.MarkNotificationAsRead;
using TaskMgmt.Application.Features.Notifications.Common;
using TaskMgmt.Application.Features.Notifications.Queries.GetNotifications;
using TaskMgmt.Application.Features.Notifications.Queries.GetUnreadNotificationCount;

namespace TaskMgmt.API.Controllers;

[ApiController]
[Route("api/v1/notifications")]
public class NotificationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<NotificationDto>>> GetAll(
        [FromQuery] GetNotificationsQuery query, CancellationToken cancellationToken)
    {
        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var count = await sender.Send(new GetUnreadNotificationCountQuery(), cancellationToken);
        return Ok(new { count });
    }

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new MarkNotificationAsReadCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        await sender.Send(new MarkAllNotificationsAsReadCommand(), cancellationToken);
        return NoContent();
    }
}
