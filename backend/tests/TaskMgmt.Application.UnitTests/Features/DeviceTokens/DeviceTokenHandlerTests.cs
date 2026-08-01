using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Features.DeviceTokens.Commands.RegisterDeviceToken;
using TaskMgmt.Application.Features.DeviceTokens.Commands.UnregisterDeviceToken;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.UnitTests.Features.DeviceTokens;

public class DeviceTokenHandlerTests
{
    [Fact]
    public async Task RegisterDeviceToken_NewToken_CreatesRowForCurrentUser()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(user.Id, SystemRole.Member);
        var handler = new RegisterDeviceTokenCommandHandler(context, currentUser);

        var result = await handler.Handle(new RegisterDeviceTokenCommand("token-abc", DevicePlatform.Android), default);

        var stored = await context.DeviceTokens.SingleAsync();
        Assert.Equal(user.Id, stored.UserId);
        Assert.Equal("token-abc", stored.Token);
        Assert.Equal(DevicePlatform.Android, stored.Platform);
        Assert.Equal(stored.Id, result.Id);
    }

    [Fact]
    public async Task RegisterDeviceToken_SameTokenSameUser_UpdatesInPlaceWithoutDuplicating()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(user.Id, SystemRole.Member);
        var handler = new RegisterDeviceTokenCommandHandler(context, currentUser);

        await handler.Handle(new RegisterDeviceTokenCommand("token-abc", DevicePlatform.Android), default);
        await handler.Handle(new RegisterDeviceTokenCommand("token-abc", DevicePlatform.Ios), default);

        var stored = await context.DeviceTokens.SingleAsync();
        Assert.Equal(DevicePlatform.Ios, stored.Platform);
    }

    [Fact]
    public async Task RegisterDeviceToken_SameTokenDifferentUser_ReassignsOwnership()
    {
        using var context = TestDbContextFactory.Create();
        var userA = TestDataFactory.CreateUser("a@example.com");
        var userB = TestDataFactory.CreateUser("b@example.com");
        context.Users.AddRange(userA, userB);
        await context.SaveChangesAsync(default);

        var handlerAsA = new RegisterDeviceTokenCommandHandler(context, new FakeCurrentUserService(userA.Id, SystemRole.Member));
        await handlerAsA.Handle(new RegisterDeviceTokenCommand("shared-device-token", DevicePlatform.Android), default);

        // Cùng thiết bị, đăng xuất A rồi đăng nhập B - token phải được gán lại cho B.
        var handlerAsB = new RegisterDeviceTokenCommandHandler(context, new FakeCurrentUserService(userB.Id, SystemRole.Member));
        await handlerAsB.Handle(new RegisterDeviceTokenCommand("shared-device-token", DevicePlatform.Android), default);

        var stored = await context.DeviceTokens.SingleAsync();
        Assert.Equal(userB.Id, stored.UserId);
    }

    [Fact]
    public async Task UnregisterDeviceToken_OwnedByCurrentUser_Removes()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(user.Id, SystemRole.Member);
        await new RegisterDeviceTokenCommandHandler(context, currentUser)
            .Handle(new RegisterDeviceTokenCommand("token-abc", DevicePlatform.Android), default);

        await new UnregisterDeviceTokenCommandHandler(context, currentUser)
            .Handle(new UnregisterDeviceTokenCommand("token-abc"), default);

        Assert.Empty(context.DeviceTokens);
    }

    [Fact]
    public async Task UnregisterDeviceToken_NotFound_DoesNotThrow()
    {
        using var context = TestDbContextFactory.Create();
        var currentUser = new FakeCurrentUserService(Guid.NewGuid(), SystemRole.Member);

        await new UnregisterDeviceTokenCommandHandler(context, currentUser)
            .Handle(new UnregisterDeviceTokenCommand("does-not-exist"), default);

        Assert.Empty(context.DeviceTokens);
    }

    [Fact]
    public async Task UnregisterDeviceToken_OwnedByAnotherUser_DoesNotRemove()
    {
        using var context = TestDbContextFactory.Create();
        var owner = TestDataFactory.CreateUser("owner@example.com");
        var otherUser = TestDataFactory.CreateUser("other@example.com");
        context.Users.AddRange(owner, otherUser);
        await context.SaveChangesAsync(default);

        await new RegisterDeviceTokenCommandHandler(context, new FakeCurrentUserService(owner.Id, SystemRole.Member))
            .Handle(new RegisterDeviceTokenCommand("token-abc", DevicePlatform.Android), default);

        await new UnregisterDeviceTokenCommandHandler(context, new FakeCurrentUserService(otherUser.Id, SystemRole.Member))
            .Handle(new UnregisterDeviceTokenCommand("token-abc"), default);

        Assert.Single(context.DeviceTokens);
    }
}
