using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;

namespace TaskMgmt.Application.Features.Auth.Commands.Logout;

public class LogoutCommandHandler(IApplicationDbContext context) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var token = await context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, cancellationToken);

        // Idempotent: token không tồn tại hoặc đã bị revoke trước đó thì coi như đăng xuất
        // thành công, không throw - client không cần biết token còn hợp lệ hay không.
        if (token is not null && token.RevokedAtUtc is null)
        {
            token.RevokedAtUtc = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
