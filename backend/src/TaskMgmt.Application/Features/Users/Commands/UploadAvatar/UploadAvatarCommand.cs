using MediatR;
using TaskMgmt.Application.Features.Auth.Common;

namespace TaskMgmt.Application.Features.Users.Commands.UploadAvatar;

public record UploadAvatarCommand(string FileName, long SizeBytes, Stream Content) : IRequest<UserDto>;
