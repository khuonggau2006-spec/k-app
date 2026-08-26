using TaskMgmt.Application.Features.Users.Commands.UploadAvatar;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.UnitTests.Features.Users;

public class UploadAvatarCommandHandlerTests
{
    [Fact]
    public async Task Handle_FirstUpload_SetsAvatarStorageKeyAndUploadsToStorage()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync(default);

        var storage = new FakeFileStorageService();
        var currentUser = new FakeCurrentUserService(user.Id, SystemRole.Member);
        var handler = new UploadAvatarCommandHandler(context, storage, currentUser);

        var content = new MemoryStream([0xFF, 0xD8, 0xFF]);
        var result = await handler.Handle(new UploadAvatarCommand("photo.jpg", content.Length, content), default);

        Assert.True(result.HasAvatar);
        Assert.Single(storage.UploadedKeys);
        Assert.Empty(storage.DeletedKeys);
        var updatedUser = await context.Users.FindAsync(user.Id);
        Assert.Equal(storage.UploadedKeys[0], updatedUser!.AvatarStorageKey);
        Assert.EndsWith(".jpg", updatedUser.AvatarStorageKey);
    }

    [Fact]
    public async Task Handle_SecondUpload_DeletesOldStorageKeyFirst()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        user.AvatarStorageKey = "avatars/old/key.png";
        context.Users.Add(user);
        await context.SaveChangesAsync(default);

        var storage = new FakeFileStorageService();
        var currentUser = new FakeCurrentUserService(user.Id, SystemRole.Member);
        var handler = new UploadAvatarCommandHandler(context, storage, currentUser);

        var content = new MemoryStream([0xFF, 0xD8, 0xFF]);
        await handler.Handle(new UploadAvatarCommand("new.jpg", content.Length, content), default);

        Assert.Equal(["avatars/old/key.png"], storage.DeletedKeys);
        Assert.Single(storage.UploadedKeys);
    }
}
