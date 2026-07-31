using MediatR;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Common.Models;
using TaskMgmt.Application.Features.Auth.Common;
using TaskMgmt.Domain.Entities;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    JwtSettings jwtSettings) : IRequestHandler<RegisterCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var user = new User
        {
            Email = request.Email.Trim().ToLowerInvariant(),
            FullName = request.FullName.Trim(),
            PasswordHash = passwordHasher.Hash(request.Password),
            SystemRole = SystemRole.Member,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        context.Users.Add(user);

        var (accessToken, accessTokenExpiresAtUtc) = jwtTokenGenerator.GenerateAccessToken(user);

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            User = user,
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
