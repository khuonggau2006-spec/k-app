using TaskMgmt.Application.Common.Models;
using TaskMgmt.Application.Features.Attachments.Commands.UploadAttachment;
using TaskMgmt.Application.UnitTests.Common;

namespace TaskMgmt.Application.UnitTests.Features.Attachments;

public class UploadAttachmentCommandValidatorTests
{
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];

    private static StorageSettings CreateStorageSettings(long maxFileSizeBytes = 1024 * 1024) => new()
    {
        Endpoint = "http://localhost:9000",
        AccessKey = "test",
        SecretKey = "test",
        BucketName = "test-bucket",
        MaxFileSizeBytes = maxFileSizeBytes,
    };

    [Fact]
    public async Task Validate_ValidPngFile_IsValid()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        context.WorkTasks.Add(task);
        await context.SaveChangesAsync(default);

        var validator = new UploadAttachmentCommandValidator(context, CreateStorageSettings());
        var command = new UploadAttachmentCommand(task.Id, "photo.png", PngHeader.Length, new MemoryStream(PngHeader));

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_UnsupportedExtension_IsInvalid()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        context.WorkTasks.Add(task);
        await context.SaveChangesAsync(default);

        var validator = new UploadAttachmentCommandValidator(context, CreateStorageSettings());
        var command = new UploadAttachmentCommand(task.Id, "malware.exe", 10, new MemoryStream([1, 2, 3]));

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UploadAttachmentCommand.FileName));
    }

    [Fact]
    public async Task Validate_FileExceedsMaxSize_IsInvalid()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        context.WorkTasks.Add(task);
        await context.SaveChangesAsync(default);

        var validator = new UploadAttachmentCommandValidator(context, CreateStorageSettings(maxFileSizeBytes: 100));
        var command = new UploadAttachmentCommand(task.Id, "photo.png", 200, new MemoryStream(PngHeader));

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UploadAttachmentCommand.SizeBytes));
    }

    [Fact]
    public async Task Validate_EmptyFile_IsInvalid()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        context.WorkTasks.Add(task);
        await context.SaveChangesAsync(default);

        var validator = new UploadAttachmentCommandValidator(context, CreateStorageSettings());
        var command = new UploadAttachmentCommand(task.Id, "photo.png", 0, new MemoryStream());

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UploadAttachmentCommand.SizeBytes));
    }

    [Fact]
    public async Task Validate_ContentDoesNotMatchDeclaredExtension_IsInvalid()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        context.WorkTasks.Add(task);
        await context.SaveChangesAsync(default);

        // Nội dung thực chất là text thuần nhưng đặt tên .png (giả mạo đuôi file).
        var fakeContent = "khong phai la anh png"u8.ToArray();
        var validator = new UploadAttachmentCommandValidator(context, CreateStorageSettings());
        var command = new UploadAttachmentCommand(task.Id, "fake.png", fakeContent.Length, new MemoryStream(fakeContent));

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UploadAttachmentCommand.FileName)
            && e.ErrorMessage.Contains("không khớp"));
    }

    [Fact]
    public async Task Validate_NonExistentWorkTask_IsInvalid()
    {
        using var context = TestDbContextFactory.Create();

        var validator = new UploadAttachmentCommandValidator(context, CreateStorageSettings());
        var command = new UploadAttachmentCommand(Guid.NewGuid(), "photo.png", PngHeader.Length, new MemoryStream(PngHeader));

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UploadAttachmentCommand.WorkTaskId));
    }
}
