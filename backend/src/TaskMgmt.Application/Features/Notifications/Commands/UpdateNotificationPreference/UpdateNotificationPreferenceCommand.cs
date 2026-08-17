using MediatR;

namespace TaskMgmt.Application.Features.Notifications.Commands.UpdateNotificationPreference;

public record UpdateNotificationPreferenceCommand(string Type, bool IsEnabled) : IRequest;
