# Cài đặt loại thông báo Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Cho phép user tự bật/tắt push theo từng loại thông báo (10 loại thật đang phát sinh trong hệ thống), trong khi vẫn giữ nguyên lịch sử trong-app đầy đủ dù loại đó đã bị tắt push.

**Architecture:** Bảng thưa `notification_preferences` — chỉ có row khi 1 loại bị **tắt**, vắng mặt = mặc định bật. `TaskNotificationHelper.NotifyAsync` (điểm hội tụ duy nhất ghi Notification + enqueue push) đọc set loại đã tắt qua cache-aside trước khi enqueue push, KHÔNG đụng vào dòng ghi `Notification` trong-app. Backend theo đúng CQRS/MediatR pattern hiện có (Command/Query + FluentValidation Validator dưới `Features/Notifications/`). Mobile mở rộng `NotificationRepository` hiện có (không tạo repository riêng) + 1 màn hình mới mở từ app bar của `NotificationCenterScreen`.

**Tech Stack:** .NET 10, MediatR, FluentValidation, EF Core (PostgreSQL), xUnit + EF Core InMemory (`TestServiceProviderFactory`/`TestDbContextFactory`), Flutter/Riverpod/Dio/go_router, flutter_test.

## Global Constraints

- Namespace mirror thư mục; 4-space indent, CRLF, file-scoped namespace (theo `backend/.editorconfig`).
- Validate chỉ qua FluentValidation, không validate tay trong handler.
- Mọi command/query mới tự động được MediatR + FluentValidation nhận diện qua assembly scan trong `TaskMgmt.Application/DependencyInjection.cs` — không cần đăng ký DI thủ công. Configuration EF mới cũng tự động được `ApplyConfigurationsFromAssembly` nhận diện.
- Tắt 1 loại thông báo **chỉ chặn push (FCM)**, KHÔNG được xoá/chặn việc ghi `Notification` trong-app — hành vi này nằm đúng 1 chỗ trong `TaskNotificationHelper.NotifyAsync`, không sửa ở từng EventHandler riêng lẻ.
- Danh sách 10 loại hợp lệ là hằng số code (`NotificationTypes.All`), không lưu DB — dùng để validate `PUT` và để trả đủ 10 dòng ở `GET` kể cả loại chưa có row nào.
- Cache set "loại đã tắt" theo user dùng `ICacheService` cache-aside (giống `UnreadNotificationCount`), TTL 5 phút, invalidate ngay trong command update.
- Ngoài phạm vi: gộp nhóm loại thông báo trên UI, mute theo từng task cụ thể (`task_mutes` — khái niệm khác, chưa tồn tại trong code), đồng bộ preference qua SignalR real-time.

Spec đầy đủ: `docs/superpowers/specs/2026-08-16-notification-preferences-design.md`.

---

## Backend

### Task 1: `NotificationPreference` data model + migration

**Files:**
- Create: `backend/src/TaskMgmt.Domain/Entities/NotificationPreference.cs`
- Modify: `backend/src/TaskMgmt.Application/Common/Interfaces/IApplicationDbContext.cs`
- Modify: `backend/src/TaskMgmt.Infrastructure/Persistence/AppDbContext.cs`
- Create: `backend/src/TaskMgmt.Infrastructure/Persistence/Configurations/NotificationPreferenceConfiguration.cs`
- Create (via EF CLI): migration under `backend/src/TaskMgmt.Infrastructure/Persistence/Migrations/`

**Interfaces:**
- Produces: `NotificationPreference` entity với `UserId (Guid)`, `User? (User)`, `Type (string)`. `IApplicationDbContext.NotificationPreferences : DbSet<NotificationPreference>`. Task 3 tiêu thụ.

- [ ] **Step 1: Tạo entity `NotificationPreference`**

```csharp
using TaskMgmt.Domain.Common;

namespace TaskMgmt.Domain.Entities;

// Bảng THƯA - chỉ có row khi user TẮT 1 loại thông báo cụ thể; vắng mặt = mặc định bật.
// Chỉ ảnh hưởng kênh push (xem TaskNotificationHelper.NotifyAsync), không ảnh hưởng việc ghi
// Notification trong-app.
public class NotificationPreference : BaseEntity
{
    public required Guid UserId { get; set; }
    public User? User { get; set; }

    public required string Type { get; set; }
}
```

- [ ] **Step 2: Thêm `DbSet` vào `IApplicationDbContext`**

Trong `backend/src/TaskMgmt.Application/Common/Interfaces/IApplicationDbContext.cs`, thêm dòng sau ngay dưới `DbSet<Notification> Notifications { get; }`:

```csharp
    DbSet<NotificationPreference> NotificationPreferences { get; }
```

- [ ] **Step 3: Thêm `DbSet` vào `AppDbContext`**

Trong `backend/src/TaskMgmt.Infrastructure/Persistence/AppDbContext.cs`, thêm dòng sau ngay dưới `public DbSet<Notification> Notifications => Set<Notification>();`:

```csharp
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
```

- [ ] **Step 4: Tạo EF configuration**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Infrastructure.Persistence.Configurations;

public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("NotificationPreferences");

        builder.Property(p => p.Type).HasMaxLength(50).IsRequired();

        // Không thể tắt cùng 1 loại 2 lần cho cùng 1 user.
        builder.HasIndex(p => new { p.UserId, p.Type }).IsUnique();

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Step 5: Build để xác nhận model hợp lệ**

Run: `cd backend && dotnet build TaskMgmt.slnx`
Expected: build thành công, không lỗi.

- [ ] **Step 6: Tạo migration**

```bash
cd backend/src/TaskMgmt.Infrastructure
dotnet ef migrations add AddNotificationPreferences --startup-project ../TaskMgmt.API
```
Expected: tạo file migration mới `<timestamp>_AddNotificationPreferences.cs` tạo bảng `NotificationPreferences` với FK tới `Users` (cascade delete) và unique index trên `(UserId, Type)`. Kiểm tra nội dung file migration sinh ra khớp với configuration ở Step 4.

- [ ] **Step 7: Commit**

```bash
cd d:/projects/K-app
git add backend/src/TaskMgmt.Domain/Entities/NotificationPreference.cs backend/src/TaskMgmt.Application/Common/Interfaces/IApplicationDbContext.cs backend/src/TaskMgmt.Infrastructure/Persistence/AppDbContext.cs backend/src/TaskMgmt.Infrastructure/Persistence/Configurations/NotificationPreferenceConfiguration.cs backend/src/TaskMgmt.Infrastructure/Persistence/Migrations/
git commit -m "feat: add NotificationPreference data model and migration"
```

---

### Task 2: `NotificationTypes` hằng số + cache key

**Files:**
- Create: `backend/src/TaskMgmt.Application/Features/Notifications/Common/NotificationTypes.cs`
- Modify: `backend/src/TaskMgmt.Application/Common/Caching/CacheKeys.cs`

**Interfaces:**
- Produces: `NotificationTypes.All : IReadOnlyList<string>` (10 loại). `CacheKeys.DisabledNotificationTypes(Guid userId) : string`, `CacheKeys.DisabledNotificationTypesExpiration : TimeSpan`. Task 3, 5 tiêu thụ.

- [ ] **Step 1: Tạo `NotificationTypes`**

Danh sách tổng hợp từ toàn bộ lời gọi `TaskNotificationHelper.NotifyAsync` hiện có (`WorkTaskNotificationEventHandlers`, `TaskAssigneeNotificationEventHandlers`, `CommentNotificationEventHandler`, `AttachmentNotificationEventHandler`, `SendDueSoonReminderJob`, `SendOverdueReminderJob`):

```csharp
namespace TaskMgmt.Application.Features.Notifications.Common;

// Nguồn sự thật duy nhất cho các giá trị hợp lệ của Notification.Type - dùng để validate PUT
// preferences và để GET preferences trả đủ 10 dòng kể cả loại chưa có row nào (mặc định bật).
// Thêm loại thông báo mới ở NotifyAsync call site nào cũng PHẢI cập nhật danh sách này, nếu
// không loại đó sẽ không xuất hiện trong màn cài đặt (coi như luôn bật, không tắt được).
public static class NotificationTypes
{
    public static readonly IReadOnlyList<string> All =
    [
        "FieldChanged",
        "StatusChanged",
        "Deleted",
        "AssigneeAdded",
        "AssigneeRemoved",
        "AssigneeRoleChanged",
        "CommentAdded",
        "AttachmentAdded",
        "DueSoon",
        "Overdue",
    ];
}
```

- [ ] **Step 2: Thêm cache key vào `CacheKeys`**

Trong `backend/src/TaskMgmt.Application/Common/Caching/CacheKeys.cs`, thêm ngay dưới dòng `public static readonly TimeSpan UnreadNotificationCountExpiration = TimeSpan.FromSeconds(30);`:

```csharp
    public static readonly TimeSpan DisabledNotificationTypesExpiration = TimeSpan.FromMinutes(5);
```

Và thêm ngay dưới `public static string UnreadNotificationCount(Guid userId) => $"notifications:unreadcount:{userId}";`:

```csharp
    public static string DisabledNotificationTypes(Guid userId) => $"notifications:disabledtypes:{userId}";
```

- [ ] **Step 3: Build để xác nhận không lỗi**

Run: `cd backend && dotnet build TaskMgmt.slnx`
Expected: build thành công.

- [ ] **Step 4: Commit**

```bash
cd d:/projects/K-app
git add backend/src/TaskMgmt.Application/Features/Notifications/Common/NotificationTypes.cs backend/src/TaskMgmt.Application/Common/Caching/CacheKeys.cs
git commit -m "feat: add NotificationTypes constant and disabled-types cache key"
```

---

### Task 3: `GetNotificationPreferencesQuery` + `UpdateNotificationPreferenceCommand`

**Files:**
- Create: `backend/src/TaskMgmt.Application/Features/Notifications/Common/NotificationPreferenceDto.cs`
- Create: `backend/src/TaskMgmt.Application/Features/Notifications/Queries/GetNotificationPreferences/GetNotificationPreferencesQuery.cs`
- Create: `backend/src/TaskMgmt.Application/Features/Notifications/Queries/GetNotificationPreferences/GetNotificationPreferencesQueryHandler.cs`
- Create: `backend/src/TaskMgmt.Application/Features/Notifications/Commands/UpdateNotificationPreference/UpdateNotificationPreferenceCommand.cs`
- Create: `backend/src/TaskMgmt.Application/Features/Notifications/Commands/UpdateNotificationPreference/UpdateNotificationPreferenceCommandHandler.cs`
- Create: `backend/src/TaskMgmt.Application/Features/Notifications/Commands/UpdateNotificationPreference/UpdateNotificationPreferenceCommandValidator.cs`
- Test: `backend/tests/TaskMgmt.Application.UnitTests/Features/Notifications/NotificationPreferenceTests.cs`

**Interfaces:**
- Consumes: `NotificationTypes.All`, `CacheKeys.DisabledNotificationTypes(Guid)` (Task 2), `IApplicationDbContext.NotificationPreferences` (Task 1).
- Produces: `GetNotificationPreferencesQuery : IRequest<List<NotificationPreferenceDto>>`, `NotificationPreferenceDto(string Type, bool IsEnabled)`, `UpdateNotificationPreferenceCommand(string Type, bool IsEnabled) : IRequest`. Task 4 (controller), Task 5 (`NotifyAsync` test) tiêu thụ.

GET (query, đọc) và PUT (command, ghi) làm cùng 1 task vì test của một bên cần type của bên kia để xác nhận đúng hành vi (đọc lại sau khi ghi) — tách rời sẽ để lại 1 task với build gãy chờ task kia, vi phạm nguyên tắc mỗi task tự review độc lập được.

- [ ] **Step 1: Viết test trước (toàn bộ, GET lẫn PUT)**

```csharp
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Features.Notifications.Commands.UpdateNotificationPreference;
using TaskMgmt.Application.Features.Notifications.Common;
using TaskMgmt.Application.Features.Notifications.Queries.GetNotificationPreferences;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Enums;
using TaskMgmt.Infrastructure.Persistence;

namespace TaskMgmt.Application.UnitTests.Features.Notifications;

public class NotificationPreferenceTests
{
    [Fact]
    public async Task GetPreferences_NoRowsDisabled_AllTenEnabled()
    {
        var userId = Guid.NewGuid();
        await using var provider = TestServiceProviderFactory.Create(userId, SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new GetNotificationPreferencesQuery());

        Assert.Equal(NotificationTypes.All.Count, result.Count);
        Assert.All(result, p => Assert.True(p.IsEnabled));
        Assert.Equal(NotificationTypes.All.OrderBy(t => t), result.Select(p => p.Type).OrderBy(t => t));
    }

    [Fact]
    public async Task GetPreferences_SomeDisabled_ReflectsState()
    {
        var userId = Guid.NewGuid();
        await using var provider = TestServiceProviderFactory.Create(userId, SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();

        await sender.Send(new UpdateNotificationPreferenceCommand("CommentAdded", false));

        var result = await sender.Send(new GetNotificationPreferencesQuery());

        Assert.False(result.Single(p => p.Type == "CommentAdded").IsEnabled);
        Assert.True(result.Single(p => p.Type == "DueSoon").IsEnabled);
    }

    [Fact]
    public async Task UpdatePreference_DisableTwice_Idempotent()
    {
        var userId = Guid.NewGuid();
        await using var provider = TestServiceProviderFactory.Create(userId, SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();
        var context = provider.GetRequiredService<AppDbContext>();

        await sender.Send(new UpdateNotificationPreferenceCommand("Overdue", false));
        await sender.Send(new UpdateNotificationPreferenceCommand("Overdue", false));

        Assert.Single(context.NotificationPreferences, p => p.UserId == userId && p.Type == "Overdue");
    }

    [Fact]
    public async Task UpdatePreference_DisableThenEnable_RemovesRow()
    {
        var userId = Guid.NewGuid();
        await using var provider = TestServiceProviderFactory.Create(userId, SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();
        var context = provider.GetRequiredService<AppDbContext>();

        await sender.Send(new UpdateNotificationPreferenceCommand("Overdue", false));
        await sender.Send(new UpdateNotificationPreferenceCommand("Overdue", true));

        Assert.Empty(context.NotificationPreferences.Where(p => p.UserId == userId && p.Type == "Overdue"));
    }

    [Fact]
    public async Task UpdatePreference_EnableWithoutExistingRow_Idempotent()
    {
        var userId = Guid.NewGuid();
        await using var provider = TestServiceProviderFactory.Create(userId, SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();

        await sender.Send(new UpdateNotificationPreferenceCommand("Overdue", true));

        var result = await sender.Send(new GetNotificationPreferencesQuery());
        Assert.True(result.Single(p => p.Type == "Overdue").IsEnabled);
    }

    [Fact]
    public async Task UpdatePreference_InvalidType_ThrowsValidationException()
    {
        var userId = Guid.NewGuid();
        await using var provider = TestServiceProviderFactory.Create(userId, SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();

        await Assert.ThrowsAsync<ValidationException>(
            () => sender.Send(new UpdateNotificationPreferenceCommand("KhongTonTai", false)));
    }
}
```

- [ ] **Step 2: Chạy test, xác nhận FAIL**

Run: `cd backend && dotnet test tests/TaskMgmt.Application.UnitTests --filter "FullyQualifiedName~NotificationPreferenceTests"`
Expected: FAIL với lỗi biên dịch — `GetNotificationPreferencesQuery`, `UpdateNotificationPreferenceCommand`, `NotificationPreferenceDto` chưa tồn tại.

- [ ] **Step 3: Tạo `NotificationPreferenceDto`**

```csharp
namespace TaskMgmt.Application.Features.Notifications.Common;

public record NotificationPreferenceDto(string Type, bool IsEnabled);
```

- [ ] **Step 4: Tạo `GetNotificationPreferencesQuery`**

```csharp
using MediatR;
using TaskMgmt.Application.Features.Notifications.Common;

namespace TaskMgmt.Application.Features.Notifications.Queries.GetNotificationPreferences;

public record GetNotificationPreferencesQuery : IRequest<List<NotificationPreferenceDto>>;
```

- [ ] **Step 5: Tạo handler cho query**

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Notifications.Common;

namespace TaskMgmt.Application.Features.Notifications.Queries.GetNotificationPreferences;

public class GetNotificationPreferencesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<GetNotificationPreferencesQuery, List<NotificationPreferenceDto>>
{
    public async Task<List<NotificationPreferenceDto>> Handle(
        GetNotificationPreferencesQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;

        var disabledTypes = await context.NotificationPreferences
            .Where(p => p.UserId == userId)
            .Select(p => p.Type)
            .ToListAsync(cancellationToken);

        return NotificationTypes.All
            .Select(type => new NotificationPreferenceDto(type, !disabledTypes.Contains(type)))
            .ToList();
    }
}
```

(Query này đọc thẳng DB, không qua cache-aside — đây không phải hot path như `NotifyAsync`, chỉ gọi khi user mở màn cài đặt.)

- [ ] **Step 6: Tạo `UpdateNotificationPreferenceCommand`**

```csharp
using MediatR;

namespace TaskMgmt.Application.Features.Notifications.Commands.UpdateNotificationPreference;

public record UpdateNotificationPreferenceCommand(string Type, bool IsEnabled) : IRequest;
```

- [ ] **Step 7: Tạo handler cho command**

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Caching;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.Notifications.Commands.UpdateNotificationPreference;

public class UpdateNotificationPreferenceCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser, ICacheService cache)
    : IRequestHandler<UpdateNotificationPreferenceCommand>
{
    public async Task Handle(UpdateNotificationPreferenceCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;

        var existing = await context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Type == request.Type, cancellationToken);

        if (request.IsEnabled)
        {
            // Bật lại = xoá row "đã tắt" nếu có. Đã bật sẵn (không có row) -> không làm gì, idempotent.
            if (existing is not null)
            {
                context.NotificationPreferences.Remove(existing);
            }
        }
        else if (existing is null)
        {
            // Tắt lần đầu = tạo row. Đã tắt sẵn -> không làm gì, idempotent.
            context.NotificationPreferences.Add(new NotificationPreference { UserId = userId, Type = request.Type });
        }

        await context.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync(CacheKeys.DisabledNotificationTypes(userId), cancellationToken);
    }
}
```

- [ ] **Step 8: Tạo validator**

```csharp
using FluentValidation;
using TaskMgmt.Application.Features.Notifications.Common;

namespace TaskMgmt.Application.Features.Notifications.Commands.UpdateNotificationPreference;

public class UpdateNotificationPreferenceCommandValidator : AbstractValidator<UpdateNotificationPreferenceCommand>
{
    public UpdateNotificationPreferenceCommandValidator()
    {
        RuleFor(x => x.Type)
            .Must(type => NotificationTypes.All.Contains(type))
            .WithMessage("Loại thông báo không hợp lệ.");
    }
}
```

- [ ] **Step 9: Chạy lại test, xác nhận PASS**

Run: `cd backend && dotnet test tests/TaskMgmt.Application.UnitTests --filter "FullyQualifiedName~NotificationPreferenceTests"`
Expected: PASS (6/6).

- [ ] **Step 10: Commit**

```bash
cd d:/projects/K-app
git add backend/src/TaskMgmt.Application/Features/Notifications/Common/NotificationPreferenceDto.cs backend/src/TaskMgmt.Application/Features/Notifications/Queries/GetNotificationPreferences/ backend/src/TaskMgmt.Application/Features/Notifications/Commands/UpdateNotificationPreference/ backend/tests/TaskMgmt.Application.UnitTests/Features/Notifications/NotificationPreferenceTests.cs
git commit -m "feat: add GetNotificationPreferencesQuery and UpdateNotificationPreferenceCommand"
```

---

### Task 4: Wire `NotificationsController`

**Files:**
- Modify: `backend/src/TaskMgmt.API/Controllers/NotificationsController.cs`

**Interfaces:**
- Consumes: `GetNotificationPreferencesQuery`, `UpdateNotificationPreferenceCommand` (Task 3).
- Produces: `GET api/v1/notifications/preferences`, `PUT api/v1/notifications/preferences/{type}`. Task 6 (mobile datasource) tiêu thụ qua HTTP.

- [ ] **Step 1: Thêm using**

Thêm vào đầu `backend/src/TaskMgmt.API/Controllers/NotificationsController.cs`, ngay dưới `using TaskMgmt.Application.Features.Notifications.Common;`:

```csharp
using TaskMgmt.Application.Features.Notifications.Commands.UpdateNotificationPreference;
using TaskMgmt.Application.Features.Notifications.Queries.GetNotificationPreferences;
```

- [ ] **Step 2: Thêm 2 action**

Thêm vào cuối class `NotificationsController`, ngay trước dấu `}` đóng class (sau `MarkAllAsRead`):

```csharp

    [HttpGet("preferences")]
    public async Task<ActionResult<List<NotificationPreferenceDto>>> GetPreferences(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetNotificationPreferencesQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("preferences/{type}")]
    public async Task<IActionResult> UpdatePreference(
        string type, UpdateNotificationPreferenceRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new UpdateNotificationPreferenceCommand(type, request.IsEnabled), cancellationToken);
        return NoContent();
    }
```

- [ ] **Step 3: Thêm request record**

Thêm vào cuối file, sau dấu `}` đóng class `NotificationsController` (mirror `ChangeTaskAssigneeRoleRequest` ở `TaskAssigneesController.cs`):

```csharp

public record UpdateNotificationPreferenceRequest(bool IsEnabled);
```

- [ ] **Step 4: Build**

Run: `cd backend && dotnet build TaskMgmt.slnx`
Expected: build thành công.

- [ ] **Step 5: Test thủ công qua Swagger**

Run: `cd backend/src/TaskMgmt.API && dotnet run`, mở `/swagger`.
Expected: 2 endpoint mới `GET /api/v1/notifications/preferences` và `PUT /api/v1/notifications/preferences/{type}` xuất hiện, gọi thử `GET` (cần Bearer token hợp lệ) trả về mảng 10 phần tử `{ type, isEnabled: true }`.

- [ ] **Step 6: Commit**

```bash
cd d:/projects/K-app
git add backend/src/TaskMgmt.API/Controllers/NotificationsController.cs
git commit -m "feat: wire notification preference endpoints"
```

---

### Task 5: `TaskNotificationHelper.NotifyAsync` tôn trọng preference

**Files:**
- Modify: `backend/src/TaskMgmt.Application/Features/Notifications/Common/TaskNotificationHelper.cs`
- Modify: `backend/tests/TaskMgmt.Application.UnitTests/Features/Notifications/NotificationTests.cs`

**Interfaces:**
- Consumes: `CacheKeys.DisabledNotificationTypes(Guid)`, `CacheKeys.DisabledNotificationTypesExpiration` (Task 2), `IApplicationDbContext.NotificationPreferences` (Task 1).
- Produces: hành vi mới của `NotifyAsync` — không thay đổi chữ ký (đã có sẵn `ICacheService cache` trong tham số).

- [ ] **Step 1: Viết test trước (thêm vào cuối class `NotificationTests`)**

```csharp

    [Fact]
    public async Task NotifyAsync_TypeDisabled_StillCreatesNotification_ButSkipsPush()
    {
        var actorId = Guid.NewGuid();
        await using var provider = TestServiceProviderFactory.Create(actorId, SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();
        var context = provider.GetRequiredService<AppDbContext>();
        var jobScheduler = (FakeBackgroundJobScheduler)provider.GetRequiredService<IBackgroundJobScheduler>();

        var task = await sender.Send(new CreateWorkTaskCommand("Task with comment", null, null, null, null));
        var otherUser = TestDataFactory.CreateUser("other2@example.com");
        context.Users.Add(otherUser);
        await context.SaveChangesAsync(default);
        await sender.Send(new AddTaskAssigneeCommand(task.Id, otherUser.Id, TaskAssigneeRole.Watcher));

        // otherUser tắt push cho CommentAdded - preference của actor không liên quan, phải đúng
        // preference của NGƯỜI NHẬN (otherUser), không phải người tạo bình luận (actor).
        context.NotificationPreferences.Add(
            new Domain.Entities.NotificationPreference { UserId = otherUser.Id, Type = "CommentAdded" });
        await context.SaveChangesAsync(default);

        await sender.Send(new CreateCommentCommand(task.Id, "Bình luận test.", []));

        var notification = Assert.Single(context.Notifications, n => n.Type == "CommentAdded");
        Assert.Equal(otherUser.Id, notification.UserId);
        Assert.DoesNotContain(jobScheduler.Enqueued, e => e.UserId == otherUser.Id);
    }

    [Fact]
    public async Task NotifyAsync_TypeNotDisabled_CreatesNotificationAndEnqueuesPush()
    {
        var actorId = Guid.NewGuid();
        await using var provider = TestServiceProviderFactory.Create(actorId, SystemRole.Member);
        var sender = provider.GetRequiredService<ISender>();
        var context = provider.GetRequiredService<AppDbContext>();
        var jobScheduler = (FakeBackgroundJobScheduler)provider.GetRequiredService<IBackgroundJobScheduler>();

        var task = await sender.Send(new CreateWorkTaskCommand("Task with comment", null, null, null, null));
        var otherUser = TestDataFactory.CreateUser("other3@example.com");
        context.Users.Add(otherUser);
        await context.SaveChangesAsync(default);
        await sender.Send(new AddTaskAssigneeCommand(task.Id, otherUser.Id, TaskAssigneeRole.Watcher));

        await sender.Send(new CreateCommentCommand(task.Id, "Bình luận test.", []));

        Assert.Single(context.Notifications, n => n.Type == "CommentAdded" && n.UserId == otherUser.Id);
        Assert.Contains(jobScheduler.Enqueued, e => e.UserId == otherUser.Id);
    }
```

Thêm using vào đầu `NotificationTests.cs` nếu chưa có: `using TaskMgmt.Application.Common.Interfaces;` (cho `IBackgroundJobScheduler`) và `using TaskMgmt.Application.UnitTests.Common;` (đã có sẵn, cho `FakeBackgroundJobScheduler` — class này `internal`, cùng project nên truy cập được).

- [ ] **Step 2: Chạy test, xác nhận FAIL**

Run: `cd backend && dotnet test tests/TaskMgmt.Application.UnitTests --filter "FullyQualifiedName~NotifyAsync_TypeDisabled"`
Expected: FAIL — `NotifyAsync_TypeDisabled_StillCreatesNotification_ButSkipsPush` thất bại vì push vẫn được enqueue (hành vi cũ chưa check preference); `NotifyAsync_TypeNotDisabled_...` PASS (hành vi cũ vốn đã đúng cho trường hợp không tắt gì).

- [ ] **Step 3: Sửa `NotifyAsync`**

Trong `backend/src/TaskMgmt.Application/Features/Notifications/Common/TaskNotificationHelper.cs`, thêm using và sửa method:

```csharp
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Caching;
using TaskMgmt.Application.Common.Extensions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Domain.Entities;
```

Thay toàn bộ method `NotifyAsync` bằng:

```csharp
    public static async Task NotifyAsync(
        IApplicationDbContext context, IBackgroundJobScheduler jobScheduler, ICacheService cache,
        Guid userId, string title, string body, Guid? workTaskId, string type, DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken)
    {
        context.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Body = body,
            Type = type,
            WorkTaskId = workTaskId,
            CreatedAtUtc = occurredAtUtc,
        });

        await cache.RemoveAsync(CacheKeys.UnreadNotificationCount(userId), cancellationToken);

        var disabledTypes = await cache.GetOrSetAsync(
            CacheKeys.DisabledNotificationTypes(userId),
            CacheKeys.DisabledNotificationTypesExpiration,
            () => context.NotificationPreferences.Where(p => p.UserId == userId).Select(p => p.Type).ToListAsync(cancellationToken),
            cancellationToken);

        if (disabledTypes.Contains(type))
        {
            return;
        }

        var data = new Dictionary<string, string> { ["type"] = type };
        if (workTaskId is not null)
        {
            data["workTaskId"] = workTaskId.Value.ToString();
        }

        jobScheduler.EnqueuePushNotification(userId, title, body, data);
    }
```

Lưu ý: dòng `context.Notifications.Add(...)` và `cache.RemoveAsync(UnreadNotificationCount...)` giữ nguyên VỊ TRÍ TRƯỚC check preference — đúng yêu cầu "vẫn ghi trong-app dù đã tắt push".

- [ ] **Step 4: Chạy lại test, xác nhận PASS**

Run: `cd backend && dotnet test tests/TaskMgmt.Application.UnitTests --filter "FullyQualifiedName~NotificationTests"`
Expected: PASS toàn bộ (8/8 - 6 test cũ + 2 test mới).

- [ ] **Step 5: Chạy toàn bộ test suite backend, xác nhận không phá vỡ gì khác**

Run: `cd backend && dotnet test TaskMgmt.slnx`
Expected: `Domain.UnitTests` + `Application.UnitTests` PASS toàn bộ. `API.IntegrationTests` vẫn FAIL 11/11 với lỗi "Connection string 'Postgres' not found" — đây là baseline có sẵn từ trước (cần Docker Postgres chạy, không liên quan tới thay đổi của task này), không phải regression.

- [ ] **Step 6: Commit**

```bash
cd d:/projects/K-app
git add backend/src/TaskMgmt.Application/Features/Notifications/Common/TaskNotificationHelper.cs backend/tests/TaskMgmt.Application.UnitTests/Features/Notifications/NotificationTests.cs
git commit -m "feat: skip push (keep in-app) when recipient disabled the notification type"
```

---

## Mobile

### Task 6: Domain entity + mở rộng `NotificationRepository`

**Files:**
- Create: `mobile/taskmgmt_app/lib/features/notifications/domain/entities/notification_preference.dart`
- Create: `mobile/taskmgmt_app/lib/features/notifications/data/models/notification_preference_model.dart`
- Modify: `mobile/taskmgmt_app/lib/features/notifications/domain/repositories/notification_repository.dart`
- Modify: `mobile/taskmgmt_app/lib/features/notifications/data/repositories/notification_repository_impl.dart`
- Modify: `mobile/taskmgmt_app/lib/features/notifications/data/datasources/notification_remote_data_source.dart`
- Modify: `mobile/taskmgmt_app/test/notification_center_test.dart`

**Interfaces:**
- Produces: `NotificationPreference { type, isEnabled }` (+ `label` getter tiếng Việt), `NotificationRepository.getPreferences() : Future<List<NotificationPreference>>`, `NotificationRepository.updatePreference(String type, bool isEnabled) : Future<void>`. Task 7 tiêu thụ.

- [ ] **Step 1: Tạo domain entity `NotificationPreference`**

```dart
class NotificationPreference {
  const NotificationPreference({required this.type, required this.isEnabled});

  final String type;
  final bool isEnabled;

  static const Map<String, String> _labels = {
    'FieldChanged': 'Thay đổi thông tin công việc',
    'StatusChanged': 'Đổi trạng thái công việc',
    'Deleted': 'Công việc bị xoá',
    'AssigneeAdded': 'Được thêm vào công việc',
    'AssigneeRemoved': 'Bị gỡ khỏi công việc',
    'AssigneeRoleChanged': 'Đổi vai trò trong công việc',
    'CommentAdded': 'Có bình luận mới',
    'AttachmentAdded': 'Có tệp đính kèm mới',
    'DueSoon': 'Sắp đến hạn',
    'Overdue': 'Quá hạn',
  };

  String get label => _labels[type] ?? type;

  NotificationPreference copyWith({bool? isEnabled}) =>
      NotificationPreference(type: type, isEnabled: isEnabled ?? this.isEnabled);
}
```

- [ ] **Step 2: Tạo `NotificationPreferenceModel`**

```dart
import '../../domain/entities/notification_preference.dart';

class NotificationPreferenceModel {
  const NotificationPreferenceModel({required this.type, required this.isEnabled});

  final String type;
  final bool isEnabled;

  factory NotificationPreferenceModel.fromJson(Map<String, dynamic> json) => NotificationPreferenceModel(
        type: json['type'] as String,
        isEnabled: json['isEnabled'] as bool,
      );

  NotificationPreference toDomain() => NotificationPreference(type: type, isEnabled: isEnabled);
}
```

- [ ] **Step 3: Thêm method vào `NotificationRemoteDataSource`**

Trong `notification_remote_data_source.dart`, thêm import `import '../models/notification_preference_model.dart';` và 2 method sau vào cuối class (sau `markAllAsRead`):

```dart

  Future<List<NotificationPreferenceModel>> getPreferences() async {
    try {
      final response = await _dio.get<List<dynamic>>('/notifications/preferences');
      return response.data!
          .map((json) => NotificationPreferenceModel.fromJson(json as Map<String, dynamic>))
          .toList();
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<void> updatePreference(String type, bool isEnabled) async {
    try {
      await _dio.put<void>('/notifications/preferences/$type', data: {'isEnabled': isEnabled});
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }
```

- [ ] **Step 4: Thêm method vào interface `NotificationRepository`**

Trong `notification_repository.dart`, thêm import `import '../entities/notification_preference.dart';` ở đầu file, và thêm 2 dòng sau vào cuối abstract class (sau `markAllAsRead`). Lưu ý tên entity là `NotificationPreference` (không có tiền tố `App`, khác với `AppNotification` đã có sẵn — đây là entity mới, không đụng entity cũ):

```dart

  Future<List<NotificationPreference>> getPreferences();

  Future<void> updatePreference(String type, bool isEnabled);
```

- [ ] **Step 5: Implement trong `NotificationRepositoryImpl`**

Trong `notification_repository_impl.dart`, thêm import `import '../../domain/entities/notification_preference.dart';` và 2 method vào cuối class:

```dart

  @override
  Future<List<NotificationPreference>> getPreferences() async {
    final models = await _remoteDataSource.getPreferences();
    return models.map((model) => model.toDomain()).toList();
  }

  @override
  Future<void> updatePreference(String type, bool isEnabled) =>
      _remoteDataSource.updatePreference(type, isEnabled);
```

- [ ] **Step 6: Cập nhật fake repository trong `test/notification_center_test.dart`**

`_FakeNotificationRepository` implement `NotificationRepository` nên phải thêm 2 method mới, nếu không file test này build lỗi. Thêm vào cuối class `_FakeNotificationRepository`:

```dart

  @override
  Future<List<NotificationPreference>> getPreferences() async => [];

  @override
  Future<void> updatePreference(String type, bool isEnabled) async {}
```

Thêm import ở đầu file: `import 'package:taskmgmt_app/features/notifications/domain/entities/notification_preference.dart';`.

- [ ] **Step 7: Phân tích tĩnh + chạy test cũ để xác nhận không phá vỡ**

Run: `cd mobile/taskmgmt_app && flutter analyze`
Expected: không lỗi/warning mới.

Run: `flutter test test/notification_center_test.dart`
Expected: PASS (4/4, không đổi so với trước).

- [ ] **Step 8: Commit**

```bash
cd d:/projects/K-app
git add mobile/taskmgmt_app/lib/features/notifications/domain/entities/notification_preference.dart mobile/taskmgmt_app/lib/features/notifications/data/models/notification_preference_model.dart mobile/taskmgmt_app/lib/features/notifications/domain/repositories/notification_repository.dart mobile/taskmgmt_app/lib/features/notifications/data/repositories/notification_repository_impl.dart mobile/taskmgmt_app/lib/features/notifications/data/datasources/notification_remote_data_source.dart mobile/taskmgmt_app/test/notification_center_test.dart
git commit -m "feat(mobile): add notification preference entity and repository calls"
```

---

### Task 7: `preferences_provider.dart`

**Files:**
- Create: `mobile/taskmgmt_app/lib/features/notifications/presentation/providers/preferences_provider.dart`

**Interfaces:**
- Consumes: `notificationRepositoryProvider` (đã có sẵn trong `notification_provider.dart`), `NotificationRepository.getPreferences()`/`updatePreference()` (Task 6).
- Produces: `notificationPreferencesProvider : AsyncNotifierProvider<NotificationPreferencesController, List<NotificationPreference>>`, `NotificationPreferencesController.toggle(String type, bool isEnabled) : Future<void>`. Task 8 tiêu thụ.

- [ ] **Step 1: Tạo provider**

```dart
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../domain/entities/notification_preference.dart';
import 'notification_provider.dart';

final notificationPreferencesProvider =
    AsyncNotifierProvider<NotificationPreferencesController, List<NotificationPreference>>(
        NotificationPreferencesController.new);

class NotificationPreferencesController extends AsyncNotifier<List<NotificationPreference>> {
  @override
  Future<List<NotificationPreference>> build() =>
      ref.read(notificationRepositoryProvider).getPreferences();

  // Optimistic: đổi UI ngay, gọi API, rollback nếu lỗi - tránh switch "đứng hình" chờ mạng.
  Future<void> toggle(String type, bool isEnabled) async {
    final previous = state;
    final current = state.valueOrNull;
    if (current == null) return;

    state = AsyncData([
      for (final pref in current)
        if (pref.type == type) pref.copyWith(isEnabled: isEnabled) else pref,
    ]);

    try {
      await ref.read(notificationRepositoryProvider).updatePreference(type, isEnabled);
    } catch (e) {
      state = previous;
      rethrow;
    }
  }
}
```

- [ ] **Step 2: Phân tích tĩnh**

Run: `cd mobile/taskmgmt_app && flutter analyze`
Expected: không lỗi/warning mới.

- [ ] **Step 3: Commit**

```bash
cd d:/projects/K-app
git add mobile/taskmgmt_app/lib/features/notifications/presentation/providers/preferences_provider.dart
git commit -m "feat(mobile): add notification preferences provider with optimistic toggle"
```

---

### Task 8: Màn hình `notification_preferences_screen.dart`

**Files:**
- Create: `mobile/taskmgmt_app/lib/features/notifications/presentation/screens/notification_preferences_screen.dart`
- Modify: `mobile/taskmgmt_app/lib/features/notifications/presentation/screens/notification_center_screen.dart`
- Modify: `mobile/taskmgmt_app/lib/core/routing/app_router.dart`
- Test: `mobile/taskmgmt_app/test/notification_preferences_test.dart`

**Interfaces:**
- Consumes: `notificationPreferencesProvider`, `NotificationPreferencesController.toggle` (Task 7), `NotificationPreference.label` (Task 6).

- [ ] **Step 1: Viết widget test trước**

```dart
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:taskmgmt_app/features/notifications/domain/entities/notification_preference.dart';
import 'package:taskmgmt_app/features/notifications/domain/entities/notification.dart';
import 'package:taskmgmt_app/features/notifications/domain/repositories/notification_repository.dart';
import 'package:taskmgmt_app/core/models/paged_result.dart';
import 'package:taskmgmt_app/features/notifications/presentation/providers/notification_provider.dart';
import 'package:taskmgmt_app/features/notifications/presentation/screens/notification_preferences_screen.dart';

class _FakePreferenceRepository implements NotificationRepository {
  final List<NotificationPreference> prefs = [
    const NotificationPreference(type: 'CommentAdded', isEnabled: true),
    const NotificationPreference(type: 'Overdue', isEnabled: false),
  ];
  bool throwOnUpdate = false;

  @override
  Future<List<NotificationPreference>> getPreferences() async => List.of(prefs);

  @override
  Future<void> updatePreference(String type, bool isEnabled) async {
    if (throwOnUpdate) {
      throw Exception('network error');
    }
    final index = prefs.indexWhere((p) => p.type == type);
    if (index != -1) prefs[index] = prefs[index].copyWith(isEnabled: isEnabled);
  }

  @override
  Future<PagedResult<AppNotification>> getNotifications({bool unreadOnly = false}) async =>
      const PagedResult(items: [], pageNumber: 1, pageSize: 50, totalCount: 0, totalPages: 0);

  @override
  Future<int> getUnreadCount() async => 0;

  @override
  Future<void> markAsRead(String id) async {}

  @override
  Future<void> markAllAsRead() async {}
}

Widget _buildScreen(_FakePreferenceRepository repo) => ProviderScope(
      overrides: [notificationRepositoryProvider.overrideWithValue(repo)],
      child: const MaterialApp(home: NotificationPreferencesScreen()),
    );

void main() {
  testWidgets('Shows a switch per preference with correct initial state', (tester) async {
    final repo = _FakePreferenceRepository();
    await tester.pumpWidget(_buildScreen(repo));
    await tester.pumpAndSettle();

    expect(find.text('Có bình luận mới'), findsOneWidget);
    expect(find.text('Quá hạn'), findsOneWidget);

    final commentSwitch = tester.widget<SwitchListTile>(
      find.widgetWithText(SwitchListTile, 'Có bình luận mới'),
    );
    expect(commentSwitch.value, isTrue);

    final overdueSwitch = tester.widget<SwitchListTile>(
      find.widgetWithText(SwitchListTile, 'Quá hạn'),
    );
    expect(overdueSwitch.value, isFalse);
  });

  testWidgets('Tapping a switch calls updatePreference and flips state', (tester) async {
    final repo = _FakePreferenceRepository();
    await tester.pumpWidget(_buildScreen(repo));
    await tester.pumpAndSettle();

    await tester.tap(find.widgetWithText(SwitchListTile, 'Quá hạn'));
    await tester.pumpAndSettle();

    expect(repo.prefs.firstWhere((p) => p.type == 'Overdue').isEnabled, isTrue);
    final overdueSwitch = tester.widget<SwitchListTile>(
      find.widgetWithText(SwitchListTile, 'Quá hạn'),
    );
    expect(overdueSwitch.value, isTrue);
  });

  testWidgets('API error rolls the switch back and shows a snackbar', (tester) async {
    final repo = _FakePreferenceRepository()..throwOnUpdate = true;
    await tester.pumpWidget(_buildScreen(repo));
    await tester.pumpAndSettle();

    await tester.tap(find.widgetWithText(SwitchListTile, 'Quá hạn'));
    await tester.pumpAndSettle();

    final overdueSwitch = tester.widget<SwitchListTile>(
      find.widgetWithText(SwitchListTile, 'Quá hạn'),
    );
    expect(overdueSwitch.value, isFalse);
    expect(find.byType(SnackBar), findsOneWidget);
  });
}
```

- [ ] **Step 2: Chạy test, xác nhận FAIL**

Run: `cd mobile/taskmgmt_app && flutter test test/notification_preferences_test.dart`
Expected: FAIL — `NotificationPreferencesScreen` chưa tồn tại.

- [ ] **Step 3: Tạo màn hình**

```dart
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../shared/widgets/error_state_view.dart';
import '../providers/preferences_provider.dart';

class NotificationPreferencesScreen extends ConsumerWidget {
  const NotificationPreferencesScreen({super.key});

  static const path = '/notifications/preferences';
  static const name = 'notification-preferences';

  Future<void> _handleToggle(BuildContext context, WidgetRef ref, String type, bool value) async {
    try {
      await ref.read(notificationPreferencesProvider.notifier).toggle(type, value);
    } catch (e) {
      if (!context.mounted) return;
      final message = e is ApiException ? e.message : 'Không thể cập nhật cài đặt.';
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final preferencesAsync = ref.watch(notificationPreferencesProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Cài đặt thông báo')),
      body: preferencesAsync.when(
        data: (preferences) => ListView(
          children: [
            for (final pref in preferences)
              SwitchListTile(
                title: Text(pref.label),
                value: pref.isEnabled,
                onChanged: (value) => _handleToggle(context, ref, pref.type, value),
              ),
          ],
        ),
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => ErrorStateView(
          message: error is ApiException ? error.message : 'Không thể tải cài đặt thông báo.',
          onRetry: () => ref.invalidate(notificationPreferencesProvider),
        ),
      ),
    );
  }
}
```

- [ ] **Step 4: Chạy lại test, xác nhận PASS**

Run: `cd mobile/taskmgmt_app && flutter test test/notification_preferences_test.dart`
Expected: PASS (3/3).

- [ ] **Step 5: Thêm icon settings vào `NotificationCenterScreen`**

Trong `notification_center_screen.dart`, thêm import `import 'notification_preferences_screen.dart';` và thêm `IconButton` sau vào `actions` (sau nút "Đánh dấu tất cả đã đọc"):

```dart
          IconButton(
            icon: const Icon(Icons.settings_outlined),
            tooltip: 'Cài đặt thông báo',
            onPressed: () => context.push(NotificationPreferencesScreen.path),
          ),
```

- [ ] **Step 6: Đăng ký route**

Trong `app_router.dart`, thêm import `import '../../features/notifications/presentation/screens/notification_preferences_screen.dart';` và thêm route sau vào mảng `routes`, ngay sau route của `NotificationCenterScreen`:

```dart
      GoRoute(
        path: NotificationPreferencesScreen.path,
        name: NotificationPreferencesScreen.name,
        builder: (context, state) => const NotificationPreferencesScreen(),
      ),
```

- [ ] **Step 7: Phân tích tĩnh + chạy toàn bộ test mobile**

Run: `cd mobile/taskmgmt_app && flutter analyze`
Expected: không lỗi/warning mới.

Run: `flutter test`
Expected: PASS toàn bộ (không phá vỡ test cũ).

- [ ] **Step 8: Commit**

```bash
cd d:/projects/K-app
git add mobile/taskmgmt_app/lib/features/notifications/presentation/screens/notification_preferences_screen.dart mobile/taskmgmt_app/lib/features/notifications/presentation/screens/notification_center_screen.dart mobile/taskmgmt_app/lib/core/routing/app_router.dart mobile/taskmgmt_app/test/notification_preferences_test.dart
git commit -m "feat(mobile): add notification preferences screen"
```

---

## Self-Review Notes

- **Spec coverage:** Task 1 → data model (spec §4); Task 2 → hằng số `NotificationTypes` + cache key (spec §3, §4); Task 3 → `GET`/`PUT /notifications/preferences` gộp chung vì test 2 chiều phụ thuộc lẫn nhau (spec §5); Task 4 → wire controller; Task 5 → sửa `NotifyAsync` giữ in-app/chặn push (spec §3, §5); Task 6, 7, 8 → toàn bộ mobile (spec §6), gồm cả việc sửa `_FakeNotificationRepository` hiện có mà spec không nêu rõ nhưng bắt buộc để không phá vỡ build (phát hiện khi rà lại `notification_center_test.dart`). Test backend + mobile ở spec §7 đều có task tương ứng (Task 3, 5 cho backend; Task 8 cho mobile widget test).
- **Placeholder scan:** không còn "TBD"/"implement later" — mọi step đều có code đầy đủ, kể cả 3 file test lớn.
- **Type consistency:** đã đối chiếu chữ ký thật `TaskNotificationHelper.NotifyAsync`, `ICacheService.GetOrSetAsync`, `IBackgroundJobScheduler.EnqueuePushNotification`, `NotificationRepository`/`NotificationRepositoryImpl`/`NotificationRemoteDataSource` với source hiện có trong repo trước khi viết plan (không suy đoán). `NotificationPreference` (entity mới) và `NotificationPreferenceDto`/`NotificationPreferenceModel` giữ nhất quán field `type`/`isEnabled` xuyên suốt Task 3–8. Task 7 `toggle(type, isEnabled)` khớp đúng tên tham số Task 8 gọi.
- **Task-splitting fix (pre-flight, trước khi dispatch Task 1):** bản gốc tách Task 3 (GET) và Task 4 (PUT) riêng, để lại build gãy có chủ đích giữa 2 task — vi phạm nguyên tắc "mỗi task tự review độc lập được". Đã gộp lại thành 1 Task 3 duy nhất (8 task tổng thay vì 9), xác nhận với người dùng trước khi thực thi.
