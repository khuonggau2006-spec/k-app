using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Notifications.Common;

namespace TaskMgmt.Application.Features.Notifications.Queries.GetNotificationPreferences;

public class GetNotificationPreferencesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<GetNotificationPreferencesQuery, List<NotificationPreferenceDto>>
{
    public async Task<List<NotificationPreferenceDto>> Handle(
        GetNotificationPreferencesQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;

        var disabledTypes = await context.NotificationPreferences
            .Where(p => p.UserId == userId)
            .Select(p => p.Type)
            .ToListAsync(cancellationToken);

        return NotificationTypes.All
            .Select(type => new NotificationPreferenceDto(type, !disabledTypes.Contains(type)))
            .ToList();
    }
}
