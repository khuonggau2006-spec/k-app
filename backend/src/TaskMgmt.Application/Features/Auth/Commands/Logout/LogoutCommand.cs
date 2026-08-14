using MediatR;

namespace TaskMgmt.Application.Features.Auth.Commands.Logout;

public record LogoutCommand(string RefreshToken) : IRequest;
