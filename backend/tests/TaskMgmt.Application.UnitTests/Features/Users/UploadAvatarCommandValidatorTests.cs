using TaskMgmt.Application.Features.Users.Commands.UploadAvatar;

namespace TaskMgmt.Application.UnitTests.Features.Users;

public class UploadAvatarCommandValidatorTests
{
    private static readonly byte[] JpegBytes = [0xFF, 0xD8, 0xFF, 0x00, 0x00, 0x00];

    [Fact]
    public void Validate_ValidJpeg_Passes()
    {
        var content = new MemoryStream(JpegBytes);
        var command = new UploadAvatarCommand("photo.jpg", content.Length, content);

        var result = new UploadAvatarCommandValidator().Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_DisallowedExtension_Fails()
    {
        var content = new MemoryStream(JpegBytes);
        var command = new UploadAvatarCommand("file.pdf", content.Length, content);

        var result = new UploadAvatarCommandValidator().Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_TooLarge_Fails()
    {
        var content = new MemoryStream(JpegBytes);
        var command = new UploadAvatarCommand("photo.jpg", 5 * 1024 * 1024 + 1, content);

        var result = new UploadAvatarCommandValidator().Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ContentDoesNotMatchExtension_Fails()
    {
        // .jpg nhưng nội dung là PNG signature - giả mạo đuôi file.
        var content = new MemoryStream([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        var command = new UploadAvatarCommand("photo.jpg", content.Length, content);

        var result = new UploadAvatarCommandValidator().Validate(command);

        Assert.False(result.IsValid);
    }
}
