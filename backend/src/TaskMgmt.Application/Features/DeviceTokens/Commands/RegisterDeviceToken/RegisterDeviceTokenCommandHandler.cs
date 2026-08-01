using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.DeviceTokens.Common;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.DeviceTokens.Commands.RegisterDeviceToken;

public class RegisterDeviceTokenCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<RegisterDeviceTokenCommand, DeviceTokenDto>
{
    public async Task<DeviceTokenDto> Handle(RegisterDeviceTokenCommand request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        // Token là duy nhất toàn hệ thống: nếu thiết bị đã đăng nhập bằng tài khoản khác trước
        // đó, gán lại token cho user hiện tại thay vì tạo bản ghi trùng.
        var token = await context.DeviceTokens.FirstOrDefaultAsync(t => t.Token == request.Token, cancellationToken);
        if (token is not null)
        {
            token.UserId = currentUser.UserId!.Value;
            token.Platform = request.Platform;
            token.LastUsedAtUtc = now;
        }
        else
        {
            token = new DeviceToken
            {
                UserId = currentUser.UserId!.Value,
                Token = request.Token,
                Platform = request.Platform,
                CreatedAtUtc = now,
                LastUsedAtUtc = now,
            };
            context.DeviceTokens.Add(token);
        }

        await context.SaveChangesAsync(cancellationToken);

        return DeviceTokenDto.FromEntity(token);
    }
}
