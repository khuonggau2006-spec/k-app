using MediatR;
using TaskMgmt.Application.Features.Auth.Common;

namespace TaskMgmt.Application.Features.Auth.Commands.Register;

public record RegisterCommand(string Email, string FullName, string Password) : IRequest<AuthResultDto>;
