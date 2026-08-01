using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;

namespace TaskMgmt.Application.Features.DeviceTokens.Commands.UnregisterDeviceToken;

public class UnregisterDeviceTokenCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<UnregisterDeviceTokenCommand>
{
    public async Task Handle(UnregisterDeviceTokenCommand request, CancellationToken cancellationToken)
    {
        // Không báo lỗi nếu không tìm thấy - đăng xuất phải luôn thành công, kể cả khi token
        // đã bị gỡ trước đó (idempotent) hoặc đã được gán sang tài khoản khác trên cùng thiết bị.
        var token = await context.DeviceTokens
            .FirstOrDefaultAsync(t => t.Token == request.Token && t.UserId == currentUser.UserId, cancellationToken);

        if (token is not null)
        {
            context.DeviceTokens.Remove(token);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
