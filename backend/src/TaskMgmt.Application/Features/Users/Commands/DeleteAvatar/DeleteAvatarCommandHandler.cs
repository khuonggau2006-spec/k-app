using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Auth.Common;

namespace TaskMgmt.Application.Features.Users.Commands.DeleteAvatar;

public class DeleteAvatarCommandHandler(IApplicationDbContext context, IFileStorageService storage, ICurrentUserService currentUser)
    : IRequestHandler<DeleteAvatarCommand, UserDto>
{
    public async Task<UserDto> Handle(DeleteAvatarCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;
        var user = await context.Users.FirstAsync(u => u.Id == userId, cancellationToken);

        if (user.AvatarStorageKey != null)
        {
            await storage.DeleteAsync(user.AvatarStorageKey, cancellationToken);
            user.AvatarStorageKey = null;
            await context.SaveChangesAsync(cancellationToken);
        }

        return UserDto.FromEntity(user);
    }
}
