using MediatR;

namespace TaskMgmt.Application.Features.Users.Queries.GetUserAvatar;

public record GetUserAvatarQuery(Guid UserId) : IRequest<UserAvatarResult>;

public record UserAvatarResult(Stream Content, string ContentType);
