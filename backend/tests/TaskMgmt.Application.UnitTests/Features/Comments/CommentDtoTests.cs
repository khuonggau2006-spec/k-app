using TaskMgmt.Application.Features.Comments.Common;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.UnitTests.Features.Comments;

public class CommentDtoTests
{
    [Fact]
    public void FromEntity_AuthorHasAvatarStorageKey_SetsAuthorHasAvatarTrue()
    {
        var author = TestDataFactory.CreateUser();
        author.AvatarStorageKey = "avatars/x/y.jpg";
        var comment = new Comment
        {
            WorkTaskId = Guid.NewGuid(),
            Content = "test",
            Author = author,
            CreatedByUserId = author.Id,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        var dto = CommentDto.FromEntity(comment);

        Assert.True(dto.AuthorHasAvatar);
    }

    [Fact]
    public void FromEntity_AuthorHasNoAvatarStorageKey_SetsAuthorHasAvatarFalse()
    {
        var author = TestDataFactory.CreateUser();
        var comment = new Comment
        {
            WorkTaskId = Guid.NewGuid(),
            Content = "test",
            Author = author,
            CreatedByUserId = author.Id,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        var dto = CommentDto.FromEntity(comment);

        Assert.False(dto.AuthorHasAvatar);
    }
}
