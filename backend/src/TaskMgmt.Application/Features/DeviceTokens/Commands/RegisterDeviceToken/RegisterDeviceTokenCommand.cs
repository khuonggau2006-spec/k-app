using MediatR;
using TaskMgmt.Application.Features.DeviceTokens.Common;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.Features.DeviceTokens.Commands.RegisterDeviceToken;

public record RegisterDeviceTokenCommand(string Token, DevicePlatform Platform) : IRequest<DeviceTokenDto>;
