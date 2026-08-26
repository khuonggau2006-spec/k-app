using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Attachments.Common;
using TaskMgmt.Application.Features.Auth.Common;

namespace TaskMgmt.Application.Features.Users.Commands.UploadAvatar;

public class UploadAvatarCommandHandler(IApplicationDbContext context, IFileStorageService storage, ICurrentUserService currentUser)
    : IRequestHandler<UploadAvatarCommand, UserDto>
{
    public async Task<UserDto> Handle(UploadAvatarCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;
        var user = await context.Users.FirstAsync(u => u.Id == userId, cancellationToken);

        AttachmentFileValidator.TryGetAllowedContentType(request.FileName, out var contentType);

        var oldStorageKey = user.AvatarStorageKey;

        // Khoá lưu trữ ngẫu nhiên theo user, không dựa vào FileName gốc - tránh path traversal/đè file.
        var storageKey = $"avatars/{userId}/{Guid.NewGuid()}{Path.GetExtension(request.FileName)}";

        request.Content.Position = 0;
        await storage.UploadAsync(storageKey, request.Content, contentType, cancellationToken);

        user.AvatarStorageKey = storageKey;
        await context.SaveChangesAsync(cancellationToken);

        if (oldStorageKey != null)
        {
            await storage.DeleteAsync(oldStorageKey, cancellationToken);
        }

        return UserDto.FromEntity(user);
    }
}
