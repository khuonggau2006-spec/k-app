using MediatR;
using TaskMgmt.Application.Features.Auth.Common;

namespace TaskMgmt.Application.Features.Users.Commands.DeleteAvatar;

public record DeleteAvatarCommand : IRequest<UserDto>;
