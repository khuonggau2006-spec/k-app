using MediatR;
using TaskMgmt.Application.Features.Auth.Common;

namespace TaskMgmt.Application.Features.Auth.Commands.RefreshAccessToken;

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResultDto>;
