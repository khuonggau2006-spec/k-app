using TaskMgmt.Application.Features.Users.Queries.GetUsers;
using TaskMgmt.Application.UnitTests.Common;

namespace TaskMgmt.Application.UnitTests.Features.Users;

public class GetUsersQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsHasAvatarTrue_WhenUserHasAvatarStorageKey()
    {
        using var context = TestDbContextFactory.Create();
        var withAvatar = TestDataFactory.CreateUser("with-avatar@example.com");
        withAvatar.AvatarStorageKey = "avatars/x/y.jpg";
        var withoutAvatar = TestDataFactory.CreateUser("no-avatar@example.com");
        context.Users.AddRange(withAvatar, withoutAvatar);
        await context.SaveChangesAsync(default);

        var handler = new GetUsersQueryHandler(context);
        var result = await handler.Handle(new GetUsersQuery(), default);

        Assert.True(result.Single(u => u.Email == "with-avatar@example.com").HasAvatar);
        Assert.False(result.Single(u => u.Email == "no-avatar@example.com").HasAvatar);
    }
}
