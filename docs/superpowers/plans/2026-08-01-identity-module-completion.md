# Hoàn thiện Module Identity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Lấp các khoảng trống của module Identity so với kế hoạch G1 và Definition of Done: Quên/Đặt lại mật khẩu, Logout (thu hồi đúng refresh token hiện tại), và unit test cho toàn bộ Auth handler.

**Architecture:** Mỗi use case mới là 1 Command + Handler + FluentValidation Validator dưới `Features/Auth/Commands/<UseCase>/`, đúng CQRS pattern đã có của module (MediatR, validate qua pipeline, không validate tay trong handler). Token đặt lại mật khẩu dùng entity `PasswordResetToken` riêng (mirror `RefreshToken`) thay vì tái sử dụng bảng `RefreshTokens`, để tránh một reset-token vô tình bị `RefreshTokenCommandHandler` chấp nhận như refresh token hợp lệ.

**Tech Stack:** .NET 10, MediatR, FluentValidation, EF Core (PostgreSQL), xUnit + EF Core InMemory (`TestDbContextFactory`), Flutter/Riverpod/Dio/go_router.

## Global Constraints

- Namespace mirror thư mục; 4-space indent, CRLF, file-scoped namespace (theo `backend/.editorconfig`).
- Validate chỉ qua FluentValidation, không validate tay trong handler.
- Mọi command/query mới tự động được MediatR + FluentValidation nhận diện qua assembly scan trong `TaskMgmt.Application/DependencyInjection.cs` — không cần đăng ký DI thủ công.
- `ForgotPassword`/`ResetPassword` không được tiết lộ việc một email có tồn tại trong hệ thống hay không (chống user enumeration).
- `Logout` phải idempotent — không bao giờ trả lỗi (mirror convention của `UnregisterDeviceTokenCommandHandler`).
- `ResetPassword` thành công phải thu hồi toàn bộ refresh token đang active của user (đăng xuất mọi thiết bị).
- Chưa có dịch vụ gửi email/SMS thật — `ForgotPassword` chỉ log token qua `ILogger` (giải pháp tạm, không dùng được cho production).
- Ngoài phạm vi: rate-limit chống brute-force, gửi email/SMS thật, tách usecase riêng cho mobile auth.

Spec đầy đủ: `docs/superpowers/specs/2026-08-01-identity-module-completion-design.md`.

---

## Backend

### Task 1: PasswordResetToken data model + migration

**Files:**
- Create: `backend/src/TaskMgmt.Domain/Entities/PasswordResetToken.cs`
- Modify: `backend/src/TaskMgmt.Domain/Entities/User.cs`
- Modify: `backend/src/TaskMgmt.Application/Common/Interfaces/IApplicationDbContext.cs`
- Modify: `backend/src/TaskMgmt.Infrastructure/Persistence/AppDbContext.cs`
- Create: `backend/src/TaskMgmt.Infrastructure/Persistence/Configurations/PasswordResetTokenConfiguration.cs`
- Create (via EF CLI): migration under `backend/src/TaskMgmt.Infrastructure/Persistence/Migrations/`

**Interfaces:**
- Produces: `PasswordResetToken` entity with `UserId (Guid)`, `User? (User)`, `Token (string)`, `CreatedAtUtc (DateTimeOffset)`, `ExpiresAtUtc (DateTimeOffset)`, `UsedAtUtc (DateTimeOffset?)`, `IsActive (bool, computed)`. `IApplicationDbContext.PasswordResetTokens : DbSet<PasswordResetToken>`. Tasks 6, 7, 10 consume these.

- [ ] **Step 1: Tạo entity `PasswordResetToken`**

```csharp
using TaskMgmt.Domain.Common;

namespace TaskMgmt.Domain.Entities;

public class PasswordResetToken : BaseEntity
{
    public required Guid UserId { get; set; }
    public User? User { get; set; }

    public required string Token { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? UsedAtUtc { get; set; }

    public bool IsActive => UsedAtUtc is null && ExpiresAtUtc > DateTimeOffset.UtcNow;
}
```

- [ ] **Step 2: Thêm navigation collection vào `User`**

Trong `backend/src/TaskMgmt.Domain/Entities/User.cs`, thêm dòng sau ngay dưới `RefreshTokens`:

```csharp
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = [];
```

- [ ] **Step 3: Thêm `DbSet` vào `IApplicationDbContext`**

Trong `backend/src/TaskMgmt.Application/Common/Interfaces/IApplicationDbContext.cs`, thêm dòng sau ngay dưới `DbSet<RefreshToken> RefreshTokens { get; }`:

```csharp
    DbSet<PasswordResetToken> PasswordResetTokens { get; }
```

- [ ] **Step 4: Thêm `DbSet` vào `AppDbContext`**

Trong `backend/src/TaskMgmt.Infrastructure/Persistence/AppDbContext.cs`, thêm dòng sau ngay dưới `public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();`:

```csharp
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
```

- [ ] **Step 5: Tạo EF configuration**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Infrastructure.Persistence.Configurations;

public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetTokens");

        builder.Property(t => t.Token).HasMaxLength(200).IsRequired();
        builder.HasIndex(t => t.Token).IsUnique();

        builder.Ignore(t => t.IsActive);

        builder.HasOne(t => t.User)
            .WithMany(u => u.PasswordResetTokens)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Step 6: Build để xác nhận model hợp lệ**

Run: `cd backend && dotnet build TaskMgmt.slnx`
Expected: build thành công, không lỗi.

- [ ] **Step 7: Tạo migration**

Run (từ `backend/src/TaskMgmt.Infrastructure`):
```bash
cd backend/src/TaskMgmt.Infrastructure
dotnet ef migrations add AddPasswordResetTokens --startup-project ../TaskMgmt.API
```
Expected: tạo file migration mới `<timestamp>_AddPasswordResetTokens.cs` tạo bảng `PasswordResetTokens` với FK tới `Users` và unique index trên `Token`. Kiểm tra nội dung file migration sinh ra khớp với configuration ở Step 5.

- [ ] **Step 8: Commit**

```bash
cd d:/projects/K-app
git add backend/src/TaskMgmt.Domain/Entities/PasswordResetToken.cs backend/src/TaskMgmt.Domain/Entities/User.cs backend/src/TaskMgmt.Application/Common/Interfaces/IApplicationDbContext.cs backend/src/TaskMgmt.Infrastructure/Persistence/AppDbContext.cs backend/src/TaskMgmt.Infrastructure/Persistence/Configurations/PasswordResetTokenConfiguration.cs backend/src/TaskMgmt.Infrastructure/Persistence/Migrations/
git commit -m "feat: add PasswordResetToken data model and migration"
```

---

### Task 2: Test helper `TestDataFactory.CreateJwtSettings()`

**Files:**
- Modify: `backend/tests/TaskMgmt.Application.UnitTests/Common/TestDataFactory.cs`

**Interfaces:**
- Produces: `TestDataFactory.CreateJwtSettings() : JwtSettings` — dùng bởi Task 3, 4, 5 để khởi tạo `JwtTokenGenerator` thật trong test.

- [ ] **Step 1: Thêm using và method**

Trong `backend/tests/TaskMgmt.Application.UnitTests/Common/TestDataFactory.cs`, thêm `using TaskMgmt.Application.Common.Models;` vào đầu file, và thêm method sau vào trong class `TestDataFactory`:

```csharp
    public static JwtSettings CreateJwtSettings() => new()
    {
        Secret = "unit-test-super-secret-signing-key-32-bytes-min",
        Issuer = "TaskMgmt.Tests",
        Audience = "TaskMgmt.Tests",
    };
```

- [ ] **Step 2: Build test project để xác nhận không lỗi**

Run: `cd backend && dotnet build tests/TaskMgmt.Application.UnitTests`
Expected: build thành công.

- [ ] **Step 3: Commit**

```bash
git add backend/tests/TaskMgmt.Application.UnitTests/Common/TestDataFactory.cs
git commit -m "test: add JwtSettings test factory helper"
```

---

### Task 3: Backfill unit test cho `LoginCommandHandler`

**Files:**
- Create: `backend/tests/TaskMgmt.Application.UnitTests/Features/Auth/LoginCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `LoginCommandHandler(IApplicationDbContext, IPasswordHasher, IJwtTokenGenerator, JwtSettings)`, `LoginCommand(string Email, string Password) : IRequest<AuthResultDto>`, `TestDataFactory.CreateUser(string email = ...)`, `TestDataFactory.CreateJwtSettings()`, `TestDbContextFactory.Create()`, `TaskMgmt.Infrastructure.Identity.PasswordHasher`, `TaskMgmt.Infrastructure.Identity.JwtTokenGenerator`.

- [ ] **Step 1: Viết test**

```csharp
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Features.Auth.Commands.Login;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Infrastructure.Identity;

namespace TaskMgmt.Application.UnitTests.Features.Auth;

public class LoginCommandHandlerTests
{
    private static readonly PasswordHasher PasswordHasher = new();

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsTokensAndCreatesRefreshToken()
    {
        using var context = TestDbContextFactory.Create();
        var jwtSettings = TestDataFactory.CreateJwtSettings();
        var user = TestDataFactory.CreateUser();
        user.PasswordHash = PasswordHasher.Hash("Password123!");
        context.Users.Add(user);
        await context.SaveChangesAsync(default);

        var handler = new LoginCommandHandler(context, PasswordHasher, new JwtTokenGenerator(jwtSettings), jwtSettings);

        var result = await handler.Handle(new LoginCommand(user.Email, "Password123!"), default);

        Assert.False(string.IsNullOrEmpty(result.AccessToken));
        Assert.Equal(user.Id, result.User.Id);
        var storedToken = Assert.Single(context.RefreshTokens.Where(t => t.UserId == user.Id));
        Assert.Equal(result.RefreshToken, storedToken.Token);
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsUnauthorized()
    {
        using var context = TestDbContextFactory.Create();
        var jwtSettings = TestDataFactory.CreateJwtSettings();
        var user = TestDataFactory.CreateUser();
        user.PasswordHash = PasswordHasher.Hash("Password123!");
        context.Users.Add(user);
        await context.SaveChangesAsync(default);

        var handler = new LoginCommandHandler(context, PasswordHasher, new JwtTokenGenerator(jwtSettings), jwtSettings);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => handler.Handle(new LoginCommand(user.Email, "WrongPassword!"), default));
    }

    [Fact]
    public async Task Handle_UnknownEmail_ThrowsUnauthorized()
    {
        using var context = TestDbContextFactory.Create();
        var jwtSettings = TestDataFactory.CreateJwtSettings();
        var handler = new LoginCommandHandler(context, PasswordHasher, new JwtTokenGenerator(jwtSettings), jwtSettings);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => handler.Handle(new LoginCommand("nobody@example.com", "Password123!"), default));
    }

    [Fact]
    public async Task Handle_InactiveUser_ThrowsUnauthorized()
    {
        using var context = TestDbContextFactory.Create();
        var jwtSettings = TestDataFactory.CreateJwtSettings();
        var user = TestDataFactory.CreateUser();
        user.PasswordHash = PasswordHasher.Hash("Password123!");
        user.IsActive = false;
        context.Users.Add(user);
        await context.SaveChangesAsync(default);

        var handler = new LoginCommandHandler(context, PasswordHasher, new JwtTokenGenerator(jwtSettings), jwtSettings);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => handler.Handle(new LoginCommand(user.Email, "Password123!"), default));
    }
}
```

- [ ] **Step 2: Chạy test, xác nhận PASS**

Run: `cd backend && dotnet test tests/TaskMgmt.Application.UnitTests --filter "FullyQualifiedName~LoginCommandHandlerTests"`
Expected: PASS (4/4) — `LoginCommandHandler` đã tồn tại từ trước, đây là test bổ sung độ phủ, không phải TDD tạo mới.

- [ ] **Step 3: Commit**

```bash
git add backend/tests/TaskMgmt.Application.UnitTests/Features/Auth/LoginCommandHandlerTests.cs
git commit -m "test: backfill unit tests for LoginCommandHandler"
```

---

### Task 4: Backfill unit test cho `RegisterCommandHandler` + `RegisterCommandValidator`

**Files:**
- Create: `backend/tests/TaskMgmt.Application.UnitTests/Features/Auth/RegisterCommandHandlerTests.cs`
- Create: `backend/tests/TaskMgmt.Application.UnitTests/Features/Auth/RegisterCommandValidatorTests.cs`

**Interfaces:**
- Consumes: `RegisterCommandHandler(IApplicationDbContext, IPasswordHasher, IJwtTokenGenerator, JwtSettings)`, `RegisterCommand(string Email, string FullName, string Password) : IRequest<AuthResultDto>`, `RegisterCommandValidator(IApplicationDbContext)`.

- [ ] **Step 1: Viết `RegisterCommandHandlerTests`**

```csharp
using TaskMgmt.Application.Features.Auth.Commands.Register;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Infrastructure.Identity;

namespace TaskMgmt.Application.UnitTests.Features.Auth;

public class RegisterCommandHandlerTests
{
    private static readonly PasswordHasher PasswordHasher = new();

    [Fact]
    public async Task Handle_ValidInput_CreatesUserWithHashedPasswordAndRefreshToken()
    {
        using var context = TestDbContextFactory.Create();
        var jwtSettings = TestDataFactory.CreateJwtSettings();
        var handler = new RegisterCommandHandler(context, PasswordHasher, new JwtTokenGenerator(jwtSettings), jwtSettings);

        var result = await handler.Handle(
            new RegisterCommand("New.User@Example.com", "New User", "Password123!"), default);

        var user = Assert.Single(context.Users);
        Assert.Equal("new.user@example.com", user.Email);
        Assert.NotEqual("Password123!", user.PasswordHash);
        Assert.True(PasswordHasher.Verify(user.PasswordHash, "Password123!"));
        var storedToken = Assert.Single(context.RefreshTokens);
        Assert.Equal(result.RefreshToken, storedToken.Token);
    }
}
```

- [ ] **Step 2: Viết `RegisterCommandValidatorTests`**

Kiểm tra logic chống trùng email — logic này nằm ở validator (`MustAsync`), không phải ở handler, nên test riêng.

```csharp
using TaskMgmt.Application.Features.Auth.Commands.Register;
using TaskMgmt.Application.UnitTests.Common;

namespace TaskMgmt.Application.UnitTests.Features.Auth;

public class RegisterCommandValidatorTests
{
    [Fact]
    public async Task Validate_EmailAlreadyExists_IsInvalid()
    {
        using var context = TestDbContextFactory.Create();
        var existing = TestDataFactory.CreateUser("taken@example.com");
        context.Users.Add(existing);
        await context.SaveChangesAsync(default);

        var validator = new RegisterCommandValidator(context);
        var command = new RegisterCommand("taken@example.com", "New User", "Password123!");

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.Email));
    }

    [Fact]
    public async Task Validate_NewEmail_IsValid()
    {
        using var context = TestDbContextFactory.Create();
        var validator = new RegisterCommandValidator(context);
        var command = new RegisterCommand("new@example.com", "New User", "Password123!");

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }
}
```

- [ ] **Step 3: Chạy test, xác nhận PASS**

Run: `cd backend && dotnet test tests/TaskMgmt.Application.UnitTests --filter "FullyQualifiedName~Register"`
Expected: PASS (3/3).

- [ ] **Step 4: Commit**

```bash
git add backend/tests/TaskMgmt.Application.UnitTests/Features/Auth/RegisterCommandHandlerTests.cs backend/tests/TaskMgmt.Application.UnitTests/Features/Auth/RegisterCommandValidatorTests.cs
git commit -m "test: backfill unit tests for RegisterCommandHandler and validator"
```

---

### Task 5: Backfill unit test cho `RefreshTokenCommandHandler`

**Files:**
- Create: `backend/tests/TaskMgmt.Application.UnitTests/Features/Auth/RefreshTokenCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `RefreshTokenCommandHandler(IApplicationDbContext, IJwtTokenGenerator, JwtSettings)`, `RefreshTokenCommand(string RefreshToken) : IRequest<AuthResultDto>`.

- [ ] **Step 1: Viết test**

```csharp
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Features.Auth.Commands.RefreshAccessToken;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Entities;
using TaskMgmt.Infrastructure.Identity;

namespace TaskMgmt.Application.UnitTests.Features.Auth;

public class RefreshTokenCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidToken_RotatesAndReturnsNewToken()
    {
        using var context = TestDbContextFactory.Create();
        var jwtSettings = TestDataFactory.CreateJwtSettings();
        var user = TestDataFactory.CreateUser();
        context.Users.Add(user);
        context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = "old-token",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(1),
        });
        await context.SaveChangesAsync(default);

        var handler = new RefreshTokenCommandHandler(context, new JwtTokenGenerator(jwtSettings), jwtSettings);

        var result = await handler.Handle(new RefreshTokenCommand("old-token"), default);

        var oldToken = context.RefreshTokens.Single(t => t.Token == "old-token");
        Assert.NotNull(oldToken.RevokedAtUtc);
        Assert.Equal(result.RefreshToken, oldToken.ReplacedByToken);
        Assert.NotEqual("old-token", result.RefreshToken);
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsUnauthorized()
    {
        using var context = TestDbContextFactory.Create();
        var jwtSettings = TestDataFactory.CreateJwtSettings();
        var user = TestDataFactory.CreateUser();
        context.Users.Add(user);
        context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = "expired-token",
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-20),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
        });
        await context.SaveChangesAsync(default);

        var handler = new RefreshTokenCommandHandler(context, new JwtTokenGenerator(jwtSettings), jwtSettings);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => handler.Handle(new RefreshTokenCommand("expired-token"), default));
    }

    [Fact]
    public async Task Handle_RevokedToken_ThrowsUnauthorized()
    {
        using var context = TestDbContextFactory.Create();
        var jwtSettings = TestDataFactory.CreateJwtSettings();
        var user = TestDataFactory.CreateUser();
        context.Users.Add(user);
        context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = "revoked-token",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(1),
            RevokedAtUtc = DateTimeOffset.UtcNow,
        });
        await context.SaveChangesAsync(default);

        var handler = new RefreshTokenCommandHandler(context, new JwtTokenGenerator(jwtSettings), jwtSettings);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => handler.Handle(new RefreshTokenCommand("revoked-token"), default));
    }

    [Fact]
    public async Task Handle_InactiveUser_ThrowsUnauthorized()
    {
        using var context = TestDbContextFactory.Create();
        var jwtSettings = TestDataFactory.CreateJwtSettings();
        var user = TestDataFactory.CreateUser();
        user.IsActive = false;
        context.Users.Add(user);
        context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = "token",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(1),
        });
        await context.SaveChangesAsync(default);

        var handler = new RefreshTokenCommandHandler(context, new JwtTokenGenerator(jwtSettings), jwtSettings);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => handler.Handle(new RefreshTokenCommand("token"), default));
    }
}
```

- [ ] **Step 2: Chạy test, xác nhận PASS**

Run: `cd backend && dotnet test tests/TaskMgmt.Application.UnitTests --filter "FullyQualifiedName~RefreshTokenCommandHandlerTests"`
Expected: PASS (4/4).

- [ ] **Step 3: Commit**

```bash
git add backend/tests/TaskMgmt.Application.UnitTests/Features/Auth/RefreshTokenCommandHandlerTests.cs
git commit -m "test: backfill unit tests for RefreshTokenCommandHandler"
```

---

### Task 6: `ForgotPassword` command

**Files:**
- Create: `backend/src/TaskMgmt.Application/Features/Auth/Commands/ForgotPassword/ForgotPasswordCommand.cs`
- Create: `backend/src/TaskMgmt.Application/Features/Auth/Commands/ForgotPassword/ForgotPasswordCommandHandler.cs`
- Create: `backend/src/TaskMgmt.Application/Features/Auth/Commands/ForgotPassword/ForgotPasswordCommandValidator.cs`
- Test: `backend/tests/TaskMgmt.Application.UnitTests/Features/Auth/ForgotPasswordCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `IApplicationDbContext.PasswordResetTokens` (Task 1), `RefreshTokenGenerator.Generate() : string` (đã có, `internal` cùng assembly, ở `TaskMgmt.Application.Features.Auth.Common`).
- Produces: `ForgotPasswordCommand(string Email) : IRequest`. Task 9 (Controller) consume.

- [ ] **Step 1: Viết failing test**

```csharp
using Microsoft.Extensions.Logging.Abstractions;
using TaskMgmt.Application.Features.Auth.Commands.ForgotPassword;
using TaskMgmt.Application.UnitTests.Common;

namespace TaskMgmt.Application.UnitTests.Features.Auth;

public class ForgotPasswordCommandHandlerTests
{
    [Fact]
    public async Task Handle_ExistingActiveUser_CreatesResetToken()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync(default);

        var handler = new ForgotPasswordCommandHandler(context, NullLogger<ForgotPasswordCommandHandler>.Instance);

        await handler.Handle(new ForgotPasswordCommand(user.Email), default);

        var token = Assert.Single(context.PasswordResetTokens);
        Assert.Equal(user.Id, token.UserId);
        Assert.True(token.IsActive);
    }

    [Fact]
    public async Task Handle_UnknownEmail_DoesNotCreateToken()
    {
        using var context = TestDbContextFactory.Create();
        var handler = new ForgotPasswordCommandHandler(context, NullLogger<ForgotPasswordCommandHandler>.Instance);

        await handler.Handle(new ForgotPasswordCommand("nobody@example.com"), default);

        Assert.Empty(context.PasswordResetTokens);
    }

    [Fact]
    public async Task Handle_InactiveUser_DoesNotCreateToken()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        user.IsActive = false;
        context.Users.Add(user);
        await context.SaveChangesAsync(default);

        var handler = new ForgotPasswordCommandHandler(context, NullLogger<ForgotPasswordCommandHandler>.Instance);

        await handler.Handle(new ForgotPasswordCommand(user.Email), default);

        Assert.Empty(context.PasswordResetTokens);
    }
}
```

- [ ] **Step 2: Chạy test, xác nhận FAIL**

Run: `cd backend && dotnet test tests/TaskMgmt.Application.UnitTests --filter "FullyQualifiedName~ForgotPasswordCommandHandlerTests"`
Expected: FAIL — build lỗi vì `ForgotPasswordCommand`/`ForgotPasswordCommandHandler` chưa tồn tại.

- [ ] **Step 3: Tạo `ForgotPasswordCommand`**

```csharp
using MediatR;

namespace TaskMgmt.Application.Features.Auth.Commands.ForgotPassword;

public record ForgotPasswordCommand(string Email) : IRequest;
```

- [ ] **Step 4: Tạo `ForgotPasswordCommandHandler`**

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Auth.Common;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler(
    IApplicationDbContext context,
    ILogger<ForgotPasswordCommandHandler> logger) : IRequestHandler<ForgotPasswordCommand>
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(15);

    public async Task Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        // Không tiết lộ email có tồn tại hay không: âm thầm bỏ qua, Controller luôn trả
        // response giống hệt trường hợp thành công dù nhánh này có chạy hay không.
        if (user is null || !user.IsActive)
        {
            return;
        }

        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            Token = RefreshTokenGenerator.Generate(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.Add(TokenLifetime),
        };

        context.PasswordResetTokens.Add(resetToken);
        await context.SaveChangesAsync(cancellationToken);

        // Chưa có dịch vụ gửi email/SMS thật: log token tạm thời để QA/dev test thủ công.
        // Cần thay bằng gọi service gửi thật trước khi lên production.
        logger.LogInformation(
            "Password reset token for user {UserId}: {Token} (expires {ExpiresAtUtc})",
            user.Id, resetToken.Token, resetToken.ExpiresAtUtc);
    }
}
```

- [ ] **Step 5: Tạo `ForgotPasswordCommandValidator`**

```csharp
using FluentValidation;

namespace TaskMgmt.Application.Features.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
```

- [ ] **Step 6: Chạy test, xác nhận PASS**

Run: `cd backend && dotnet test tests/TaskMgmt.Application.UnitTests --filter "FullyQualifiedName~ForgotPasswordCommandHandlerTests"`
Expected: PASS (3/3).

- [ ] **Step 7: Commit**

```bash
git add backend/src/TaskMgmt.Application/Features/Auth/Commands/ForgotPassword/ backend/tests/TaskMgmt.Application.UnitTests/Features/Auth/ForgotPasswordCommandHandlerTests.cs
git commit -m "feat: add ForgotPassword command"
```

---

### Task 7: `ResetPassword` command

**Files:**
- Create: `backend/src/TaskMgmt.Application/Features/Auth/Commands/ResetPassword/ResetPasswordCommand.cs`
- Create: `backend/src/TaskMgmt.Application/Features/Auth/Commands/ResetPassword/ResetPasswordCommandHandler.cs`
- Create: `backend/src/TaskMgmt.Application/Features/Auth/Commands/ResetPassword/ResetPasswordCommandValidator.cs`
- Test: `backend/tests/TaskMgmt.Application.UnitTests/Features/Auth/ResetPasswordCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `IApplicationDbContext.PasswordResetTokens`, `IApplicationDbContext.RefreshTokens`, `IPasswordHasher.Hash(string) : string` (đã có).
- Produces: `ResetPasswordCommand(string Token, string NewPassword) : IRequest`. Task 9 consume.

- [ ] **Step 1: Viết failing test**

```csharp
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Features.Auth.Commands.ResetPassword;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Entities;
using TaskMgmt.Infrastructure.Identity;

namespace TaskMgmt.Application.UnitTests.Features.Auth;

public class ResetPasswordCommandHandlerTests
{
    private static readonly PasswordHasher PasswordHasher = new();

    [Fact]
    public async Task Handle_ValidToken_UpdatesPasswordAndRevokesActiveRefreshTokens()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        user.PasswordHash = PasswordHasher.Hash("OldPassword1!");
        context.Users.Add(user);
        context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = "active-refresh",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(1),
        });
        context.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            Token = "reset-token",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(15),
        });
        await context.SaveChangesAsync(default);

        var handler = new ResetPasswordCommandHandler(context, PasswordHasher);

        await handler.Handle(new ResetPasswordCommand("reset-token", "NewPassword1!"), default);

        var updatedUser = context.Users.Single(u => u.Id == user.Id);
        Assert.True(PasswordHasher.Verify(updatedUser.PasswordHash, "NewPassword1!"));
        var usedToken = context.PasswordResetTokens.Single(t => t.Token == "reset-token");
        Assert.NotNull(usedToken.UsedAtUtc);
        var revokedRefreshToken = context.RefreshTokens.Single(t => t.Token == "active-refresh");
        Assert.NotNull(revokedRefreshToken.RevokedAtUtc);
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsUnauthorized()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        context.Users.Add(user);
        context.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            Token = "expired-reset",
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-20),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
        });
        await context.SaveChangesAsync(default);

        var handler = new ResetPasswordCommandHandler(context, PasswordHasher);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => handler.Handle(new ResetPasswordCommand("expired-reset", "NewPassword1!"), default));
    }

    [Fact]
    public async Task Handle_AlreadyUsedToken_ThrowsUnauthorized()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        context.Users.Add(user);
        context.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            Token = "used-reset",
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(10),
            UsedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
        });
        await context.SaveChangesAsync(default);

        var handler = new ResetPasswordCommandHandler(context, PasswordHasher);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => handler.Handle(new ResetPasswordCommand("used-reset", "NewPassword1!"), default));
    }

    [Fact]
    public async Task Handle_UnknownToken_ThrowsUnauthorized()
    {
        using var context = TestDbContextFactory.Create();
        var handler = new ResetPasswordCommandHandler(context, PasswordHasher);

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => handler.Handle(new ResetPasswordCommand("no-such-token", "NewPassword1!"), default));
    }
}
```

- [ ] **Step 2: Chạy test, xác nhận FAIL**

Run: `cd backend && dotnet test tests/TaskMgmt.Application.UnitTests --filter "FullyQualifiedName~ResetPasswordCommandHandlerTests"`
Expected: FAIL — build lỗi vì `ResetPasswordCommand`/`ResetPasswordCommandHandler` chưa tồn tại.

- [ ] **Step 3: Tạo `ResetPasswordCommand`**

```csharp
using MediatR;

namespace TaskMgmt.Application.Features.Auth.Commands.ResetPassword;

public record ResetPasswordCommand(string Token, string NewPassword) : IRequest;
```

- [ ] **Step 4: Tạo `ResetPasswordCommandHandler`**

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Common.Interfaces;

namespace TaskMgmt.Application.Features.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher)
    : IRequestHandler<ResetPasswordCommand>
{
    private const string InvalidTokenMessage = "Token đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.";

    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var resetToken = await context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == request.Token, cancellationToken)
            ?? throw new UnauthorizedException(InvalidTokenMessage);

        if (!resetToken.IsActive || resetToken.User is null)
        {
            throw new UnauthorizedException(InvalidTokenMessage);
        }

        var user = resetToken.User;
        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        resetToken.UsedAtUtc = DateTimeOffset.UtcNow;

        var activeRefreshTokens = await context.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeRefreshTokens)
        {
            token.RevokedAtUtc = DateTimeOffset.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 5: Tạo `ResetPasswordCommandValidator`**

```csharp
using FluentValidation;

namespace TaskMgmt.Application.Features.Auth.Commands.ResetPassword;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(100);
    }
}
```

- [ ] **Step 6: Chạy test, xác nhận PASS**

Run: `cd backend && dotnet test tests/TaskMgmt.Application.UnitTests --filter "FullyQualifiedName~ResetPasswordCommandHandlerTests"`
Expected: PASS (4/4).

- [ ] **Step 7: Commit**

```bash
git add backend/src/TaskMgmt.Application/Features/Auth/Commands/ResetPassword/ backend/tests/TaskMgmt.Application.UnitTests/Features/Auth/ResetPasswordCommandHandlerTests.cs
git commit -m "feat: add ResetPassword command"
```

---

### Task 8: `Logout` command

**Files:**
- Create: `backend/src/TaskMgmt.Application/Features/Auth/Commands/Logout/LogoutCommand.cs`
- Create: `backend/src/TaskMgmt.Application/Features/Auth/Commands/Logout/LogoutCommandHandler.cs`
- Create: `backend/src/TaskMgmt.Application/Features/Auth/Commands/Logout/LogoutCommandValidator.cs`
- Test: `backend/tests/TaskMgmt.Application.UnitTests/Features/Auth/LogoutCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `IApplicationDbContext.RefreshTokens`, `ICurrentUserService.UserId : Guid?` (đã có), `FakeCurrentUserService(Guid? userId, SystemRole? role)` (đã có trong `Common/`).
- Produces: `LogoutCommand(string RefreshToken) : IRequest`. Task 9 consume.

- [ ] **Step 1: Viết failing test**

```csharp
using TaskMgmt.Application.Features.Auth.Commands.Logout;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.UnitTests.Features.Auth;

public class LogoutCommandHandlerTests
{
    [Fact]
    public async Task Handle_OwnActiveToken_RevokesIt()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        context.Users.Add(user);
        context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = "my-token",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(1),
        });
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(user.Id, user.SystemRole);
        var handler = new LogoutCommandHandler(context, currentUser);

        await handler.Handle(new LogoutCommand("my-token"), default);

        var token = context.RefreshTokens.Single(t => t.Token == "my-token");
        Assert.NotNull(token.RevokedAtUtc);
    }

    [Fact]
    public async Task Handle_TokenBelongsToAnotherUser_DoesNothing()
    {
        using var context = TestDbContextFactory.Create();
        var owner = TestDataFactory.CreateUser("owner@example.com");
        var otherUser = TestDataFactory.CreateUser("other@example.com");
        context.Users.AddRange(owner, otherUser);
        context.RefreshTokens.Add(new RefreshToken
        {
            UserId = owner.Id,
            Token = "owners-token",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(1),
        });
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(otherUser.Id, otherUser.SystemRole);
        var handler = new LogoutCommandHandler(context, currentUser);

        await handler.Handle(new LogoutCommand("owners-token"), default);

        var token = context.RefreshTokens.Single(t => t.Token == "owners-token");
        Assert.Null(token.RevokedAtUtc);
    }

    [Fact]
    public async Task Handle_UnknownToken_DoesNotThrow()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(user.Id, user.SystemRole);
        var handler = new LogoutCommandHandler(context, currentUser);

        await handler.Handle(new LogoutCommand("no-such-token"), default);
    }

    [Fact]
    public async Task Handle_AlreadyRevokedToken_StaysRevoked()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        context.Users.Add(user);
        var revokedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = "already-revoked",
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-1),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(1),
            RevokedAtUtc = revokedAt,
        });
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(user.Id, user.SystemRole);
        var handler = new LogoutCommandHandler(context, currentUser);

        await handler.Handle(new LogoutCommand("already-revoked"), default);

        var token = context.RefreshTokens.Single(t => t.Token == "already-revoked");
        Assert.Equal(revokedAt, token.RevokedAtUtc);
    }
}
```

- [ ] **Step 2: Chạy test, xác nhận FAIL**

Run: `cd backend && dotnet test tests/TaskMgmt.Application.UnitTests --filter "FullyQualifiedName~LogoutCommandHandlerTests"`
Expected: FAIL — build lỗi vì `LogoutCommand`/`LogoutCommandHandler` chưa tồn tại.

- [ ] **Step 3: Tạo `LogoutCommand`**

```csharp
using MediatR;

namespace TaskMgmt.Application.Features.Auth.Commands.Logout;

public record LogoutCommand(string RefreshToken) : IRequest;
```

- [ ] **Step 4: Tạo `LogoutCommandHandler`**

Mirror `UnregisterDeviceTokenCommandHandler` — idempotent, không báo lỗi nếu token không tồn tại/không khớp user.

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;

namespace TaskMgmt.Application.Features.Auth.Commands.Logout;

public class LogoutCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        // Idempotent: không báo lỗi nếu token không tồn tại, thuộc user khác, hoặc đã bị
        // revoke/rotate trước đó (ví dụ do tab/thiết bị khác tự refresh) — đăng xuất luôn
        // phải thành công.
        var token = await context.RefreshTokens.FirstOrDefaultAsync(
            t => t.Token == request.RefreshToken && t.UserId == currentUser.UserId,
            cancellationToken);

        if (token is not null && token.IsActive)
        {
            token.RevokedAtUtc = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
```

- [ ] **Step 5: Tạo `LogoutCommandValidator`**

```csharp
using FluentValidation;

namespace TaskMgmt.Application.Features.Auth.Commands.Logout;

public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
```

- [ ] **Step 6: Chạy test, xác nhận PASS**

Run: `cd backend && dotnet test tests/TaskMgmt.Application.UnitTests --filter "FullyQualifiedName~LogoutCommandHandlerTests"`
Expected: PASS (4/4).

- [ ] **Step 7: Commit**

```bash
git add backend/src/TaskMgmt.Application/Features/Auth/Commands/Logout/ backend/tests/TaskMgmt.Application.UnitTests/Features/Auth/LogoutCommandHandlerTests.cs
git commit -m "feat: add Logout command"
```

---

### Task 9: Wire `AuthController`

**Files:**
- Modify: `backend/src/TaskMgmt.API/Controllers/AuthController.cs`

**Interfaces:**
- Consumes: `ForgotPasswordCommand` (Task 6), `ResetPasswordCommand` (Task 7), `LogoutCommand` (Task 8).

- [ ] **Step 1: Viết lại toàn bộ `AuthController.cs`**

Bỏ `[AllowAnonymous]` ở mức class (vì `Logout` cần yêu cầu đăng nhập qua `FallbackPolicy` mặc định của API — `[AllowAnonymous]` ở class sẽ tắt hoàn toàn authorization cho mọi action bên trong, kể cả khi thêm `[Authorize]` ở action). Thay vào đó, đánh dấu `[AllowAnonymous]` riêng cho từng action công khai.

```csharp
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskMgmt.Application.Features.Auth.Commands.ForgotPassword;
using TaskMgmt.Application.Features.Auth.Commands.Login;
using TaskMgmt.Application.Features.Auth.Commands.Logout;
using TaskMgmt.Application.Features.Auth.Commands.RefreshAccessToken;
using TaskMgmt.Application.Features.Auth.Commands.Register;
using TaskMgmt.Application.Features.Auth.Commands.ResetPassword;
using TaskMgmt.Application.Features.Auth.Common;

namespace TaskMgmt.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(ISender sender) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResultDto>> Register(RegisterCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResultDto>> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public async Task<ActionResult<AuthResultDto>> RefreshToken(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return Ok(new { message = "Nếu email tồn tại trong hệ thống, hướng dẫn đặt lại mật khẩu đã được gửi." });
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    // Không có [AllowAnonymous]: yêu cầu access token hợp lệ qua FallbackPolicy
    // (RequireAuthenticatedUser) đã cấu hình trong Program.cs.
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return NoContent();
    }
}
```

- [ ] **Step 2: Build và chạy toàn bộ test backend**

Run: `cd backend && dotnet build TaskMgmt.slnx && dotnet test TaskMgmt.slnx`
Expected: build thành công, toàn bộ test PASS (bao gồm test cũ + test mới từ Task 3–8).

- [ ] **Step 3: Kiểm tra thủ công qua Swagger**

Run: `cd backend/src/TaskMgmt.API && dotnet run`
Mở `https://localhost:<port>/swagger`, xác nhận thấy đủ 6 endpoint dưới `Auth`: `register`, `login`, `refresh-token`, `forgot-password`, `reset-password`, `logout`. Gọi thử `POST /api/v1/auth/logout` không kèm Bearer token → phải trả `401`.

- [ ] **Step 4: Commit**

```bash
git add backend/src/TaskMgmt.API/Controllers/AuthController.cs
git commit -m "feat: wire forgot-password, reset-password, and logout endpoints"
```

---

### Task 10: Mở rộng `CleanupExpiredTokensJob` dọn `PasswordResetToken`

**Files:**
- Modify: `backend/src/TaskMgmt.Infrastructure/BackgroundJobs/CleanupExpiredTokensJob.cs`
- Modify: `backend/tests/TaskMgmt.Application.UnitTests/BackgroundJobs/CleanupExpiredTokensJobTests.cs`

**Interfaces:**
- Consumes: `IApplicationDbContext.PasswordResetTokens` (Task 1).

- [ ] **Step 1: Viết failing test**

Thêm test case sau vào cuối class `CleanupExpiredTokensJobTests` (file đã tồn tại):

```csharp
    [Fact]
    public async Task Execute_RemovesExpiredAndUsedPasswordResetTokens()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        context.Users.Add(user);

        var expiredReset = new PasswordResetToken
        {
            UserId = user.Id,
            Token = "expired-reset",
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-30),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-15),
        };
        var usedReset = new PasswordResetToken
        {
            UserId = user.Id,
            Token = "used-reset",
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-10),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5),
            UsedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
        };
        var activeReset = new PasswordResetToken
        {
            UserId = user.Id,
            Token = "active-reset",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(15),
        };
        context.PasswordResetTokens.AddRange(expiredReset, usedReset, activeReset);
        await context.SaveChangesAsync(default);

        var job = new CleanupExpiredTokensJob(context, NullLogger<CleanupExpiredTokensJob>.Instance);

        await job.ExecuteAsync();

        var remaining = context.PasswordResetTokens.ToList();
        Assert.Single(remaining);
        Assert.Equal("active-reset", remaining[0].Token);
    }
```

Thêm `using TaskMgmt.Domain.Entities;` đã có sẵn ở đầu file (dùng chung với `RefreshToken`), không cần thêm using mới.

- [ ] **Step 2: Chạy test, xác nhận FAIL**

Run: `cd backend && dotnet test tests/TaskMgmt.Application.UnitTests --filter "FullyQualifiedName~Execute_RemovesExpiredAndUsedPasswordResetTokens"`
Expected: FAIL — job chưa xoá `PasswordResetToken`, `remaining.Count` sẽ là 3 thay vì 1.

- [ ] **Step 3: Cập nhật `CleanupExpiredTokensJob`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskMgmt.Application.Common.Interfaces;

namespace TaskMgmt.Infrastructure.BackgroundJobs;

// Dọn refresh token và password reset token đã hết hạn/đã dùng để các bảng không phình
// vô hạn theo thời gian.
public class CleanupExpiredTokensJob(IApplicationDbContext context, ILogger<CleanupExpiredTokensJob> logger)
{
    public async Task ExecuteAsync()
    {
        var cancellationToken = CancellationToken.None;
        var now = DateTimeOffset.UtcNow;

        var expiredRefreshTokens = await context.RefreshTokens
            .Where(t => t.ExpiresAtUtc < now)
            .ToListAsync(cancellationToken);

        if (expiredRefreshTokens.Count > 0)
        {
            context.RefreshTokens.RemoveRange(expiredRefreshTokens);
        }

        var stalePasswordResetTokens = await context.PasswordResetTokens
            .Where(t => t.ExpiresAtUtc < now || t.UsedAtUtc != null)
            .ToListAsync(cancellationToken);

        if (stalePasswordResetTokens.Count > 0)
        {
            context.PasswordResetTokens.RemoveRange(stalePasswordResetTokens);
        }

        if (expiredRefreshTokens.Count > 0 || stalePasswordResetTokens.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation(
            "CleanupExpiredTokensJob: đã xoá {RefreshCount} refresh token hết hạn, {ResetCount} password reset token hết hạn/đã dùng.",
            expiredRefreshTokens.Count, stalePasswordResetTokens.Count);
    }
}
```

- [ ] **Step 4: Chạy toàn bộ test của file, xác nhận PASS**

Run: `cd backend && dotnet test tests/TaskMgmt.Application.UnitTests --filter "FullyQualifiedName~CleanupExpiredTokensJobTests"`
Expected: PASS (3/3 — 2 test cũ + 1 test mới).

- [ ] **Step 5: Commit**

```bash
git add backend/src/TaskMgmt.Infrastructure/BackgroundJobs/CleanupExpiredTokensJob.cs backend/tests/TaskMgmt.Application.UnitTests/BackgroundJobs/CleanupExpiredTokensJobTests.cs
git commit -m "feat: purge expired password reset tokens in CleanupExpiredTokensJob"
```

---

## Mobile

### Task 11: `AuthRemoteDataSource` + `AuthRepository` cho forgot/reset/logout

**Files:**
- Modify: `mobile/taskmgmt_app/lib/features/auth/data/datasources/auth_remote_data_source.dart`
- Modify: `mobile/taskmgmt_app/lib/features/auth/domain/repositories/auth_repository.dart`
- Modify: `mobile/taskmgmt_app/lib/features/auth/data/repositories/auth_repository_impl.dart`

**Interfaces:**
- Produces: `AuthRepository.forgotPassword(String email) : Future<void>`, `AuthRepository.resetPassword({required String token, required String newPassword}) : Future<void>`. Task 12, 13 consume qua `authRepositoryProvider` (đã có sẵn trong `auth_provider.dart`, không cần sửa).

- [ ] **Step 1: Thêm method vào `AuthRemoteDataSource`**

Trong `auth_remote_data_source.dart`, thêm 3 method sau vào cuối class (sau `refreshToken`):

```dart
  Future<void> forgotPassword(String email) async {
    try {
      await _dio.post<void>('/auth/forgot-password', data: {'email': email});
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<void> resetPassword({required String token, required String newPassword}) async {
    try {
      await _dio.post<void>('/auth/reset-password', data: {'token': token, 'newPassword': newPassword});
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<void> logout(String refreshToken) async {
    try {
      await _dio.post<void>('/auth/logout', data: {'refreshToken': refreshToken});
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }
```

- [ ] **Step 2: Thêm method vào interface `AuthRepository`**

Trong `auth_repository.dart`, thêm 2 dòng sau ngay trước `Future<void> logout();`:

```dart
  Future<void> forgotPassword(String email);

  Future<void> resetPassword({required String token, required String newPassword});

```

- [ ] **Step 3: Implement trong `AuthRepositoryImpl`**

Trong `auth_repository_impl.dart`, thêm 2 method mới và sửa `logout()` để gọi API trước khi xoá session cục bộ:

```dart
  @override
  Future<void> forgotPassword(String email) => _remoteDataSource.forgotPassword(email);

  @override
  Future<void> resetPassword({required String token, required String newPassword}) =>
      _remoteDataSource.resetPassword(token: token, newPassword: newPassword);

  @override
  Future<void> logout() async {
    final session = await _tokenStorage.readSession();
    if (session != null) {
      try {
        await _remoteDataSource.logout(session.refreshToken);
      } catch (_) {
        // Best-effort: đăng xuất cục bộ vẫn phải thành công dù API logout lỗi
        // (mất mạng, refresh token đã hết hạn/bị rotate ở thiết bị khác...).
      }
    }
    await _tokenStorage.clear();
  }
```

Xoá dòng `Future<void> logout() => _tokenStorage.clear();` cũ (thay bằng block ở trên).

- [ ] **Step 4: Phân tích tĩnh**

Run: `cd mobile/taskmgmt_app && flutter analyze`
Expected: không có lỗi/warning mới liên quan tới các file vừa sửa.

- [ ] **Step 5: Commit**

```bash
git add mobile/taskmgmt_app/lib/features/auth/data/datasources/auth_remote_data_source.dart mobile/taskmgmt_app/lib/features/auth/domain/repositories/auth_repository.dart mobile/taskmgmt_app/lib/features/auth/data/repositories/auth_repository_impl.dart
git commit -m "feat(mobile): add forgot/reset password calls and call backend on logout"
```

---

### Task 12: Màn hình `ForgotPasswordScreen`

**Files:**
- Create: `mobile/taskmgmt_app/lib/features/auth/presentation/screens/forgot_password_screen.dart`
- Modify: `mobile/taskmgmt_app/lib/features/auth/presentation/screens/login_screen.dart`
- Modify: `mobile/taskmgmt_app/lib/core/routing/app_router.dart`

**Interfaces:**
- Consumes: `authRepositoryProvider` (có sẵn, `auth_provider.dart`), `AuthRepository.forgotPassword(String) : Future<void>` (Task 11).
- Produces: `ForgotPasswordScreen.path = '/forgot-password'`, `ForgotPasswordScreen.name = 'forgot-password'`. Task 13 và `app_router.dart` dùng path này.

- [ ] **Step 1: Tạo `ForgotPasswordScreen`**

```dart
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/network/api_exception.dart';
import '../providers/auth_provider.dart';

class ForgotPasswordScreen extends ConsumerStatefulWidget {
  const ForgotPasswordScreen({super.key});

  static const path = '/forgot-password';
  static const name = 'forgot-password';

  @override
  ConsumerState<ForgotPasswordScreen> createState() => _ForgotPasswordScreenState();
}

class _ForgotPasswordScreenState extends ConsumerState<ForgotPasswordScreen> {
  final _formKey = GlobalKey<FormState>();
  final _emailController = TextEditingController();

  bool _isSubmitting = false;
  bool _submitted = false;
  String? _errorMessage;

  @override
  void dispose() {
    _emailController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() {
      _isSubmitting = true;
      _errorMessage = null;
    });

    try {
      await ref.read(authRepositoryProvider).forgotPassword(_emailController.text.trim());
      setState(() => _submitted = true);
    } catch (e) {
      setState(() {
        _errorMessage = e is ApiException ? e.allMessages.join('\n') : 'Đã có lỗi xảy ra. Vui lòng thử lại.';
      });
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Quên mật khẩu')),
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 400),
              child: _submitted ? _buildSuccessMessage(context) : _buildForm(context),
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildSuccessMessage(BuildContext context) => Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.mark_email_read_outlined, size: 64, color: Theme.of(context).colorScheme.primary),
          const SizedBox(height: 16),
          const Text(
            'Nếu email tồn tại trong hệ thống, hướng dẫn đặt lại mật khẩu đã được gửi.',
            textAlign: TextAlign.center,
          ),
        ],
      );

  Widget _buildForm(BuildContext context) => Form(
        key: _formKey,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              'Nhập email để nhận hướng dẫn đặt lại mật khẩu',
              style: Theme.of(context).textTheme.headlineSmall,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 24),
            if (_errorMessage != null) ...[
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: Theme.of(context).colorScheme.errorContainer,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  _errorMessage!,
                  style: TextStyle(color: Theme.of(context).colorScheme.onErrorContainer),
                ),
              ),
              const SizedBox(height: 16),
            ],
            TextFormField(
              controller: _emailController,
              keyboardType: TextInputType.emailAddress,
              textInputAction: TextInputAction.done,
              decoration: const InputDecoration(labelText: 'Email', prefixIcon: Icon(Icons.email_outlined)),
              validator: (value) {
                if (value == null || value.trim().isEmpty) return 'Vui lòng nhập email.';
                if (!value.contains('@')) return 'Email không hợp lệ.';
                return null;
              },
              onFieldSubmitted: (_) => _submit(),
            ),
            const SizedBox(height: 24),
            FilledButton(
              onPressed: _isSubmitting ? null : _submit,
              child: _isSubmitting
                  ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                  : const Text('Gửi yêu cầu'),
            ),
          ],
        ),
      );
}
```

- [ ] **Step 2: Thêm link "Quên mật khẩu?" vào `LoginScreen`**

Trong `login_screen.dart`, thêm import:

```dart
import 'forgot_password_screen.dart';
```

Và thêm `TextButton` sau nút `TextButton` "Chưa có tài khoản? Đăng ký ngay" hiện có (trước `],` đóng `children`):

```dart
                    TextButton(
                      onPressed: _isSubmitting ? null : () => context.push(ForgotPasswordScreen.path),
                      child: const Text('Quên mật khẩu?'),
                    ),
```

- [ ] **Step 3: Đăng ký route trong `app_router.dart`**

Thêm import:

```dart
import '../../features/auth/presentation/screens/forgot_password_screen.dart';
```

Sửa dòng `isAuthRoute` để bao gồm route mới:

```dart
      final isAuthRoute = state.matchedLocation == LoginScreen.path ||
          state.matchedLocation == RegisterScreen.path ||
          state.matchedLocation == ForgotPasswordScreen.path;
```

Thêm `GoRoute` mới ngay sau route của `RegisterScreen`:

```dart
      GoRoute(
        path: ForgotPasswordScreen.path,
        name: ForgotPasswordScreen.name,
        builder: (context, state) => const ForgotPasswordScreen(),
      ),
```

- [ ] **Step 4: Phân tích tĩnh**

Run: `cd mobile/taskmgmt_app && flutter analyze`
Expected: không có lỗi.

- [ ] **Step 5: Commit**

```bash
git add mobile/taskmgmt_app/lib/features/auth/presentation/screens/forgot_password_screen.dart mobile/taskmgmt_app/lib/features/auth/presentation/screens/login_screen.dart mobile/taskmgmt_app/lib/core/routing/app_router.dart
git commit -m "feat(mobile): add ForgotPasswordScreen"
```

---

### Task 13: Màn hình `ResetPasswordScreen`

**Files:**
- Create: `mobile/taskmgmt_app/lib/features/auth/presentation/screens/reset_password_screen.dart`
- Modify: `mobile/taskmgmt_app/lib/core/routing/app_router.dart`

**Interfaces:**
- Consumes: `authRepositoryProvider`, `AuthRepository.resetPassword({required String token, required String newPassword}) : Future<void>` (Task 11), `LoginScreen.path` (có sẵn).
- Produces: `ResetPasswordScreen.path = '/reset-password'`, `ResetPasswordScreen.name = 'reset-password'`.

- [ ] **Step 1: Tạo `ResetPasswordScreen`**

```dart
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/network/api_exception.dart';
import '../providers/auth_provider.dart';
import 'login_screen.dart';

class ResetPasswordScreen extends ConsumerStatefulWidget {
  const ResetPasswordScreen({super.key});

  static const path = '/reset-password';
  static const name = 'reset-password';

  @override
  ConsumerState<ResetPasswordScreen> createState() => _ResetPasswordScreenState();
}

class _ResetPasswordScreenState extends ConsumerState<ResetPasswordScreen> {
  final _formKey = GlobalKey<FormState>();
  final _tokenController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();

  bool _isSubmitting = false;
  bool _obscurePassword = true;
  bool _succeeded = false;
  String? _errorMessage;

  @override
  void dispose() {
    _tokenController.dispose();
    _passwordController.dispose();
    _confirmPasswordController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() {
      _isSubmitting = true;
      _errorMessage = null;
    });

    try {
      await ref.read(authRepositoryProvider).resetPassword(
            token: _tokenController.text.trim(),
            newPassword: _passwordController.text,
          );
      setState(() => _succeeded = true);
    } catch (e) {
      setState(() {
        _errorMessage = e is ApiException ? e.allMessages.join('\n') : 'Đã có lỗi xảy ra. Vui lòng thử lại.';
      });
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Đặt lại mật khẩu')),
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 400),
              child: _succeeded ? _buildSuccessMessage(context) : _buildForm(context),
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildSuccessMessage(BuildContext context) => Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.check_circle_outline, size: 64, color: Theme.of(context).colorScheme.primary),
          const SizedBox(height: 16),
          const Text('Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại.', textAlign: TextAlign.center),
          const SizedBox(height: 24),
          FilledButton(
            onPressed: () => context.go(LoginScreen.path),
            child: const Text('Về trang đăng nhập'),
          ),
        ],
      );

  Widget _buildForm(BuildContext context) => Form(
        key: _formKey,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              'Nhập mã và mật khẩu mới',
              style: Theme.of(context).textTheme.headlineSmall,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 24),
            if (_errorMessage != null) ...[
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: Theme.of(context).colorScheme.errorContainer,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  _errorMessage!,
                  style: TextStyle(color: Theme.of(context).colorScheme.onErrorContainer),
                ),
              ),
              const SizedBox(height: 16),
            ],
            TextFormField(
              controller: _tokenController,
              textInputAction: TextInputAction.next,
              decoration: const InputDecoration(
                labelText: 'Mã đặt lại mật khẩu',
                prefixIcon: Icon(Icons.vpn_key_outlined),
              ),
              validator: (value) {
                if (value == null || value.trim().isEmpty) return 'Vui lòng nhập mã đặt lại mật khẩu.';
                return null;
              },
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _passwordController,
              obscureText: _obscurePassword,
              textInputAction: TextInputAction.next,
              decoration: InputDecoration(
                labelText: 'Mật khẩu mới',
                prefixIcon: const Icon(Icons.lock_outline),
                suffixIcon: IconButton(
                  icon: Icon(_obscurePassword ? Icons.visibility_outlined : Icons.visibility_off_outlined),
                  onPressed: () => setState(() => _obscurePassword = !_obscurePassword),
                ),
              ),
              validator: (value) {
                if (value == null || value.isEmpty) return 'Vui lòng nhập mật khẩu.';
                if (value.length < 8) return 'Mật khẩu phải có ít nhất 8 ký tự.';
                return null;
              },
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _confirmPasswordController,
              obscureText: _obscurePassword,
              textInputAction: TextInputAction.done,
              decoration: const InputDecoration(
                labelText: 'Xác nhận mật khẩu mới',
                prefixIcon: Icon(Icons.lock_outline),
              ),
              validator: (value) {
                if (value != _passwordController.text) return 'Mật khẩu xác nhận không khớp.';
                return null;
              },
              onFieldSubmitted: (_) => _submit(),
            ),
            const SizedBox(height: 24),
            FilledButton(
              onPressed: _isSubmitting ? null : _submit,
              child: _isSubmitting
                  ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                  : const Text('Đặt lại mật khẩu'),
            ),
          ],
        ),
      );
}
```

- [ ] **Step 2: Đăng ký route trong `app_router.dart`**

Thêm import:

```dart
import '../../features/auth/presentation/screens/reset_password_screen.dart';
```

Sửa `isAuthRoute` (đã có `ForgotPasswordScreen.path` từ Task 12) để thêm route này:

```dart
      final isAuthRoute = state.matchedLocation == LoginScreen.path ||
          state.matchedLocation == RegisterScreen.path ||
          state.matchedLocation == ForgotPasswordScreen.path ||
          state.matchedLocation == ResetPasswordScreen.path;
```

Thêm `GoRoute` mới ngay sau route của `ForgotPasswordScreen`:

```dart
      GoRoute(
        path: ResetPasswordScreen.path,
        name: ResetPasswordScreen.name,
        builder: (context, state) => const ResetPasswordScreen(),
      ),
```

- [ ] **Step 3: Phân tích tĩnh và chạy toàn bộ test mobile**

Run: `cd mobile/taskmgmt_app && flutter analyze && flutter test`
Expected: không có lỗi phân tích tĩnh; toàn bộ test hiện có PASS (không có test mới cho 2 màn hình này — nằm ngoài phạm vi spec).

- [ ] **Step 4: Commit**

```bash
git add mobile/taskmgmt_app/lib/features/auth/presentation/screens/reset_password_screen.dart mobile/taskmgmt_app/lib/core/routing/app_router.dart
git commit -m "feat(mobile): add ResetPasswordScreen"
```

---

## Self-Review Notes

- **Spec coverage:** Task 1, 6, 7 → data model + API cho Quên/Đặt lại mật khẩu; Task 8, 9 → Logout; Task 3, 4, 5, 6, 7, 8 → unit test cho toàn bộ 6 Auth handler (3 cũ + 3 mới); Task 10 → dọn `PasswordResetToken` hết hạn (mục "Rủi ro & lưu ý" của spec); Task 11–13 → mobile forgot/reset/logout. Toàn bộ mục trong spec section 2–7 đều có task tương ứng.
- **Placeholder scan:** không còn "TBD"/"implement later" — mọi step đều có code đầy đủ.
- **Type consistency:** đã đối chiếu chữ ký `LoginCommandHandler`, `RegisterCommandHandler`, `RefreshTokenCommandHandler` với source thật trong repo trước khi viết test (Task 3–5); `LogoutCommandHandler`/`ForgotPasswordCommandHandler`/`ResetPasswordCommandHandler` dùng đúng tên method/property được định nghĩa trong task tạo ra chúng (Task 6–8) khi được Task 9 tham chiếu.
