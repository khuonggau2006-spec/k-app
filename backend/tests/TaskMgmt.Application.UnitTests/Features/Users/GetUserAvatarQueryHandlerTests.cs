using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Features.Users.Queries.GetUserAvatar;
using TaskMgmt.Application.UnitTests.Common;

namespace TaskMgmt.Application.UnitTests.Features.Users;

public class GetUserAvatarQueryHandlerTests
{
    [Fact]
    public async Task Handle_UserHasAvatar_ReturnsStreamAndContentType()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        user.AvatarStorageKey = "avatars/x/photo.png";
        context.Users.Add(user);
        await context.SaveChangesAsync(default);

        var handler = new GetUserAvatarQueryHandler(context, new FakeFileStorageService());

        var result = await handler.Handle(new GetUserAvatarQuery(user.Id), default);

        Assert.Equal("image/png", result.ContentType);
        Assert.NotNull(result.Content);
    }

    [Fact]
    public async Task Handle_UserHasNoAvatar_ThrowsNotFound()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync(default);

        var handler = new GetUserAvatarQueryHandler(context, new FakeFileStorageService());

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new GetUserAvatarQuery(user.Id), default));
    }
}
