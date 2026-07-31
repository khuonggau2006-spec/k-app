using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Common.Models;
using TaskMgmt.Application.Features.Auth.Common;

namespace TaskMgmt.Application.Features.Auth.Commands.RefreshAccessToken;

public class RefreshTokenCommandHandler(
    IApplicationDbContext context,
    IJwtTokenGenerator jwtTokenGenerator,
    JwtSettings jwtSettings) : IRequestHandler<RefreshTokenCommand, AuthResultDto>
{
    private const string InvalidTokenMessage = "Refresh token không hợp lệ hoặc đã hết hạn.";

    public async Task<AuthResultDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var existingToken = await context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, cancellationToken)
            ?? throw new UnauthorizedException(InvalidTokenMessage);

        if (!existingToken.IsActive || existingToken.User is null || !existingToken.User.IsActive)
        {
            throw new UnauthorizedException(InvalidTokenMessage);
        }

        var user = existingToken.User;

        var newToken = new Domain.Entities.RefreshToken
        {
            UserId = user.Id,
            Token = RefreshTokenGenerator.Generate(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(jwtSettings.RefreshTokenExpirationDays),
        };

        existingToken.RevokedAtUtc = DateTimeOffset.UtcNow;
        existingToken.ReplacedByToken = newToken.Token;

        context.RefreshTokens.Add(newToken);

        var (accessToken, accessTokenExpiresAtUtc) = jwtTokenGenerator.GenerateAccessToken(user);

        await context.SaveChangesAsync(cancellationToken);

        return new AuthResultDto(
            accessToken,
            accessTokenExpiresAtUtc,
            newToken.Token,
            newToken.ExpiresAtUtc,
            UserDto.FromEntity(user));
    }
}
