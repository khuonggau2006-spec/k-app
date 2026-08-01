using MediatR;

namespace TaskMgmt.Application.Features.Notifications.Queries.GetUnreadNotificationCount;

public record GetUnreadNotificationCountQuery : IRequest<int>;
