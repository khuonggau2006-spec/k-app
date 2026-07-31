using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Common.Models;
using TaskMgmt.Application.Features.Auth.Common;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    JwtSettings jwtSettings) : IRequestHandler<LoginCommand, AuthResultDto>
{
    private const string InvalidCredentialsMessage = "Email hoặc mật khẩu không đúng.";

    public async Task<AuthResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken)
            ?? throw new UnauthorizedException(InvalidCredentialsMessage);

        if (!user.IsActive || !passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        var (accessToken, accessTokenExpiresAtUtc) = jwtTokenGenerator.GenerateAccessToken(user);

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = RefreshTokenGenerator.Generate(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(jwtSettings.RefreshTokenExpirationDays),
        };

        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync(cancellationToken);

        return new AuthResultDto(
            accessToken,
            accessTokenExpiresAtUtc,
            refreshToken.Token,
            refreshToken.ExpiresAtUtc,
            UserDto.FromEntity(user));
    }
}
