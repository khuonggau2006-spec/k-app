using MediatR;
using TaskMgmt.Application.Common.Models;
using TaskMgmt.Application.Features.Notifications.Common;

namespace TaskMgmt.Application.Features.Notifications.Queries.GetNotifications;

public record GetNotificationsQuery(
    bool UnreadOnly = false,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedResult<NotificationDto>>;
