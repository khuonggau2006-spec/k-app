using MediatR;
using TaskMgmt.Application.Features.Auth.Common;

namespace TaskMgmt.Application.Features.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<AuthResultDto>;
