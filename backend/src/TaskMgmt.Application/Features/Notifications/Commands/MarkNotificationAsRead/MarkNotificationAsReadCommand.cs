using MediatR;

namespace TaskMgmt.Application.Features.Notifications.Commands.MarkNotificationAsRead;

public record MarkNotificationAsReadCommand(Guid Id) : IRequest;
