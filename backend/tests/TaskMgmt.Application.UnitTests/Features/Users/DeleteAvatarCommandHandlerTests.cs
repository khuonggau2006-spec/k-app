using TaskMgmt.Application.Features.Users.Commands.DeleteAvatar;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.UnitTests.Features.Users;

public class DeleteAvatarCommandHandlerTests
{
    [Fact]
    public async Task Handle_UserHasAvatar_DeletesFromStorageAndClearsField()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        user.AvatarStorageKey = "avatars/x/y.jpg";
        context.Users.Add(user);
        await context.SaveChangesAsync(default);

        var storage = new FakeFileStorageService();
        var currentUser = new FakeCurrentUserService(user.Id, SystemRole.Member);
        var handler = new DeleteAvatarCommandHandler(context, storage, currentUser);

        var result = await handler.Handle(new DeleteAvatarCommand(), default);

        Assert.False(result.HasAvatar);
        Assert.Equal(["avatars/x/y.jpg"], storage.DeletedKeys);
        var updatedUser = await context.Users.FindAsync(user.Id);
        Assert.Null(updatedUser!.AvatarStorageKey);
    }

    [Fact]
    public async Task Handle_UserHasNoAvatar_IsNoOp()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync(default);

        var storage = new FakeFileStorageService();
        var currentUser = new FakeCurrentUserService(user.Id, SystemRole.Member);
        var handler = new DeleteAvatarCommandHandler(context, storage, currentUser);

        var result = await handler.Handle(new DeleteAvatarCommand(), default);

        Assert.False(result.HasAvatar);
        Assert.Empty(storage.DeletedKeys);
    }
}
