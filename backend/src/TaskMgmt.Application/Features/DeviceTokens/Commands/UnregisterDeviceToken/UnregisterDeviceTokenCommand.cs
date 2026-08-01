using MediatR;

namespace TaskMgmt.Application.Features.DeviceTokens.Commands.UnregisterDeviceToken;

public record UnregisterDeviceTokenCommand(string Token) : IRequest;
