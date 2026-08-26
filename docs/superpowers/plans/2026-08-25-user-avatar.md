# Avatar người dùng Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Cho phép mỗi user tự upload/đổi/xoá avatar của mình qua màn "Hồ sơ của tôi" mới, và hiển thị avatar thật (thay chữ cái đầu) ở assignee list + comment list.

**Architecture:** Backend thêm `AvatarStorageKey` trên `User`, 3 endpoint mới trên `UsersController` (`POST/DELETE /me/avatar`, `GET /{id}/avatar`) tái dùng `IFileStorageService` (S3/MinIO) đã có cho Attachment. `UserDto`/`TaskAssigneeDto`/`CommentDto` thêm cờ `HasAvatar` để mobile biết có nên tải ảnh hay hiện chữ cái đầu ngay. Mobile có widget `UserAvatar` dùng chung, cache ảnh qua Riverpod `FutureProvider.family` theo `userId`, và màn `ProfileScreen` mới để tự đổi avatar.

**Tech Stack:** .NET 10 (MediatR/CQRS, FluentValidation, EF Core/PostgreSQL, S3FileStorageService/MinIO), Flutter (Riverpod, Dio, image_picker, go_router).

**Spec:** `docs/superpowers/specs/2026-08-25-user-avatar-design.md`

## Global Constraints

- Định dạng avatar cho phép: `.jpg/.jpeg/.png/.webp` — kiểm tra đuôi + magic bytes.
- Dung lượng tối đa: 5MB (`5 * 1024 * 1024` bytes), cố định, không cấu hình.
- Không lộ `AvatarStorageKey`/URL S3 ra API — chỉ trả cờ `HasAvatar: bool`; ảnh luôn stream qua endpoint có xác thực.
- Mỗi user chỉ có 1 avatar tại 1 thời điểm — upload mới luôn xoá + thay thế ảnh cũ trong storage.
- Ngoài phạm vi: avatar trong danh sách thông báo, avatar trong task history timeline (đó là icon loại sự kiện, không phải người dùng), crop/resize ảnh phía client, presigned/public URL.

---

## Task 1: Domain — `AvatarStorageKey` trên `User` + migration

**Files:**
- Modify: `backend/src/TaskMgmt.Domain/Entities/User.cs`
- Create: migration qua `dotnet ef migrations add AddUserAvatar` (sinh file trong `backend/src/TaskMgmt.Infrastructure/Persistence/Migrations/`)

**Interfaces:**
- Produces: `User.AvatarStorageKey` (`string?`) — mọi task backend sau đều đọc/ghi field này.

- [ ] **Step 1: Thêm field vào entity**

Trong `backend/src/TaskMgmt.Domain/Entities/User.cs`, thêm dòng sau `IsActive`:

```csharp
    public bool IsActive { get; set; } = true;
    public string? AvatarStorageKey { get; set; }
```

- [ ] **Step 2: Build để xác nhận không lỗi biên dịch**

Run (từ `backend/`): `dotnet build TaskMgmt.slnx`
Expected: Build succeeded.

- [ ] **Step 3: Sinh migration**

Run (từ `backend/src/TaskMgmt.Infrastructure`):
```bash
dotnet ef migrations add AddUserAvatar --startup-project ../TaskMgmt.API
```
Expected: file mới `<timestamp>_AddUserAvatar.cs` được tạo, chứa `migrationBuilder.AddColumn<string>(name: "AvatarStorageKey", table: "Users", type: "text", nullable: true);` — mở file vừa sinh để xác nhận đúng nội dung này (không có thay đổi nào khác ngoài dự kiến).

- [ ] **Step 4: Áp migration vào DB dev**

Run (vẫn ở `backend/src/TaskMgmt.Infrastructure`, cần Docker Postgres đang chạy — `docker compose up -d` ở repo root nếu chưa):
```bash
dotnet ef database update --startup-project ../TaskMgmt.API
```
Expected: `Done.` không lỗi.

- [ ] **Step 5: Commit**

```bash
cd backend
git add src/TaskMgmt.Domain/Entities/User.cs src/TaskMgmt.Infrastructure/Persistence/Migrations/
git commit -m "feat(backend): add AvatarStorageKey column to User"
```

---

## Task 2: `UserDto.HasAvatar` — ripple qua Auth + GetUsers

**Files:**
- Modify: `backend/src/TaskMgmt.Application/Features/Auth/Common/AuthResultDto.cs`
- Modify: `backend/src/TaskMgmt.Application/Features/Users/Queries/GetUsers/GetUsersQueryHandler.cs`
- Test: `backend/tests/TaskMgmt.Application.UnitTests/Features/Users/GetUsersQueryHandlerTests.cs` (mới)

**Interfaces:**
- Consumes: `User.AvatarStorageKey` (Task 1).
- Produces: `UserDto(Guid Id, string Email, string FullName, SystemRole SystemRole, bool HasAvatar)` — mọi task backend sau (Upload/Delete avatar handler) trả kiểu này.

`LoginCommandHandler`/`RegisterCommandHandler`/`RefreshTokenCommandHandler` đều gọi `UserDto.FromEntity(user)` — sửa 1 chỗ (`FromEntity`) là đủ cho cả 3, không cần sửa riêng từng handler.

- [ ] **Step 1: Viết test trước cho `GetUsersQueryHandler`**

Tạo `backend/tests/TaskMgmt.Application.UnitTests/Features/Users/GetUsersQueryHandlerTests.cs`:

```csharp
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
```

- [ ] **Step 2: Chạy test, xác nhận FAIL (biên dịch lỗi vì `HasAvatar` chưa tồn tại)**

Run (từ `backend/`): `dotnet test tests/TaskMgmt.Application.UnitTests --filter "FullyQualifiedName~GetUsersQueryHandlerTests"`
Expected: FAIL — lỗi biên dịch `'UserDto' does not contain a definition for 'HasAvatar'`.

- [ ] **Step 3: Sửa `UserDto` + `FromEntity`**

Trong `backend/src/TaskMgmt.Application/Features/Auth/Common/AuthResultDto.cs`:

```csharp
public record UserDto(Guid Id, string Email, string FullName, SystemRole SystemRole, bool HasAvatar)
{
    public static UserDto FromEntity(User user) => new(user.Id, user.Email, user.FullName, user.SystemRole, user.AvatarStorageKey != null);
}
```

- [ ] **Step 4: Sửa `GetUsersQueryHandler`**

Trong `backend/src/TaskMgmt.Application/Features/Users/Queries/GetUsers/GetUsersQueryHandler.cs`, dòng `.Select(...)`:

```csharp
            .Select(u => new UserDto(u.Id, u.Email, u.FullName, u.SystemRole, u.AvatarStorageKey != null))
```

- [ ] **Step 5: Chạy lại test, xác nhận PASS**

Run: `dotnet test tests/TaskMgmt.Application.UnitTests --filter "FullyQualifiedName~GetUsersQueryHandlerTests"`
Expected: PASS (1/1).

- [ ] **Step 6: Build toàn bộ để bắt các chỗ khác dùng constructor `UserDto` cũ**

Run: `dotnet build TaskMgmt.slnx`
Expected: Build succeeded — nếu lỗi "not enough arguments" ở đâu đó ngoài các file đã sửa, đó là chỗ dựng `UserDto` thủ công còn sót (tìm bằng `grep -rn "new UserDto(" backend/src`), sửa nốt trước khi tiếp tục.

- [ ] **Step 7: Commit**

```bash
cd backend
git add src/TaskMgmt.Application/Features/Auth/Common/AuthResultDto.cs src/TaskMgmt.Application/Features/Users/Queries/GetUsers/GetUsersQueryHandler.cs tests/TaskMgmt.Application.UnitTests/Features/Users/GetUsersQueryHandlerTests.cs
git commit -m "feat(backend): add HasAvatar flag to UserDto"
```

---

## Task 3: `UploadAvatarCommand` + Validator + Handler

**Files:**
- Create: `backend/src/TaskMgmt.Application/Features/Users/Commands/UploadAvatar/UploadAvatarCommand.cs`
- Create: `backend/src/TaskMgmt.Application/Features/Users/Commands/UploadAvatar/UploadAvatarCommandValidator.cs`
- Create: `backend/src/TaskMgmt.Application/Features/Users/Commands/UploadAvatar/UploadAvatarCommandHandler.cs`
- Modify: `backend/tests/TaskMgmt.Application.UnitTests/Common/FakeFileStorageService.cs` (thêm khả năng ghi lại lời gọi, dùng để assert)
- Test: `backend/tests/TaskMgmt.Application.UnitTests/Features/Users/UploadAvatarCommandHandlerTests.cs`
- Test: `backend/tests/TaskMgmt.Application.UnitTests/Features/Users/UploadAvatarCommandValidatorTests.cs`

**Interfaces:**
- Consumes: `UserDto` (Task 2), `IFileStorageService` (đã có, `backend/src/TaskMgmt.Application/Common/Interfaces/IFileStorageService.cs`), `AttachmentFileValidator.TryGetAllowedContentType`/`MatchesSignature` (đã có, `backend/src/TaskMgmt.Application/Features/Attachments/Common/AttachmentFileValidator.cs`), `ICurrentUserService.UserId` (đã có).
- Produces: `UploadAvatarCommand(string FileName, long SizeBytes, Stream Content) : IRequest<UserDto>` — dùng ở Task 6 (controller).

- [ ] **Step 1: Mở rộng `FakeFileStorageService` để ghi lại lời gọi (không phá test cũ)**

Đọc `backend/tests/TaskMgmt.Application.UnitTests/Common/FakeFileStorageService.cs` hiện tại, thay bằng:

```csharp
using TaskMgmt.Application.Common.Interfaces;

namespace TaskMgmt.Application.UnitTests.Common;

internal class FakeFileStorageService : IFileStorageService
{
    public List<string> UploadedKeys { get; } = [];
    public List<string> DeletedKeys { get; } = [];

    public Task UploadAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken)
    {
        UploadedKeys.Add(storageKey);
        return Task.CompletedTask;
    }

    public Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken) =>
        Task.FromResult<Stream>(new MemoryStream());

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        DeletedKeys.Add(storageKey);
        return Task.CompletedTask;
    }
}
```

Test cũ (`DeleteAttachmentCommandHandlerTests`, `UploadAttachmentCommandValidatorTests`) không đọc `UploadedKeys`/`DeletedKeys` nên không bị ảnh hưởng.

- [ ] **Step 2: Viết test trước cho handler (upload lần đầu)**

Tạo `backend/tests/TaskMgmt.Application.UnitTests/Features/Users/UploadAvatarCommandHandlerTests.cs`:

```csharp
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
```

- [ ] **Step 3: Chạy test, xác nhận FAIL (không biên dịch được vì chưa có class)**

Run: `dotnet test tests/TaskMgmt.Application.UnitTests --filter "FullyQualifiedName~UploadAvatarCommandHandlerTests"`
Expected: FAIL — build error, không tìm thấy `UploadAvatarCommand`/`UploadAvatarCommandHandler`.

- [ ] **Step 4: Tạo command + handler**

`backend/src/TaskMgmt.Application/Features/Users/Commands/UploadAvatar/UploadAvatarCommand.cs`:

```csharp
using MediatR;
using TaskMgmt.Application.Features.Auth.Common;

namespace TaskMgmt.Application.Features.Users.Commands.UploadAvatar;

public record UploadAvatarCommand(string FileName, long SizeBytes, Stream Content) : IRequest<UserDto>;
```

`backend/src/TaskMgmt.Application/Features/Users/Commands/UploadAvatar/UploadAvatarCommandHandler.cs`:

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Attachments.Common;
using TaskMgmt.Application.Features.Auth.Common;

namespace TaskMgmt.Application.Features.Users.Commands.UploadAvatar;

public class UploadAvatarCommandHandler(IApplicationDbContext context, IFileStorageService storage, ICurrentUserService currentUser)
    : IRequestHandler<UploadAvatarCommand, UserDto>
{
    public async Task<UserDto> Handle(UploadAvatarCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;
        var user = await context.Users.FirstAsync(u => u.Id == userId, cancellationToken);

        AttachmentFileValidator.TryGetAllowedContentType(request.FileName, out var contentType);

        if (user.AvatarStorageKey != null)
        {
            await storage.DeleteAsync(user.AvatarStorageKey, cancellationToken);
        }

        // Khoá lưu trữ ngẫu nhiên theo user, không dựa vào FileName gốc - tránh path traversal/đè file.
        var storageKey = $"avatars/{userId}/{Guid.NewGuid()}{Path.GetExtension(request.FileName)}";

        request.Content.Position = 0;
        await storage.UploadAsync(storageKey, request.Content, contentType, cancellationToken);

        user.AvatarStorageKey = storageKey;
        await context.SaveChangesAsync(cancellationToken);

        return UserDto.FromEntity(user);
    }
}
```

- [ ] **Step 5: Chạy lại test, xác nhận PASS**

Run: `dotnet test tests/TaskMgmt.Application.UnitTests --filter "FullyQualifiedName~UploadAvatarCommandHandlerTests"`
Expected: PASS (2/2).

- [ ] **Step 6: Viết test cho validator (trước khi viết validator)**

Tạo `backend/tests/TaskMgmt.Application.UnitTests/Features/Users/UploadAvatarCommandValidatorTests.cs`:

```csharp
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
```

- [ ] **Step 7: Chạy test validator, xác nhận FAIL**

Run: `dotnet test tests/TaskMgmt.Application.UnitTests --filter "FullyQualifiedName~UploadAvatarCommandValidatorTests"`
Expected: FAIL — không tìm thấy `UploadAvatarCommandValidator`.

- [ ] **Step 8: Tạo validator**

`backend/src/TaskMgmt.Application/Features/Users/Commands/UploadAvatar/UploadAvatarCommandValidator.cs`:

```csharp
using FluentValidation;
using TaskMgmt.Application.Features.Attachments.Common;

namespace TaskMgmt.Application.Features.Users.Commands.UploadAvatar;

public class UploadAvatarCommandValidator : AbstractValidator<UploadAvatarCommand>
{
    private const long MaxSizeBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public UploadAvatarCommandValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);

        RuleFor(x => x.FileName)
            .Must(fileName => AllowedExtensions.Contains(Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase))
            .WithMessage("Chỉ nhận ảnh JPG/PNG/WEBP.");

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0)
            .WithMessage("Ảnh rỗng.")
            .LessThanOrEqualTo(MaxSizeBytes)
            .WithMessage("Dung lượng ảnh vượt quá 5MB.");

        // Đối chiếu magic bytes với định dạng khai báo qua đuôi file để chặn file giả mạo đuôi.
        RuleFor(x => x)
            .Must(x =>
            {
                if (!AllowedExtensions.Contains(Path.GetExtension(x.FileName), StringComparer.OrdinalIgnoreCase)
                    || !AttachmentFileValidator.TryGetAllowedContentType(x.FileName, out var contentType))
                {
                    return true; // đã báo lỗi ở rule FileName phía trên, tránh trùng lỗi.
                }

                Span<byte> header = stackalloc byte[16];
                var bytesRead = x.Content.Read(header);
                x.Content.Position = 0;
                return AttachmentFileValidator.MatchesSignature(contentType, header[..bytesRead]);
            })
            .WithMessage("Nội dung ảnh không khớp với định dạng khai báo.")
            .WithName(nameof(UploadAvatarCommand.FileName));
    }
}
```

- [ ] **Step 9: Chạy lại test validator, xác nhận PASS**

Run: `dotnet test tests/TaskMgmt.Application.UnitTests --filter "FullyQualifiedName~UploadAvatarCommandValidatorTests"`
Expected: PASS (4/4).

- [ ] **Step 10: Commit**

```bash
cd backend
git add src/TaskMgmt.Application/Features/Users/Commands/UploadAvatar/ tests/TaskMgmt.Application.UnitTests/Common/FakeFileStorageService.cs tests/TaskMgmt.Application.UnitTests/Features/Users/UploadAvatarCommand*.cs
git commit -m "feat(backend): add UploadAvatarCommand with format/size validation"
```

---

## Task 4: `DeleteAvatarCommand` + Handler

**Files:**
- Create: `backend/src/TaskMgmt.Application/Features/Users/Commands/DeleteAvatar/DeleteAvatarCommand.cs`
- Create: `backend/src/TaskMgmt.Application/Features/Users/Commands/DeleteAvatar/DeleteAvatarCommandHandler.cs`
- Test: `backend/tests/TaskMgmt.Application.UnitTests/Features/Users/DeleteAvatarCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `UserDto` (Task 2), `IFileStorageService`, `ICurrentUserService`.
- Produces: `DeleteAvatarCommand : IRequest<UserDto>` — dùng ở Task 6.

- [ ] **Step 1: Viết test trước**

Tạo `backend/tests/TaskMgmt.Application.UnitTests/Features/Users/DeleteAvatarCommandHandlerTests.cs`:

```csharp
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
```

- [ ] **Step 2: Chạy test, xác nhận FAIL**

Run: `dotnet test tests/TaskMgmt.Application.UnitTests --filter "FullyQualifiedName~DeleteAvatarCommandHandlerTests"`
Expected: FAIL — không tìm thấy `DeleteAvatarCommand`/`DeleteAvatarCommandHandler`.

- [ ] **Step 3: Tạo command + handler**

`backend/src/TaskMgmt.Application/Features/Users/Commands/DeleteAvatar/DeleteAvatarCommand.cs`:

```csharp
using MediatR;
using TaskMgmt.Application.Features.Auth.Common;

namespace TaskMgmt.Application.Features.Users.Commands.DeleteAvatar;

public record DeleteAvatarCommand : IRequest<UserDto>;
```

`backend/src/TaskMgmt.Application/Features/Users/Commands/DeleteAvatar/DeleteAvatarCommandHandler.cs`:

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Auth.Common;

namespace TaskMgmt.Application.Features.Users.Commands.DeleteAvatar;

public class DeleteAvatarCommandHandler(IApplicationDbContext context, IFileStorageService storage, ICurrentUserService currentUser)
    : IRequestHandler<DeleteAvatarCommand, UserDto>
{
    public async Task<UserDto> Handle(DeleteAvatarCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;
        var user = await context.Users.FirstAsync(u => u.Id == userId, cancellationToken);

        if (user.AvatarStorageKey != null)
        {
            await storage.DeleteAsync(user.AvatarStorageKey, cancellationToken);
            user.AvatarStorageKey = null;
            await context.SaveChangesAsync(cancellationToken);
        }

        return UserDto.FromEntity(user);
    }
}
```

- [ ] **Step 4: Chạy lại test, xác nhận PASS**

Run: `dotnet test tests/TaskMgmt.Application.UnitTests --filter "FullyQualifiedName~DeleteAvatarCommandHandlerTests"`
Expected: PASS (2/2).

- [ ] **Step 5: Commit**

```bash
cd backend
git add src/TaskMgmt.Application/Features/Users/Commands/DeleteAvatar/ tests/TaskMgmt.Application.UnitTests/Features/Users/DeleteAvatarCommandHandlerTests.cs
git commit -m "feat(backend): add DeleteAvatarCommand"
```

---

## Task 5: `GetUserAvatarQuery` + Handler

**Files:**
- Create: `backend/src/TaskMgmt.Application/Features/Users/Queries/GetUserAvatar/GetUserAvatarQuery.cs`
- Create: `backend/src/TaskMgmt.Application/Features/Users/Queries/GetUserAvatar/GetUserAvatarQueryHandler.cs`
- Test: `backend/tests/TaskMgmt.Application.UnitTests/Features/Users/GetUserAvatarQueryHandlerTests.cs`

**Interfaces:**
- Consumes: `IFileStorageService`, `AttachmentFileValidator.TryGetAllowedContentType`, `NotFoundException` (`backend/src/TaskMgmt.Application/Common/Exceptions/NotFoundException.cs`).
- Produces: `GetUserAvatarQuery(Guid UserId) : IRequest<UserAvatarResult>`, `UserAvatarResult(Stream Content, string ContentType)` — dùng ở Task 6.

- [ ] **Step 1: Viết test trước**

Tạo `backend/tests/TaskMgmt.Application.UnitTests/Features/Users/GetUserAvatarQueryHandlerTests.cs`:

```csharp
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
```

- [ ] **Step 2: Chạy test, xác nhận FAIL**

Run: `dotnet test tests/TaskMgmt.Application.UnitTests --filter "FullyQualifiedName~GetUserAvatarQueryHandlerTests"`
Expected: FAIL — không tìm thấy `GetUserAvatarQuery`/`GetUserAvatarQueryHandler`.

- [ ] **Step 3: Tạo query + handler**

`backend/src/TaskMgmt.Application/Features/Users/Queries/GetUserAvatar/GetUserAvatarQuery.cs`:

```csharp
using MediatR;

namespace TaskMgmt.Application.Features.Users.Queries.GetUserAvatar;

public record GetUserAvatarQuery(Guid UserId) : IRequest<UserAvatarResult>;

public record UserAvatarResult(Stream Content, string ContentType);
```

`backend/src/TaskMgmt.Application/Features/Users/Queries/GetUserAvatar/GetUserAvatarQueryHandler.cs`:

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Attachments.Common;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.Users.Queries.GetUserAvatar;

public class GetUserAvatarQueryHandler(IApplicationDbContext context, IFileStorageService storage)
    : IRequestHandler<GetUserAvatarQuery, UserAvatarResult>
{
    public async Task<UserAvatarResult> Handle(GetUserAvatarQuery request, CancellationToken cancellationToken)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        if (user.AvatarStorageKey == null)
        {
            throw new NotFoundException(nameof(User.AvatarStorageKey), request.UserId);
        }

        // Đuôi file được giữ nguyên lúc tạo storage key (UploadAvatarCommandHandler) nên suy
        // content-type trực tiếp từ đó, không cần lưu cột content-type riêng.
        AttachmentFileValidator.TryGetAllowedContentType(user.AvatarStorageKey, out var contentType);
        var stream = await storage.DownloadAsync(user.AvatarStorageKey, cancellationToken);

        return new UserAvatarResult(stream, contentType);
    }
}
```

- [ ] **Step 4: Chạy lại test, xác nhận PASS**

Run: `dotnet test tests/TaskMgmt.Application.UnitTests --filter "FullyQualifiedName~GetUserAvatarQueryHandlerTests"`
Expected: PASS (2/2).

- [ ] **Step 5: Commit**

```bash
cd backend
git add src/TaskMgmt.Application/Features/Users/Queries/GetUserAvatar/ tests/TaskMgmt.Application.UnitTests/Features/Users/GetUserAvatarQueryHandlerTests.cs
git commit -m "feat(backend): add GetUserAvatarQuery"
```

---

## Task 6: `UsersController` — 3 endpoint avatar

**Files:**
- Modify: `backend/src/TaskMgmt.API/Controllers/UsersController.cs`

**Interfaces:**
- Consumes: `UploadAvatarCommand` (Task 3), `DeleteAvatarCommand` (Task 4), `GetUserAvatarQuery`/`UserAvatarResult` (Task 5).
- Produces: `POST /api/v1/users/me/avatar`, `DELETE /api/v1/users/me/avatar`, `GET /api/v1/users/{id}/avatar` — dùng bởi mobile Task 10.

Không có unit test riêng cho controller (đúng pattern hiện có — `AttachmentsController` cũng không có test controller riêng, được phủ qua handler tests + xác minh thủ công).

- [ ] **Step 1: Sửa `UsersController`**

Thay toàn bộ nội dung `backend/src/TaskMgmt.API/Controllers/UsersController.cs`:

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TaskMgmt.Application.Features.Auth.Common;
using TaskMgmt.Application.Features.Users.Commands.DeleteAvatar;
using TaskMgmt.Application.Features.Users.Commands.UploadAvatar;
using TaskMgmt.Application.Features.Users.Queries.GetUserAvatar;
using TaskMgmt.Application.Features.Users.Queries.GetUsers;

namespace TaskMgmt.API.Controllers;

[ApiController]
[Route("api/v1/users")]
public class UsersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUsersQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("me/avatar")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<UserDto>> UploadAvatar(IFormFile file, CancellationToken cancellationToken)
    {
        var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        var result = await sender.Send(new UploadAvatarCommand(file.FileName, file.Length, buffer), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("me/avatar")]
    public async Task<ActionResult<UserDto>> DeleteAvatar(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteAvatarCommand(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/avatar")]
    public async Task<IActionResult> GetAvatar(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUserAvatarQuery(id), cancellationToken);
        return File(result.Content, result.ContentType);
    }
}
```

- [ ] **Step 2: Build toàn bộ**

Run (từ `backend/`): `dotnet build TaskMgmt.slnx`
Expected: Build succeeded.

- [ ] **Step 3: Xác minh thủ công qua Swagger**

Run: `cd src/TaskMgmt.API && dotnet run` (nếu Docker Postgres chưa chạy: `docker compose up -d` ở repo root trước) — mở `http://localhost:5299/swagger`, đăng nhập lấy access token (nút Authorize), thử `POST /api/v1/users/me/avatar` với 1 ảnh jpg nhỏ, xác nhận trả `200` kèm `hasAvatar: true`; thử `GET /api/v1/users/{id}/avatar` với chính id đó, xác nhận trả ảnh; thử `DELETE /api/v1/users/me/avatar`, xác nhận `hasAvatar: false`; gọi lại `GET .../avatar` xác nhận `404`.

- [ ] **Step 4: Commit**

```bash
cd backend
git add src/TaskMgmt.API/Controllers/UsersController.cs
git commit -m "feat(backend): expose avatar upload/delete/download endpoints"
```

---

## Task 7: `TaskAssigneeDto.UserHasAvatar` — ripple qua 3 handler

**Files:**
- Modify: `backend/src/TaskMgmt.Application/Features/TaskAssignees/Common/TaskAssigneeDto.cs`
- Modify: `backend/src/TaskMgmt.Application/Features/TaskAssignees/Queries/GetTaskAssignees/GetTaskAssigneesQueryHandler.cs`
- Modify: `backend/src/TaskMgmt.Application/Features/TaskAssignees/Commands/AddTaskAssignee/AddTaskAssigneeCommandHandler.cs`
- Modify: `backend/src/TaskMgmt.Application/Features/TaskAssignees/Commands/ChangeTaskAssigneeRole/ChangeTaskAssigneeRoleCommandHandler.cs`
- Test: `backend/tests/TaskMgmt.Application.UnitTests/Features/TaskAssignees/GetTaskAssigneesQueryHandlerTests.cs` (mới)

**Interfaces:**
- Consumes: `User.AvatarStorageKey` (Task 1).
- Produces: `TaskAssigneeDto(..., bool UserHasAvatar)` — dùng ở mobile Task 12.

- [ ] **Step 1: Viết test trước**

Tạo `backend/tests/TaskMgmt.Application.UnitTests/Features/TaskAssignees/GetTaskAssigneesQueryHandlerTests.cs`:

```csharp
using TaskMgmt.Application.Features.TaskAssignees.Queries.GetTaskAssignees;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.UnitTests.Features.TaskAssignees;

public class GetTaskAssigneesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsUserHasAvatarTrue_WhenAssigneeHasAvatarStorageKey()
    {
        using var context = TestDbContextFactory.Create();
        var task = TestDataFactory.CreateWorkTask();
        var user = TestDataFactory.CreateUser();
        user.AvatarStorageKey = "avatars/x/y.jpg";
        context.WorkTasks.Add(task);
        context.Users.Add(user);
        context.TaskAssignees.Add(TestDataFactory.CreateAssignee(task.Id, user.Id, TaskAssigneeRole.Assignee));
        await context.SaveChangesAsync(default);

        var handler = new GetTaskAssigneesQueryHandler(context);
        var result = await handler.Handle(new GetTaskAssigneesQuery(task.Id), default);

        Assert.True(result.Single().UserHasAvatar);
    }
}
```

- [ ] **Step 2: Chạy test, xác nhận FAIL**

Run: `dotnet test tests/TaskMgmt.Application.UnitTests --filter "FullyQualifiedName~GetTaskAssigneesQueryHandlerTests"`
Expected: FAIL — biên dịch lỗi `'TaskAssigneeDto' does not contain a definition for 'UserHasAvatar'`.

- [ ] **Step 3: Sửa `TaskAssigneeDto`**

`backend/src/TaskMgmt.Application/Features/TaskAssignees/Common/TaskAssigneeDto.cs`:

```csharp
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.Features.TaskAssignees.Common;

public record TaskAssigneeDto(
    Guid Id,
    Guid WorkTaskId,
    Guid UserId,
    string UserFullName,
    string UserEmail,
    bool UserHasAvatar,
    TaskAssigneeRole Role,
    DateTimeOffset AssignedAtUtc);
```

- [ ] **Step 4: Sửa 3 chỗ dựng `TaskAssigneeDto`**

`GetTaskAssigneesQueryHandler.cs` dòng `.Select(...)`:

```csharp
            .Select(a => new TaskAssigneeDto(
                a.Id, a.WorkTaskId, a.UserId, a.User!.FullName, a.User!.Email, a.User!.AvatarStorageKey != null, a.Role, a.AssignedAtUtc))
```

`AddTaskAssigneeCommandHandler.cs` dòng `return new TaskAssigneeDto(...)`:

```csharp
        return new TaskAssigneeDto(assignee.Id, assignee.WorkTaskId, assignee.UserId, user.FullName, user.Email, user.AvatarStorageKey != null, assignee.Role, assignee.AssignedAtUtc);
```

`ChangeTaskAssigneeRoleCommandHandler.cs` dòng `return new TaskAssigneeDto(...)`:

```csharp
        return new TaskAssigneeDto(
            assignee.Id, assignee.WorkTaskId, assignee.UserId, assignee.User!.FullName, assignee.User!.Email, assignee.User!.AvatarStorageKey != null, assignee.Role, assignee.AssignedAtUtc);
```

- [ ] **Step 5: Chạy lại test, xác nhận PASS**

Run: `dotnet test tests/TaskMgmt.Application.UnitTests --filter "FullyQualifiedName~GetTaskAssigneesQueryHandlerTests"`
Expected: PASS (1/1).

- [ ] **Step 6: Build + chạy toàn bộ test TaskAssignees để không phá test cũ**

Run: `dotnet build TaskMgmt.slnx` rồi `dotnet test tests/TaskMgmt.Application.UnitTests --filter "FullyQualifiedName~TaskAssignees"`
Expected: Build succeeded; toàn bộ test TaskAssignees (cũ + mới) PASS.

- [ ] **Step 7: Commit**

```bash
cd backend
git add src/TaskMgmt.Application/Features/TaskAssignees/ tests/TaskMgmt.Application.UnitTests/Features/TaskAssignees/GetTaskAssigneesQueryHandlerTests.cs
git commit -m "feat(backend): add UserHasAvatar to TaskAssigneeDto"
```

---

## Task 8: `CommentDto.AuthorHasAvatar`

**Files:**
- Modify: `backend/src/TaskMgmt.Application/Features/Comments/Common/CommentDto.cs`
- Test: `backend/tests/TaskMgmt.Application.UnitTests/Features/Comments/CommentDtoTests.cs` (mới)

**Interfaces:**
- Consumes: `User.AvatarStorageKey` (Task 1).
- Produces: `CommentDto(..., bool AuthorHasAvatar)` — dùng ở mobile Task 12.

`CommentDto.FromEntity` là nơi dựng duy nhất (đã xác nhận qua `grep -rn "new CommentDto("` chỉ khớp định nghĩa record, không có chỗ dựng thủ công nào khác) — chỉ cần sửa 1 chỗ.

- [ ] **Step 1: Viết test trước**

Tạo `backend/tests/TaskMgmt.Application.UnitTests/Features/Comments/CommentDtoTests.cs`:

```csharp
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
```

- [ ] **Step 2: Chạy test, xác nhận FAIL**

Run: `dotnet test tests/TaskMgmt.Application.UnitTests --filter "FullyQualifiedName~CommentDtoTests"`
Expected: FAIL — biên dịch lỗi `'CommentDto' does not contain a definition for 'AuthorHasAvatar'`.

- [ ] **Step 3: Sửa `CommentDto`**

`backend/src/TaskMgmt.Application/Features/Comments/Common/CommentDto.cs`:

```csharp
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.Comments.Common;

public record CommentMentionDto(Guid UserId, string FullName, string Email);

public record CommentDto(
    Guid Id,
    Guid WorkTaskId,
    string Content,
    Guid? AuthorUserId,
    string AuthorFullName,
    string AuthorEmail,
    bool AuthorHasAvatar,
    DateTimeOffset CreatedAtUtc,
    List<CommentMentionDto> Mentions)
{
    // Yêu cầu Comment đã được load kèm Author và Mentions.MentionedUser (Include/ThenInclude).
    public static CommentDto FromEntity(Comment comment) => new(
        comment.Id,
        comment.WorkTaskId,
        comment.Content,
        comment.CreatedByUserId,
        comment.Author?.FullName ?? string.Empty,
        comment.Author?.Email ?? string.Empty,
        comment.Author?.AvatarStorageKey != null,
        comment.CreatedAtUtc,
        comment.Mentions
            .Select(m => new CommentMentionDto(m.MentionedUserId, m.MentionedUser?.FullName ?? string.Empty, m.MentionedUser?.Email ?? string.Empty))
            .ToList());
}
```

- [ ] **Step 4: Chạy lại test, xác nhận PASS**

Run: `dotnet test tests/TaskMgmt.Application.UnitTests --filter "FullyQualifiedName~CommentDtoTests"`
Expected: PASS (2/2).

- [ ] **Step 5: Build + chạy toàn bộ test suite backend**

Run: `dotnet build TaskMgmt.slnx` rồi `dotnet test TaskMgmt.slnx`
Expected: Build succeeded; toàn bộ test PASS (không có test nào bị phá bởi các ripple DTO ở Task 2/7/8).

- [ ] **Step 6: Commit**

```bash
cd backend
git add src/TaskMgmt.Application/Features/Comments/Common/CommentDto.cs tests/TaskMgmt.Application.UnitTests/Features/Comments/CommentDtoTests.cs
git commit -m "feat(backend): add AuthorHasAvatar to CommentDto"
```

---

## Task 9: Mobile — `User.hasAvatar` + `AuthController.updateUser`

**Files:**
- Modify: `mobile/taskmgmt_app/lib/features/auth/domain/entities/user.dart`
- Modify: `mobile/taskmgmt_app/lib/features/auth/data/models/user_model.dart`
- Modify: `mobile/taskmgmt_app/lib/features/auth/presentation/providers/auth_provider.dart`
- Test: `mobile/taskmgmt_app/test/auth_provider_test.dart` (mới)

**Interfaces:**
- Produces: `User(..., {required bool hasAvatar})`, `AuthController.updateUser(User user)` — dùng ở mobile Task 10 (upload/xoá avatar cập nhật lại state đăng nhập), Task 11/12/13/14 (đọc `hasAvatar`).

- [ ] **Step 1: Viết test trước cho `AuthController.updateUser`**

Tạo `mobile/taskmgmt_app/test/auth_provider_test.dart`:

```dart
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:taskmgmt_app/features/auth/domain/entities/user.dart';
import 'package:taskmgmt_app/features/auth/domain/repositories/auth_repository.dart';
import 'package:taskmgmt_app/features/auth/presentation/providers/auth_provider.dart';

class _FakeAuthRepository implements AuthRepository {
  @override
  Future<User?> restoreSession() async => null;

  @override
  Future<User> login({required String email, required String password}) async => throw UnimplementedError();

  @override
  Future<User> register({required String email, required String fullName, required String password}) async =>
      throw UnimplementedError();

  @override
  Future<void> logout() async {}
}

void main() {
  test('updateUser replaces auth state with the given user', () async {
    final container = ProviderContainer(
      overrides: [authRepositoryProvider.overrideWithValue(_FakeAuthRepository())],
    );
    addTearDown(container.dispose);

    await container.read(authControllerProvider.future);

    const updated = User(id: '1', email: 'a@b.com', fullName: 'A', systemRole: SystemRole.member, hasAvatar: true);
    container.read(authControllerProvider.notifier).updateUser(updated);

    expect(container.read(authControllerProvider).valueOrNull, updated);
  });
}
```

- [ ] **Step 2: Chạy test, xác nhận FAIL**

Run (từ `mobile/taskmgmt_app`): `flutter test test/auth_provider_test.dart`
Expected: FAIL — biên dịch lỗi (`hasAvatar` không tồn tại trên `User`, `updateUser` không tồn tại trên controller).

- [ ] **Step 3: Sửa `User` entity**

`mobile/taskmgmt_app/lib/features/auth/domain/entities/user.dart`:

```dart
enum SystemRole { admin, manager, member }

SystemRole systemRoleFromString(String value) => switch (value) {
      'Admin' => SystemRole.admin,
      'Manager' => SystemRole.manager,
      _ => SystemRole.member,
    };

class User {
  const User({
    required this.id,
    required this.email,
    required this.fullName,
    required this.systemRole,
    required this.hasAvatar,
  });

  final String id;
  final String email;
  final String fullName;
  final SystemRole systemRole;
  final bool hasAvatar;

  @override
  bool operator ==(Object other) =>
      other is User &&
      other.id == id &&
      other.email == email &&
      other.fullName == fullName &&
      other.systemRole == systemRole &&
      other.hasAvatar == hasAvatar;

  @override
  int get hashCode => Object.hash(id, email, fullName, systemRole, hasAvatar);
}
```

(`==`/`hashCode` thêm mới để test ở Step 1 so sánh `expect(..., updated)` bằng giá trị thay vì identity — `User` trước đây không cần so sánh giá trị ở đâu cả.)

- [ ] **Step 4: Sửa `UserModel`**

`mobile/taskmgmt_app/lib/features/auth/data/models/user_model.dart`:

```dart
import '../../domain/entities/user.dart';

class UserModel {
  const UserModel({
    required this.id,
    required this.email,
    required this.fullName,
    required this.systemRole,
    required this.hasAvatar,
  });

  final String id;
  final String email;
  final String fullName;
  final String systemRole;
  final bool hasAvatar;

  factory UserModel.fromJson(Map<String, dynamic> json) => UserModel(
        id: json['id'] as String,
        email: json['email'] as String,
        fullName: json['fullName'] as String,
        systemRole: json['systemRole'] as String,
        hasAvatar: json['hasAvatar'] as bool,
      );

  User toDomain() => User(
        id: id,
        email: email,
        fullName: fullName,
        systemRole: systemRoleFromString(systemRole),
        hasAvatar: hasAvatar,
      );
}
```

- [ ] **Step 5: Thêm `updateUser` vào `AuthController`**

Trong `mobile/taskmgmt_app/lib/features/auth/presentation/providers/auth_provider.dart`, thêm method mới trong class `AuthController` (sau `logout`):

```dart
  // Gọi sau khi upload/xoá avatar thành công (users_provider.dart) để phản ánh ngay hasAvatar
  // mới trên Home/Profile mà không cần gọi lại API xác thực.
  void updateUser(User user) {
    state = AsyncData(user);
  }
```

Thêm `import '../../domain/entities/user.dart';` ở đầu file nếu chưa có (đã có sẵn qua `AuthController extends AsyncNotifier<User?>`, kiểm tra import hiện tại đã đủ chưa trước khi thêm trùng).

- [ ] **Step 6: Chạy lại test, xác nhận PASS**

Run: `flutter test test/auth_provider_test.dart`
Expected: PASS (1/1).

- [ ] **Step 7: Build toàn bộ để bắt các chỗ khởi tạo `User`/`UserModel` cũ còn thiếu `hasAvatar`**

Run: `flutter analyze`
Expected: không lỗi biên dịch liên quan `hasAvatar` bị thiếu. Nếu có (ví dụ test khác tự dựng `User(...)`), sửa nốt thêm `hasAvatar: false` (hoặc giá trị phù hợp ngữ cảnh test đó) trước khi tiếp tục — tìm bằng `grep -rn "User(" mobile/taskmgmt_app/test mobile/taskmgmt_app/lib | grep -v hasAvatar`.

- [ ] **Step 8: Chạy toàn bộ test suite mobile**

Run: `flutter test`
Expected: tất cả PASS.

- [ ] **Step 9: Commit**

```bash
cd mobile/taskmgmt_app
git add lib/features/auth/domain/entities/user.dart lib/features/auth/data/models/user_model.dart lib/features/auth/presentation/providers/auth_provider.dart test/auth_provider_test.dart
git commit -m "feat(mobile): add hasAvatar to User + AuthController.updateUser"
```

---

## Task 10: Mobile — mở rộng `users` feature với avatar upload/download/delete

**Files:**
- Modify: `mobile/taskmgmt_app/lib/features/users/data/datasources/user_remote_data_source.dart`
- Modify: `mobile/taskmgmt_app/lib/features/users/domain/repositories/user_repository.dart`
- Modify: `mobile/taskmgmt_app/lib/features/users/data/repositories/user_repository_impl.dart`
- Modify: `mobile/taskmgmt_app/lib/features/users/presentation/providers/user_provider.dart`
- Test: `mobile/taskmgmt_app/test/user_provider_test.dart` (mới — test qua fake repository, không đụng Dio thật)

**Interfaces:**
- Consumes: `User`/`UserModel.hasAvatar` (Task 9), `mapDioException` (`mobile/taskmgmt_app/lib/core/network/api_exception.dart`, đã có).
- Produces: `UserRepository.uploadAvatar/deleteAvatar/downloadAvatar`, `avatarBytesProvider = FutureProvider.family<Uint8List?, String>` — dùng ở Task 11 (`UserAvatar` widget), Task 13 (`ProfileScreen`).

- [ ] **Step 1: Viết test trước cho `avatarBytesProvider`**

Tạo `mobile/taskmgmt_app/test/user_provider_test.dart`:

```dart
import 'dart:typed_data';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:taskmgmt_app/features/auth/domain/entities/user.dart';
import 'package:taskmgmt_app/features/users/domain/repositories/user_repository.dart';
import 'package:taskmgmt_app/features/users/presentation/providers/user_provider.dart';

class _FakeUserRepository implements UserRepository {
  @override
  Future<List<User>> getUsers() async => [];

  @override
  Future<Uint8List?> downloadAvatar(String userId) async =>
      userId == 'has-avatar' ? Uint8List.fromList([1, 2, 3]) : null;

  @override
  Future<User> uploadAvatar({required Uint8List bytes, required String fileName}) async => throw UnimplementedError();

  @override
  Future<User> deleteAvatar() async => throw UnimplementedError();
}

void main() {
  test('avatarBytesProvider returns bytes for a user with an avatar', () async {
    final container = ProviderContainer(
      overrides: [userRepositoryProvider.overrideWithValue(_FakeUserRepository())],
    );
    addTearDown(container.dispose);

    final bytes = await container.read(avatarBytesProvider('has-avatar').future);

    expect(bytes, Uint8List.fromList([1, 2, 3]));
  });

  test('avatarBytesProvider returns null for a user without an avatar', () async {
    final container = ProviderContainer(
      overrides: [userRepositoryProvider.overrideWithValue(_FakeUserRepository())],
    );
    addTearDown(container.dispose);

    final bytes = await container.read(avatarBytesProvider('no-avatar').future);

    expect(bytes, isNull);
  });
}
```

- [ ] **Step 2: Chạy test, xác nhận FAIL**

Run: `flutter test test/user_provider_test.dart`
Expected: FAIL — biên dịch lỗi (`UserRepository` chưa có `downloadAvatar`/`uploadAvatar`/`deleteAvatar`, `avatarBytesProvider` chưa tồn tại).

- [ ] **Step 3: Sửa `UserRepository` (interface)**

`mobile/taskmgmt_app/lib/features/users/domain/repositories/user_repository.dart`:

```dart
import 'dart:typed_data';

import '../../../auth/domain/entities/user.dart';

abstract class UserRepository {
  Future<List<User>> getUsers();

  Future<Uint8List?> downloadAvatar(String userId);

  Future<User> uploadAvatar({required Uint8List bytes, required String fileName});

  Future<User> deleteAvatar();
}
```

- [ ] **Step 4: Sửa `UserRemoteDataSource`**

`mobile/taskmgmt_app/lib/features/users/data/datasources/user_remote_data_source.dart`:

```dart
import 'dart:typed_data';

import 'package:dio/dio.dart';

import '../../../../core/network/api_exception.dart';
import '../../../auth/data/models/user_model.dart';

class UserRemoteDataSource {
  UserRemoteDataSource(this._dio);

  final Dio _dio;

  Future<List<UserModel>> getUsers() async {
    try {
      final response = await _dio.get<List<dynamic>>('/users');
      return response.data!.map((json) => UserModel.fromJson(json as Map<String, dynamic>)).toList();
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<Uint8List?> downloadAvatar(String userId) async {
    try {
      final response = await _dio.get<List<int>>(
        '/users/$userId/avatar',
        options: Options(responseType: ResponseType.bytes),
      );
      return Uint8List.fromList(response.data!);
    } on DioException catch (e) {
      if (e.response?.statusCode == 404) return null;
      throw mapDioException(e);
    }
  }

  Future<UserModel> uploadAvatar({required Uint8List bytes, required String fileName}) async {
    try {
      final formData = FormData.fromMap({
        'file': MultipartFile.fromBytes(bytes, filename: fileName),
      });
      final response = await _dio.post<Map<String, dynamic>>('/users/me/avatar', data: formData);
      return UserModel.fromJson(response.data!);
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<UserModel> deleteAvatar() async {
    try {
      final response = await _dio.delete<Map<String, dynamic>>('/users/me/avatar');
      return UserModel.fromJson(response.data!);
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }
}
```

- [ ] **Step 5: Sửa `UserRepositoryImpl`**

`mobile/taskmgmt_app/lib/features/users/data/repositories/user_repository_impl.dart`:

```dart
import 'dart:typed_data';

import '../../../auth/domain/entities/user.dart';
import '../../domain/repositories/user_repository.dart';
import '../datasources/user_remote_data_source.dart';

class UserRepositoryImpl implements UserRepository {
  UserRepositoryImpl(this._remoteDataSource);

  final UserRemoteDataSource _remoteDataSource;

  @override
  Future<List<User>> getUsers() async {
    final models = await _remoteDataSource.getUsers();
    return models.map((model) => model.toDomain()).toList();
  }

  @override
  Future<Uint8List?> downloadAvatar(String userId) => _remoteDataSource.downloadAvatar(userId);

  @override
  Future<User> uploadAvatar({required Uint8List bytes, required String fileName}) async {
    final model = await _remoteDataSource.uploadAvatar(bytes: bytes, fileName: fileName);
    return model.toDomain();
  }

  @override
  Future<User> deleteAvatar() async {
    final model = await _remoteDataSource.deleteAvatar();
    return model.toDomain();
  }
}
```

- [ ] **Step 6: Thêm `avatarBytesProvider` vào `user_provider.dart`**

`mobile/taskmgmt_app/lib/features/users/presentation/providers/user_provider.dart`:

```dart
import 'dart:typed_data';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/di/injection.dart';
import '../../domain/repositories/user_repository.dart';

final userRepositoryProvider = Provider<UserRepository>((ref) => getIt<UserRepository>());

final usersProvider = FutureProvider((ref) => ref.read(userRepositoryProvider).getUsers());

// Cache theo userId trong suốt phiên app - nhiều widget cùng hiện avatar của 1 người (assignee
// list, comment list) chỉ tải ảnh 1 lần nhờ Riverpod tự chia sẻ kết quả family theo tham số.
final avatarBytesProvider = FutureProvider.family<Uint8List?, String>((ref, userId) {
  return ref.read(userRepositoryProvider).downloadAvatar(userId);
});
```

- [ ] **Step 7: Chạy lại test, xác nhận PASS**

Run: `flutter test test/user_provider_test.dart`
Expected: PASS (2/2).

- [ ] **Step 8: `flutter analyze` + chạy toàn bộ test suite**

Run: `flutter analyze` rồi `flutter test`
Expected: không lỗi; tất cả test PASS.

- [ ] **Step 9: Commit**

```bash
cd mobile/taskmgmt_app
git add lib/features/users/ test/user_provider_test.dart
git commit -m "feat(mobile): add avatar upload/download/delete to users feature"
```

---

## Task 11: Mobile — widget `UserAvatar` dùng chung

**Files:**
- Create: `mobile/taskmgmt_app/lib/shared/widgets/user_avatar.dart`
- Test: `mobile/taskmgmt_app/test/user_avatar_test.dart`

**Interfaces:**
- Consumes: `avatarBytesProvider` (Task 10).
- Produces: `UserAvatar({required String userId, required bool hasAvatar, required String fallbackText, double radius})` — dùng ở Task 12 (assignee/comment list), Task 13 (`ProfileScreen`), Task 14 (Home).

- [ ] **Step 1: Viết test trước**

Tạo `mobile/taskmgmt_app/test/user_avatar_test.dart`:

```dart
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:taskmgmt_app/features/auth/domain/entities/user.dart';
import 'package:taskmgmt_app/features/users/domain/repositories/user_repository.dart';
import 'package:taskmgmt_app/features/users/presentation/providers/user_provider.dart';
import 'package:taskmgmt_app/shared/widgets/user_avatar.dart';

class _FakeUserRepository implements UserRepository {
  _FakeUserRepository({this.bytes, this.error});

  final Uint8List? bytes;
  final Object? error;

  @override
  Future<List<User>> getUsers() async => [];

  @override
  Future<Uint8List?> downloadAvatar(String userId) async {
    if (error != null) throw error!;
    return bytes;
  }

  @override
  Future<User> uploadAvatar({required Uint8List bytes, required String fileName}) async =>
      throw UnimplementedError();

  @override
  Future<User> deleteAvatar() async => throw UnimplementedError();
}

Widget _buildWidget(UserRepository repository, {required bool hasAvatar}) => ProviderScope(
      overrides: [userRepositoryProvider.overrideWithValue(repository)],
      child: MaterialApp(
        home: Scaffold(
          body: UserAvatar(userId: 'u1', hasAvatar: hasAvatar, fallbackText: 'A'),
        ),
      ),
    );

void main() {
  testWidgets('hasAvatar=false shows fallback text immediately, no network call', (tester) async {
    var called = false;
    final repository = _FakeUserRepository();
    // Bọc để phát hiện có gọi downloadAvatar không - dùng repository riêng theo dõi lời gọi.
    await tester.pumpWidget(_buildWidget(_TrackingRepository(inner: repository, onCalled: () => called = true), hasAvatar: false));
    await tester.pumpAndSettle();

    expect(find.text('A'), findsOneWidget);
    expect(called, isFalse);
  });

  testWidgets('hasAvatar=true and bytes load shows CircleAvatar with backgroundImage', (tester) async {
    final repository = _FakeUserRepository(bytes: Uint8List.fromList([1, 2, 3]));
    await tester.pumpWidget(_buildWidget(repository, hasAvatar: true));
    await tester.pumpAndSettle();

    final avatar = tester.widget<CircleAvatar>(find.byType(CircleAvatar));
    expect(avatar.backgroundImage, isNotNull);
    expect(find.text('A'), findsNothing);
  });

  testWidgets('hasAvatar=true and download fails falls back to text', (tester) async {
    final repository = _FakeUserRepository(error: Exception('network error'));
    await tester.pumpWidget(_buildWidget(repository, hasAvatar: true));
    await tester.pumpAndSettle();

    expect(find.text('A'), findsOneWidget);
  });
}

class _TrackingRepository implements UserRepository {
  _TrackingRepository({required this.inner, required this.onCalled});

  final UserRepository inner;
  final VoidCallback onCalled;

  @override
  Future<List<User>> getUsers() => inner.getUsers();

  @override
  Future<Uint8List?> downloadAvatar(String userId) {
    onCalled();
    return inner.downloadAvatar(userId);
  }

  @override
  Future<User> uploadAvatar({required Uint8List bytes, required String fileName}) =>
      inner.uploadAvatar(bytes: bytes, fileName: fileName);

  @override
  Future<User> deleteAvatar() => inner.deleteAvatar();
}
```

- [ ] **Step 2: Chạy test, xác nhận FAIL**

Run: `flutter test test/user_avatar_test.dart`
Expected: FAIL — không tìm thấy file `lib/shared/widgets/user_avatar.dart`.

- [ ] **Step 3: Tạo widget**

`mobile/taskmgmt_app/lib/shared/widgets/user_avatar.dart`:

```dart
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../features/users/presentation/providers/user_provider.dart';

class UserAvatar extends ConsumerWidget {
  const UserAvatar({
    super.key,
    required this.userId,
    required this.hasAvatar,
    required this.fallbackText,
    this.radius = 20,
  });

  final String userId;
  final bool hasAvatar;
  final String fallbackText;
  final double radius;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    if (!hasAvatar) {
      return CircleAvatar(radius: radius, child: Text(fallbackText));
    }

    final bytesAsync = ref.watch(avatarBytesProvider(userId));
    return bytesAsync.when(
      data: (bytes) => bytes == null
          ? CircleAvatar(radius: radius, child: Text(fallbackText))
          : CircleAvatar(radius: radius, backgroundImage: MemoryImage(bytes)),
      loading: () => CircleAvatar(
        radius: radius,
        child: SizedBox.square(
          dimension: radius * 0.6,
          child: const CircularProgressIndicator(strokeWidth: 2),
        ),
      ),
      error: (_, _) => CircleAvatar(radius: radius, child: Text(fallbackText)),
    );
  }
}
```

- [ ] **Step 4: Chạy lại test, xác nhận PASS**

Run: `flutter test test/user_avatar_test.dart`
Expected: PASS (3/3).

- [ ] **Step 5: Commit**

```bash
cd mobile/taskmgmt_app
git add lib/shared/widgets/user_avatar.dart test/user_avatar_test.dart
git commit -m "feat(mobile): add shared UserAvatar widget"
```

---

## Task 12: Mobile — hiện avatar thật ở assignee list + comment list

**Files:**
- Modify: `mobile/taskmgmt_app/lib/features/task_assignees/domain/entities/task_assignee.dart`
- Modify: `mobile/taskmgmt_app/lib/features/task_assignees/data/models/task_assignee_model.dart`
- Modify: `mobile/taskmgmt_app/lib/features/task_assignees/presentation/widgets/assignee_list_section.dart`
- Modify: `mobile/taskmgmt_app/lib/features/comments/domain/entities/comment.dart`
- Modify: `mobile/taskmgmt_app/lib/features/comments/data/models/comment_model.dart`
- Modify: `mobile/taskmgmt_app/lib/features/comments/presentation/widgets/comment_list_section.dart`

**Interfaces:**
- Consumes: `UserAvatar` (Task 11), `TaskAssigneeDto.UserHasAvatar` (backend Task 7), `CommentDto.AuthorHasAvatar` (backend Task 8).

- [ ] **Step 1: Thêm `userHasAvatar` vào `TaskAssignee` entity**

`mobile/taskmgmt_app/lib/features/task_assignees/domain/entities/task_assignee.dart`, sửa class `TaskAssignee`:

```dart
class TaskAssignee {
  const TaskAssignee({
    required this.id,
    required this.workTaskId,
    required this.userId,
    required this.userFullName,
    required this.userEmail,
    required this.userHasAvatar,
    required this.role,
  });

  final String id;
  final String workTaskId;
  final String userId;
  final String userFullName;
  final String userEmail;
  final bool userHasAvatar;
  final TaskAssigneeRole role;
}
```

- [ ] **Step 2: Thêm `userHasAvatar` vào `TaskAssigneeModel`**

`mobile/taskmgmt_app/lib/features/task_assignees/data/models/task_assignee_model.dart`:

```dart
import '../../domain/entities/task_assignee.dart';

class TaskAssigneeModel {
  const TaskAssigneeModel({
    required this.id,
    required this.workTaskId,
    required this.userId,
    required this.userFullName,
    required this.userEmail,
    required this.userHasAvatar,
    required this.role,
  });

  final String id;
  final String workTaskId;
  final String userId;
  final String userFullName;
  final String userEmail;
  final bool userHasAvatar;
  final String role;

  factory TaskAssigneeModel.fromJson(Map<String, dynamic> json) => TaskAssigneeModel(
        id: json['id'] as String,
        workTaskId: json['workTaskId'] as String,
        userId: json['userId'] as String,
        userFullName: json['userFullName'] as String,
        userEmail: json['userEmail'] as String,
        userHasAvatar: json['userHasAvatar'] as bool,
        role: json['role'] as String,
      );

  TaskAssignee toDomain() => TaskAssignee(
        id: id,
        workTaskId: workTaskId,
        userId: userId,
        userFullName: userFullName,
        userEmail: userEmail,
        userHasAvatar: userHasAvatar,
        role: taskAssigneeRoleFromString(role),
      );
}
```

- [ ] **Step 3: Đổi `CircleAvatar` trong `assignee_list_section.dart`**

Thêm import ở đầu file: `import '../../../../shared/widgets/user_avatar.dart';`

Sửa đoạn (quanh dòng 97-99):

```dart
                          leading: CircleAvatar(
                            child: Text(assignee.userFullName.isNotEmpty ? assignee.userFullName[0].toUpperCase() : '?'),
                          ),
```

thành:

```dart
                          leading: UserAvatar(
                            userId: assignee.userId,
                            hasAvatar: assignee.userHasAvatar,
                            fallbackText: assignee.userFullName.isNotEmpty ? assignee.userFullName[0].toUpperCase() : '?',
                          ),
```

- [ ] **Step 4: Chạy test hiện có của assignee (nếu có) + `flutter analyze`**

Run: `grep -rl "TaskAssigneeModel(\|TaskAssignee(" mobile/taskmgmt_app/test` để tìm test đang dựng `TaskAssignee`/`TaskAssigneeModel` thủ công — thêm `userHasAvatar: false` (hoặc giá trị phù hợp) vào từng chỗ tìm thấy.

Run: `flutter analyze`
Expected: không lỗi.

- [ ] **Step 5: Thêm `authorHasAvatar` vào `Comment` entity**

`mobile/taskmgmt_app/lib/features/comments/domain/entities/comment.dart`:

```dart
class CommentMention {
  const CommentMention({required this.userId, required this.fullName, required this.email});

  final String userId;
  final String fullName;
  final String email;
}

class Comment {
  const Comment({
    required this.id,
    required this.workTaskId,
    required this.content,
    required this.authorUserId,
    required this.authorFullName,
    required this.authorEmail,
    required this.authorHasAvatar,
    required this.createdAtUtc,
    required this.mentions,
  });

  final String id;
  final String workTaskId;
  final String content;
  final String? authorUserId;
  final String authorFullName;
  final String authorEmail;
  final bool authorHasAvatar;
  final DateTime createdAtUtc;
  final List<CommentMention> mentions;
}
```

- [ ] **Step 6: Thêm `authorHasAvatar` vào `CommentModel`**

`mobile/taskmgmt_app/lib/features/comments/data/models/comment_model.dart`, sửa class `CommentModel`:

```dart
class CommentModel {
  const CommentModel({
    required this.id,
    required this.workTaskId,
    required this.content,
    required this.authorUserId,
    required this.authorFullName,
    required this.authorEmail,
    required this.authorHasAvatar,
    required this.createdAtUtc,
    required this.mentions,
  });

  final String id;
  final String workTaskId;
  final String content;
  final String? authorUserId;
  final String authorFullName;
  final String authorEmail;
  final bool authorHasAvatar;
  final DateTime createdAtUtc;
  final List<CommentMentionModel> mentions;

  factory CommentModel.fromJson(Map<String, dynamic> json) => CommentModel(
        id: json['id'] as String,
        workTaskId: json['workTaskId'] as String,
        content: json['content'] as String,
        authorUserId: json['authorUserId'] as String?,
        authorFullName: json['authorFullName'] as String,
        authorEmail: json['authorEmail'] as String,
        authorHasAvatar: json['authorHasAvatar'] as bool,
        createdAtUtc: DateTime.parse(json['createdAtUtc'] as String),
        mentions: (json['mentions'] as List<dynamic>)
            .map((m) => CommentMentionModel.fromJson(m as Map<String, dynamic>))
            .toList(),
      );

  Comment toDomain() => Comment(
        id: id,
        workTaskId: workTaskId,
        content: content,
        authorUserId: authorUserId,
        authorFullName: authorFullName,
        authorEmail: authorEmail,
        authorHasAvatar: authorHasAvatar,
        createdAtUtc: createdAtUtc,
        mentions: mentions.map((m) => m.toDomain()).toList(),
      );
}
```

- [ ] **Step 7: Đổi `CircleAvatar` trong `comment_list_section.dart`**

Thêm import ở đầu file: `import '../../../../shared/widgets/user_avatar.dart';`

Sửa đoạn (quanh dòng 164-166) — `authorUserId` có thể null (comment của user đã xoá), giữ fallback chữ cái khi đó:

```dart
          CircleAvatar(
            child: Text(comment.authorFullName.isNotEmpty ? comment.authorFullName[0].toUpperCase() : '?'),
          ),
```

thành:

```dart
          comment.authorUserId == null
              ? CircleAvatar(
                  child: Text(comment.authorFullName.isNotEmpty ? comment.authorFullName[0].toUpperCase() : '?'),
                )
              : UserAvatar(
                  userId: comment.authorUserId!,
                  hasAvatar: comment.authorHasAvatar,
                  fallbackText: comment.authorFullName.isNotEmpty ? comment.authorFullName[0].toUpperCase() : '?',
                ),
```

- [ ] **Step 8: Sửa các chỗ dựng `Comment`/`CommentModel` thủ công trong test hiện có**

Run: `grep -rl "CommentModel(\|Comment(" mobile/taskmgmt_app/test` — thêm `authorHasAvatar: false` (hoặc giá trị phù hợp) vào từng chỗ tìm thấy đang dựng `Comment`/`CommentModel` trực tiếp (không phải qua `fromJson`).

- [ ] **Step 9: `flutter analyze` + chạy toàn bộ test suite**

Run: `flutter analyze` rồi `flutter test`
Expected: không lỗi; tất cả test PASS. Nếu có test widget cũ của assignee/comment list bị lỗi vì thiếu override `userRepositoryProvider` (vì `UserAvatar` giờ watch `avatarBytesProvider`), thêm `userRepositoryProvider.overrideWithValue(...)` (fake trả `null`/`[]` cho `downloadAvatar`) vào `ProviderScope` của test đó — theo đúng pattern override đã dùng ở các test khác trong file.

- [ ] **Step 10: Commit**

```bash
cd mobile/taskmgmt_app
git add lib/features/task_assignees/ lib/features/comments/ test/
git commit -m "feat(mobile): show real avatars in assignee and comment lists"
```

---

## Task 13: Mobile — màn "Hồ sơ của tôi"

**Files:**
- Create: `mobile/taskmgmt_app/lib/features/users/presentation/screens/profile_screen.dart`
- Modify: `mobile/taskmgmt_app/lib/core/routing/app_router.dart`
- Test: `mobile/taskmgmt_app/test/profile_screen_test.dart`

**Interfaces:**
- Consumes: `UserAvatar` (Task 11), `UserRepository.uploadAvatar/deleteAvatar` (Task 10), `AuthController.updateUser` (Task 9), `authControllerProvider` (đã có).
- Produces: `ProfileScreen.path = '/profile'`, `ProfileScreen.name = 'profile'` — dùng ở Task 14 (Home).

- [ ] **Step 1: Viết test trước**

Tạo `mobile/taskmgmt_app/test/profile_screen_test.dart`:

```dart
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:taskmgmt_app/features/auth/domain/entities/user.dart';
import 'package:taskmgmt_app/features/auth/domain/repositories/auth_repository.dart';
import 'package:taskmgmt_app/features/auth/presentation/providers/auth_provider.dart';
import 'package:taskmgmt_app/features/users/domain/repositories/user_repository.dart';
import 'package:taskmgmt_app/features/users/presentation/providers/user_provider.dart';
import 'package:taskmgmt_app/features/users/presentation/screens/profile_screen.dart';

const _userWithAvatar = User(
  id: '1',
  email: 'a@b.com',
  fullName: 'Nguyễn Test',
  systemRole: SystemRole.member,
  hasAvatar: true,
);

const _userWithoutAvatar = User(
  id: '1',
  email: 'a@b.com',
  fullName: 'Nguyễn Test',
  systemRole: SystemRole.member,
  hasAvatar: false,
);

class _FakeAuthRepository implements AuthRepository {
  _FakeAuthRepository(this.initialUser);

  final User initialUser;

  @override
  Future<User?> restoreSession() async => initialUser;

  @override
  Future<User> login({required String email, required String password}) async => throw UnimplementedError();

  @override
  Future<User> register({required String email, required String fullName, required String password}) async =>
      throw UnimplementedError();

  @override
  Future<void> logout() async {}
}

class _FakeUserRepository implements UserRepository {
  _FakeUserRepository();

  bool deleteAvatarCalled = false;

  @override
  Future<List<User>> getUsers() async => [];

  @override
  Future<Uint8List?> downloadAvatar(String userId) async => null;

  @override
  Future<User> uploadAvatar({required Uint8List bytes, required String fileName}) async => throw UnimplementedError();

  @override
  Future<User> deleteAvatar() async {
    deleteAvatarCalled = true;
    return _userWithoutAvatar;
  }
}

Widget _buildScreen(User user, UserRepository userRepository) => ProviderScope(
      overrides: [
        authRepositoryProvider.overrideWithValue(_FakeAuthRepository(user)),
        userRepositoryProvider.overrideWithValue(userRepository),
      ],
      child: const MaterialApp(home: ProfileScreen()),
    );

void main() {
  testWidgets('Shows full name, email, and delete button when user has an avatar', (tester) async {
    await tester.pumpWidget(_buildScreen(_userWithAvatar, _FakeUserRepository()));
    await tester.pumpAndSettle();

    expect(find.text('Nguyễn Test'), findsOneWidget);
    expect(find.text('a@b.com'), findsOneWidget);
    expect(find.text('Xoá avatar'), findsOneWidget);
  });

  testWidgets('Hides delete button when user has no avatar', (tester) async {
    await tester.pumpWidget(_buildScreen(_userWithoutAvatar, _FakeUserRepository()));
    await tester.pumpAndSettle();

    expect(find.text('Xoá avatar'), findsNothing);
  });

  testWidgets('Tapping delete avatar calls the repository', (tester) async {
    final userRepository = _FakeUserRepository();
    await tester.pumpWidget(_buildScreen(_userWithAvatar, userRepository));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Xoá avatar'));
    await tester.pumpAndSettle();

    expect(userRepository.deleteAvatarCalled, isTrue);
    expect(find.text('Xoá avatar'), findsNothing);
  });
}
```

- [ ] **Step 2: Chạy test, xác nhận FAIL**

Run: `flutter test test/profile_screen_test.dart`
Expected: FAIL — không tìm thấy file `lib/features/users/presentation/screens/profile_screen.dart`.

- [ ] **Step 3: Tạo `ProfileScreen`**

`mobile/taskmgmt_app/lib/features/users/presentation/screens/profile_screen.dart`:

```dart
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../shared/widgets/user_avatar.dart';
import '../../../auth/presentation/providers/auth_provider.dart';
import '../providers/user_provider.dart';

class ProfileScreen extends ConsumerStatefulWidget {
  const ProfileScreen({super.key});

  static const path = '/profile';
  static const name = 'profile';

  @override
  ConsumerState<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends ConsumerState<ProfileScreen> {
  bool _isBusy = false;

  void _showError(String message) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
  }

  Future<void> _pickAndUpload(ImageSource source) async {
    try {
      final xfile = await ImagePicker().pickImage(source: source);
      if (xfile == null) return;

      setState(() => _isBusy = true);
      final bytes = await xfile.readAsBytes();
      final updatedUser =
          await ref.read(userRepositoryProvider).uploadAvatar(bytes: bytes, fileName: xfile.name);
      ref.read(authControllerProvider.notifier).updateUser(updatedUser);
    } catch (e) {
      _showError(e is ApiException ? e.message : 'Không thể tải ảnh lên.');
    } finally {
      if (mounted) setState(() => _isBusy = false);
    }
  }

  Future<void> _showSourceSheet() {
    return showModalBottomSheet<void>(
      context: context,
      builder: (sheetContext) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              leading: const Icon(Icons.photo_camera_outlined),
              title: const Text('Chụp ảnh'),
              onTap: () {
                Navigator.of(sheetContext).pop();
                _pickAndUpload(ImageSource.camera);
              },
            ),
            ListTile(
              leading: const Icon(Icons.photo_library_outlined),
              title: const Text('Chọn từ thư viện'),
              onTap: () {
                Navigator.of(sheetContext).pop();
                _pickAndUpload(ImageSource.gallery);
              },
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _deleteAvatar() async {
    try {
      setState(() => _isBusy = true);
      final updatedUser = await ref.read(userRepositoryProvider).deleteAvatar();
      ref.read(authControllerProvider.notifier).updateUser(updatedUser);
    } catch (e) {
      _showError(e is ApiException ? e.message : 'Không thể xoá avatar.');
    } finally {
      if (mounted) setState(() => _isBusy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final user = ref.watch(authControllerProvider).valueOrNull;

    return Scaffold(
      appBar: AppBar(title: const Text('Hồ sơ của tôi')),
      body: user == null
          ? const Center(child: CircularProgressIndicator())
          : ListView(
              padding: const EdgeInsets.all(16),
              children: [
                Center(
                  child: Stack(
                    children: [
                      UserAvatar(
                        userId: user.id,
                        hasAvatar: user.hasAvatar,
                        fallbackText: user.fullName.isNotEmpty ? user.fullName[0].toUpperCase() : '?',
                        radius: 56,
                      ),
                      if (_isBusy)
                        const Positioned.fill(
                          child: CircleAvatar(
                            radius: 56,
                            backgroundColor: Colors.black38,
                            child: CircularProgressIndicator(),
                          ),
                        ),
                      Positioned(
                        bottom: 0,
                        right: 0,
                        child: IconButton.filled(
                          icon: const Icon(Icons.camera_alt),
                          tooltip: 'Đổi avatar',
                          onPressed: _isBusy ? null : _showSourceSheet,
                        ),
                      ),
                    ],
                  ),
                ),
                if (user.hasAvatar) ...[
                  const SizedBox(height: 8),
                  Center(
                    child: TextButton(
                      onPressed: _isBusy ? null : _deleteAvatar,
                      child: const Text('Xoá avatar'),
                    ),
                  ),
                ],
                const SizedBox(height: 24),
                ListTile(
                  leading: const Icon(Icons.person_outline),
                  title: const Text('Họ tên'),
                  subtitle: Text(user.fullName),
                ),
                ListTile(
                  leading: const Icon(Icons.email_outlined),
                  title: const Text('Email'),
                  subtitle: Text(user.email),
                ),
              ],
            ),
    );
  }
}
```

- [ ] **Step 4: Chạy lại test, xác nhận PASS**

Run: `flutter test test/profile_screen_test.dart`
Expected: PASS (3/3).

- [ ] **Step 5: Thêm route vào `app_router.dart`**

Đọc `mobile/taskmgmt_app/lib/core/routing/app_router.dart` hiện tại (đã có `HomeScreen` từ tính năng trước), thêm import:

```dart
import '../../features/users/presentation/screens/profile_screen.dart';
```

Thêm route mới trong `routes: [...]`, ngay sau route của `HomeScreen`:

```dart
      GoRoute(
        path: ProfileScreen.path,
        name: ProfileScreen.name,
        builder: (context, state) => const ProfileScreen(),
      ),
```

- [ ] **Step 6: `flutter analyze` + chạy toàn bộ test suite**

Run: `flutter analyze` rồi `flutter test`
Expected: không lỗi; tất cả test PASS.

- [ ] **Step 7: Commit**

```bash
cd mobile/taskmgmt_app
git add lib/features/users/presentation/screens/profile_screen.dart lib/core/routing/app_router.dart test/profile_screen_test.dart
git commit -m "feat(mobile): add Profile screen with avatar upload/delete"
```

---

## Task 14: Mobile — avatar trên Home, mở màn Hồ sơ

**Files:**
- Modify: `mobile/taskmgmt_app/lib/features/home/presentation/screens/home_screen.dart`
- Modify: `mobile/taskmgmt_app/test/home_screen_test.dart`

**Interfaces:**
- Consumes: `UserAvatar` (Task 11), `ProfileScreen.path` (Task 13), `User.hasAvatar` (Task 9).

- [ ] **Step 1: Sửa test hiện có của Home để phản ánh avatar mới**

Đọc `mobile/taskmgmt_app/test/home_screen_test.dart` hiện tại. Trong `_FakeAuthRepository.restoreSession`, thêm `hasAvatar: false` vào `User(...)` đã dựng sẵn. Thêm 1 test mới vào cuối `void main() { ... }`:

```dart
  testWidgets('Tapping the avatar navigates to the profile screen', (tester) async {
    tester.view.physicalSize = const Size(800, 1400);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(_buildApp());
    await tester.pumpAndSettle();

    await tester.tap(find.byType(GestureDetector).first);
    await tester.pumpAndSettle();

    expect(find.text('Màn Hồ sơ'), findsOneWidget);
  });
```

Thêm route giả `/profile` vào `router` trong `_buildApp()` (cạnh các route giả `/tasks`, `/dashboard`,...):

```dart
      GoRoute(path: '/profile', builder: (context, state) => const Scaffold(body: Text('Màn Hồ sơ'))),
```

- [ ] **Step 2: Chạy test, xác nhận FAIL**

Run: `flutter test test/home_screen_test.dart`
Expected: FAIL — `hasAvatar` chưa tồn tại trên `User` dựng trong test (lỗi biên dịch), và/hoặc test mới không tìm thấy `GestureDetector` bấm được.

- [ ] **Step 3: Sửa `HomeScreen`**

Đọc `mobile/taskmgmt_app/lib/features/home/presentation/screens/home_screen.dart` hiện tại. Thêm import:

```dart
import 'package:go_router/go_router.dart';

import '../../../../shared/widgets/user_avatar.dart';
import '../../../users/presentation/screens/profile_screen.dart';
```

(giữ nguyên các import khác đã có). Sửa đoạn hiện đang là:

```dart
            if (user != null)
              Padding(
                padding: const EdgeInsets.only(bottom: 16),
                child: Text('Xin chào, ${user.fullName}', style: Theme.of(context).textTheme.bodyLarge),
              ),
```

thành:

```dart
            if (user != null)
              Padding(
                padding: const EdgeInsets.only(bottom: 16),
                child: GestureDetector(
                  onTap: () => context.push(ProfileScreen.path),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      UserAvatar(
                        userId: user.id,
                        hasAvatar: user.hasAvatar,
                        fallbackText: user.fullName.isNotEmpty ? user.fullName[0].toUpperCase() : '?',
                        radius: 18,
                      ),
                      const SizedBox(width: 12),
                      Text('Xin chào, ${user.fullName}', style: Theme.of(context).textTheme.bodyLarge),
                    ],
                  ),
                ),
              ),
```

- [ ] **Step 4: Chạy lại test, xác nhận PASS**

Run: `flutter test test/home_screen_test.dart`
Expected: PASS (toàn bộ test trong file, kể cả test mới).

- [ ] **Step 5: Thêm route `ProfileScreen` thật vào `app_router.dart` nếu Task 13 chưa merge vào cùng nhánh**

Nếu Task 13 đã hoàn thành trước đó trong cùng phiên làm việc, route thật đã tồn tại — bỏ qua bước này. Nếu không, xác nhận `app_router.dart` đã có route `ProfileScreen.path` (từ Task 13) trước khi coi Task 14 là xong.

- [ ] **Step 6: `flutter analyze` + chạy toàn bộ test suite mobile**

Run: `flutter analyze` rồi `flutter test`
Expected: không lỗi; tất cả test PASS.

- [ ] **Step 7: Commit**

```bash
cd mobile/taskmgmt_app
git add lib/features/home/presentation/screens/home_screen.dart test/home_screen_test.dart
git commit -m "feat(mobile): show avatar on Home, tap to open Profile screen"
```

---

## Xác minh thủ công cuối cùng (sau khi cả 14 task hoàn thành)

Không tự động hoá được (giới hạn platform-channel của `image_picker`, giống các tính năng trước) — chạy trên máy ảo/thiết bị thật:

1. `docker compose up -d` (repo root), `cd backend/src/TaskMgmt.API && dotnet run`.
2. Build + cài app lên máy ảo/thiết bị (nhớ đúng `_host` trong `app_config.dart` — `10.0.2.2` cho máy ảo).
3. Đăng nhập → xác nhận Home hiện avatar chữ cái đầu (chưa có ảnh) cạnh "Xin chào".
4. Bấm avatar → vào Hồ sơ → bấm nút camera → chọn ảnh từ thư viện → xác nhận avatar cập nhật ngay trên màn Hồ sơ và khi quay lại Home.
5. Vào 1 task đã gán chính mình làm assignee → xác nhận avatar thật hiện trong danh sách assignee (không phải chữ cái đầu).
6. Viết 1 comment trên task đó → xác nhận avatar thật hiện cạnh comment.
7. Quay lại Hồ sơ → bấm "Xoá avatar" → xác nhận quay về chữ cái đầu ở cả Home, assignee list, comment list (sau khi refresh/điều hướng lại — nhớ rủi ro đã ghi trong spec: nơi khác đang cache bytes cũ trong phiên có thể chưa cập nhật ngay).
