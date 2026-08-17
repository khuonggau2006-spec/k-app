using MediatR;
using TaskMgmt.Application.Features.Notifications.Common;

namespace TaskMgmt.Application.Features.Notifications.Queries.GetNotificationPreferences;

public record GetNotificationPreferencesQuery : IRequest<List<NotificationPreferenceDto>>;
