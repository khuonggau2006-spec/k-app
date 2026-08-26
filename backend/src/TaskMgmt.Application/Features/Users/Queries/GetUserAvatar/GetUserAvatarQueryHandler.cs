using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Attachments.Common;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.Users.Queries.GetUserAvatar;

public class GetUserAvatarQueryHandler(IApplicationDbContext context, IFileStorageService storage)
    : IRequestHandler<GetUserAvatarQuery, UserAvatarResult>
{
    public async Task<UserAvatarResult> Handle(GetUserAvatarQuery request, CancellationToken cancellationToken)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        if (user.AvatarStorageKey == null)
        {
            throw new NotFoundException(nameof(User.AvatarStorageKey), request.UserId);
        }

        // Đuôi file được giữ nguyên lúc tạo storage key (UploadAvatarCommandHandler) nên suy
        // content-type trực tiếp từ đó, không cần lưu cột content-type riêng.
        AttachmentFileValidator.TryGetAllowedContentType(user.AvatarStorageKey, out var contentType);
        var stream = await storage.DownloadAsync(user.AvatarStorageKey, cancellationToken);

        return new UserAvatarResult(stream, contentType);
    }
}
