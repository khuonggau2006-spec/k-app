using TaskMgmt.Application.Features.Auth.Commands.Logout;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.UnitTests.Features.Auth;

public class LogoutCommandHandlerTests
{
    private static RefreshToken CreateToken(Guid userId, DateTimeOffset? revokedAtUtc = null) => new()
    {
        UserId = userId,
        Token = Guid.NewGuid().ToString(),
        CreatedAtUtc = DateTimeOffset.UtcNow,
        ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(14),
        RevokedAtUtc = revokedAtUtc,
    };

    [Fact]
    public async Task Handle_ActiveToken_RevokesIt()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        var token = CreateToken(user.Id);
        context.Users.Add(user);
        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync(default);

        var handler = new LogoutCommandHandler(context);
        await handler.Handle(new LogoutCommand(token.Token), default);

        var reloaded = await context.RefreshTokens.FindAsync(token.Id);
        Assert.NotNull(reloaded!.RevokedAtUtc);
    }

    [Fact]
    public async Task Handle_UnknownToken_DoesNotThrow()
    {
        using var context = TestDbContextFactory.Create();
        var handler = new LogoutCommandHandler(context);

        var exception = await Record.ExceptionAsync(
            () => handler.Handle(new LogoutCommand("token-khong-ton-tai"), default));

        Assert.Null(exception);
    }

    [Fact]
    public async Task Handle_AlreadyRevokedToken_DoesNotThrow_AndKeepsOriginalRevokedAtUtc()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        var revokedAt = DateTimeOffset.UtcNow.AddDays(-1);
        var token = CreateToken(user.Id, revokedAt);
        context.Users.Add(user);
        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync(default);

        var handler = new LogoutCommandHandler(context);
        var exception = await Record.ExceptionAsync(
            () => handler.Handle(new LogoutCommand(token.Token), default));

        Assert.Null(exception);
        var reloaded = await context.RefreshTokens.FindAsync(token.Id);
        Assert.Equal(revokedAt, reloaded!.RevokedAtUtc);
    }
}
