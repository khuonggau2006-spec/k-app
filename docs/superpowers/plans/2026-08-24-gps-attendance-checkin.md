# GPS Attendance Check-in Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let a user check-in/check-out once per day with GPS, validated server-side against any active `Location`'s allowed radius, with a monthly history + simple stats view.

**Architecture:** New backend module `Attendance` (CQRS via MediatR, mirrors the existing `Locations` module exactly) with a new `AttendanceRecord` entity and a pure `GeoDistance` Haversine helper for server-side radius validation. New mobile feature `attendance/` (data/domain/presentation, mirrors `locations/`) using `geolocator` to read device GPS only at button-press time (no continuous location watching).

**Tech Stack:** Backend: .NET 10, EF Core/Npgsql, MediatR, FluentValidation (all existing). Mobile: Flutter/Riverpod/Dio (existing) + `geolocator` (new).

**Spec:** `docs/superpowers/specs/2026-08-24-gps-attendance-checkin-design.md`

## Global Constraints

- 1 `AttendanceRecord` per user per calendar day (`WorkDate`), unique on `(UserId, WorkDate)`.
- `WorkDate` is computed in Vietnam time (UTC+7), never raw UTC date: `DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime)`.
- Check-in is valid if within `CheckInRadiusMeters` (default 100m) of **any** active `Location` — no per-user Location assignment.
- Check-out does not block on radius (only requires an open check-in today); it still records a best-effort matched location if one is in range, purely for display.
- No shift/schedule, no late/absent/leave/holiday tracking, no manager/admin view of other users' attendance, no approval workflow — out of scope per spec §2.
- All new user-facing strings are Vietnamese.
- `--concurrency=1` when running `flutter test` in this environment.

---

### Task 1: Backend schema — `AttendanceRecord` entity, `Location.CheckInRadiusMeters`, migration

**Files:**
- Create: `backend/src/TaskMgmt.Domain/Entities/AttendanceRecord.cs`
- Modify: `backend/src/TaskMgmt.Domain/Entities/Location.cs`
- Modify: `backend/src/TaskMgmt.Application/Common/Interfaces/IApplicationDbContext.cs`
- Modify: `backend/src/TaskMgmt.Infrastructure/Persistence/AppDbContext.cs`
- Create: `backend/src/TaskMgmt.Infrastructure/Persistence/Configurations/AttendanceRecordConfiguration.cs`
- Create (generated): new files under `backend/src/TaskMgmt.Infrastructure/Persistence/Migrations/`

**Interfaces:**
- Produces: `AttendanceRecord` (fields below), `context.AttendanceRecords : DbSet<AttendanceRecord>`, `Location.CheckInRadiusMeters : double`. All later backend tasks depend on this.

This is a schema-only task — no business logic yet, so no new unit test. Verified by a clean build, a successfully generated migration, and the full existing test suite still passing.

- [ ] **Step 1: Add the `AttendanceRecord` entity**

Create `backend/src/TaskMgmt.Domain/Entities/AttendanceRecord.cs`:

```csharp
using TaskMgmt.Domain.Common;

namespace TaskMgmt.Domain.Entities;

public class AttendanceRecord : AuditableEntity
{
    public required Guid UserId { get; set; }
    public User? User { get; set; }
    public required DateOnly WorkDate { get; set; }
    public DateTimeOffset? CheckInAtUtc { get; set; }
    public double? CheckInLatitude { get; set; }
    public double? CheckInLongitude { get; set; }
    public Guid? CheckInLocationId { get; set; }
    public Location? CheckInLocation { get; set; }
    public DateTimeOffset? CheckOutAtUtc { get; set; }
    public double? CheckOutLatitude { get; set; }
    public double? CheckOutLongitude { get; set; }
    public Guid? CheckOutLocationId { get; set; }
    public Location? CheckOutLocation { get; set; }
}
```

- [ ] **Step 2: Add `CheckInRadiusMeters` to `Location`**

In `backend/src/TaskMgmt.Domain/Entities/Location.cs`, add this property right after `IsActive`:

```csharp
    public double CheckInRadiusMeters { get; set; } = 100;
```

- [ ] **Step 3: Register the new `DbSet` on the interface**

In `backend/src/TaskMgmt.Application/Common/Interfaces/IApplicationDbContext.cs`, add after `DbSet<NotificationPreference> NotificationPreferences { get; }`:

```csharp
    DbSet<AttendanceRecord> AttendanceRecords { get; }
```

- [ ] **Step 4: Register the new `DbSet` on `AppDbContext`**

In `backend/src/TaskMgmt.Infrastructure/Persistence/AppDbContext.cs`, add after the `NotificationPreferences` property:

```csharp
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
```

- [ ] **Step 5: Add the EF Core configuration**

Create `backend/src/TaskMgmt.Infrastructure/Persistence/Configurations/AttendanceRecordConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Infrastructure.Persistence.Configurations;

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("AttendanceRecords");

        builder.HasIndex(a => new { a.UserId, a.WorkDate }).IsUnique();

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.CheckInLocation)
            .WithMany()
            .HasForeignKey(a => a.CheckInLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.CheckOutLocation)
            .WithMany()
            .HasForeignKey(a => a.CheckOutLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Domain.Entities.User>()
            .WithMany()
            .HasForeignKey(a => a.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Domain.Entities.User>()
            .WithMany()
            .HasForeignKey(a => a.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

- [ ] **Step 6: Generate the EF Core migration**

Run:
```bash
cd backend/src/TaskMgmt.Infrastructure
dotnet ef migrations add AddAttendanceRecords --startup-project ../TaskMgmt.API
```
Expected: creates `<timestamp>_AddAttendanceRecords.cs` + `.Designer.cs` under `Persistence/Migrations/`, and updates `AppDbContextModelSnapshot.cs`. This does not require a live database connection — EF builds the migration from the model, not from an actual DB. Do not hand-write migration files.

- [ ] **Step 7: Verify the build and existing tests are unaffected**

Run:
```bash
cd backend
dotnet build TaskMgmt.slnx
dotnet test TaskMgmt.slnx
```
Expected: build succeeds, all existing tests still pass (no new tests yet — this task adds schema only).

- [ ] **Step 8: Commit**

```bash
git add backend/src/TaskMgmt.Domain/Entities/AttendanceRecord.cs backend/src/TaskMgmt.Domain/Entities/Location.cs backend/src/TaskMgmt.Application/Common/Interfaces/IApplicationDbContext.cs backend/src/TaskMgmt.Infrastructure/Persistence/AppDbContext.cs backend/src/TaskMgmt.Infrastructure/Persistence/Configurations/AttendanceRecordConfiguration.cs backend/src/TaskMgmt.Infrastructure/Persistence/Migrations/
git commit -m "feat(backend): add AttendanceRecord schema and Location.CheckInRadiusMeters"
```

---

### Task 2: Expose `CheckInRadiusMeters` on Location's read/update API

**Files:**
- Modify: `backend/src/TaskMgmt.Application/Features/Locations/Common/LocationDto.cs`
- Modify: `backend/src/TaskMgmt.Application/Features/Locations/Commands/UpdateLocation/UpdateLocationCommand.cs`
- Modify: `backend/src/TaskMgmt.Application/Features/Locations/Commands/UpdateLocation/UpdateLocationCommandHandler.cs`
- Modify: `backend/src/TaskMgmt.Application/Features/Locations/Commands/UpdateLocation/UpdateLocationCommandValidator.cs`
- Modify: `backend/src/TaskMgmt.Application/Features/Locations/Queries/GetLocations/GetLocationsQueryHandler.cs`
- Test: `backend/tests/TaskMgmt.Application.UnitTests/Features/Locations/UpdateLocationCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `Location.CheckInRadiusMeters` (Task 1).
- Produces: `LocationDto.CheckInRadiusMeters : double` — Task 4's `CheckInCommandHandler` reads `Location.CheckInRadiusMeters` directly from the entity (not the DTO), so this task is only about exposing the field for editing/display; it does not block Task 4.

`CreateLocationCommand` intentionally stays unchanged — new locations get the entity's default (100m); `CheckInRadiusMeters` is only adjustable afterwards via update, keeping this task small.

- [ ] **Step 1: Add `CheckInRadiusMeters` to `LocationDto`**

Replace the full content of `backend/src/TaskMgmt.Application/Features/Locations/Common/LocationDto.cs`:

```csharp
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.Locations.Common;

public record LocationDto(
    Guid Id,
    string Name,
    string? Address,
    double Latitude,
    double Longitude,
    double CheckInRadiusMeters,
    bool IsActive,
    Guid? ParentLocationId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc)
{
    public static LocationDto FromEntity(Location location) => new(
        location.Id,
        location.Name,
        location.Address,
        location.Latitude,
        location.Longitude,
        location.CheckInRadiusMeters,
        location.IsActive,
        location.ParentLocationId,
        location.CreatedAtUtc,
        location.UpdatedAtUtc);
}
```

- [ ] **Step 2: Update the manual projection in `GetLocationsQueryHandler`**

In `backend/src/TaskMgmt.Application/Features/Locations/Queries/GetLocations/GetLocationsQueryHandler.cs`, replace the `.Select(...)` call:

```csharp
            .Select(l => new LocationDto(
                l.Id,
                l.Name,
                l.Address,
                l.Latitude,
                l.Longitude,
                l.CheckInRadiusMeters,
                l.IsActive,
                l.ParentLocationId,
                l.CreatedAtUtc,
                l.UpdatedAtUtc))
```

- [ ] **Step 3: Add `CheckInRadiusMeters` to `UpdateLocationCommand`**

Replace `backend/src/TaskMgmt.Application/Features/Locations/Commands/UpdateLocation/UpdateLocationCommand.cs`:

```csharp
using MediatR;
using TaskMgmt.Application.Features.Locations.Common;

namespace TaskMgmt.Application.Features.Locations.Commands.UpdateLocation;

public record UpdateLocationCommand(
    Guid Id,
    string Name,
    string? Address,
    double Latitude,
    double Longitude,
    double CheckInRadiusMeters,
    bool IsActive,
    Guid? ParentLocationId) : IRequest<LocationDto>;
```

- [ ] **Step 4: Persist it in the handler**

In `backend/src/TaskMgmt.Application/Features/Locations/Commands/UpdateLocation/UpdateLocationCommandHandler.cs`, add this line in `Handle`, right after `location.Longitude = request.Longitude;`:

```csharp
        location.CheckInRadiusMeters = request.CheckInRadiusMeters;
```

- [ ] **Step 5: Validate it**

In `backend/src/TaskMgmt.Application/Features/Locations/Commands/UpdateLocation/UpdateLocationCommandValidator.cs`, add this rule after the `Longitude` rule:

```csharp
        RuleFor(x => x.CheckInRadiusMeters)
            .GreaterThan(0);
```

- [ ] **Step 6: Fix any other positional constructions that broke**

Both `UpdateLocationCommand` and `LocationDto` are positional records that just gained a new required parameter — this breaks any existing code that constructs them positionally. Search for other call sites:

```bash
cd backend
grep -rn "new UpdateLocationCommand(\|LocationDto(" tests/ src/TaskMgmt.API/Controllers/
```

For every match outside the files already edited in this task, insert `100` (a reasonable test/default radius value) as the `CheckInRadiusMeters` argument, in the same position it occupies in the record definition above (right after `Longitude`, before `IsActive`).

- [ ] **Step 7: Add a test for the new field**

Check whether `backend/tests/TaskMgmt.Application.UnitTests/Features/Locations/UpdateLocationCommandHandlerTests.cs` already exists. If it does, add this test method to the existing class. If it does not, create the file with this content:

```csharp
using TaskMgmt.Application.Features.Locations.Commands.UpdateLocation;
using TaskMgmt.Application.UnitTests.Common;

namespace TaskMgmt.Application.UnitTests.Features.Locations;

public class UpdateLocationCommandHandlerTests
{
    [Fact]
    public async Task Handle_UpdatesCheckInRadiusMeters()
    {
        using var context = TestDbContextFactory.Create();
        var location = TestDataFactory.CreateLocation();
        context.Locations.Add(location);
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(Guid.NewGuid(), TaskMgmt.Domain.Enums.SystemRole.Manager);
        var handler = new UpdateLocationCommandHandler(context, currentUser, new FakeCacheService());

        var result = await handler.Handle(
            new UpdateLocationCommand(location.Id, location.Name, location.Address, location.Latitude, location.Longitude, 250, true, null),
            default);

        Assert.Equal(250, result.CheckInRadiusMeters);
        var saved = await context.Locations.FindAsync(location.Id);
        Assert.Equal(250, saved!.CheckInRadiusMeters);
    }
}
```

If a `FakeCacheService` constructor signature differs from a bare `new FakeCacheService()`, adjust to match how it's constructed elsewhere in the test project (check another handler test that takes `ICacheService`, e.g. search `grep -rn "FakeCacheService" backend/tests/`).

- [ ] **Step 8: Run tests**

Run: `cd backend && dotnet test TaskMgmt.slnx`
Expected: PASS, no regressions.

- [ ] **Step 9: Commit**

```bash
git add backend/src/TaskMgmt.Application/Features/Locations/ backend/tests/TaskMgmt.Application.UnitTests/Features/Locations/
git commit -m "feat(backend): expose CheckInRadiusMeters on Location read/update API"
```

---

### Task 3: `GeoDistance` — Haversine distance utility

**Files:**
- Create: `backend/src/TaskMgmt.Application/Common/GeoDistance.cs`
- Test: `backend/tests/TaskMgmt.Application.UnitTests/Common/GeoDistanceTests.cs`

**Interfaces:**
- Produces: `static double GeoDistance.CalculateMeters(double lat1, double lng1, double lat2, double lng2)` — Task 4 and Task 5's handlers call this directly.
- Consumes: nothing (leaf pure-math module).

- [ ] **Step 1: Write the failing tests**

Create `backend/tests/TaskMgmt.Application.UnitTests/Common/GeoDistanceTests.cs`:

```csharp
using TaskMgmt.Application.Common;

namespace TaskMgmt.Application.UnitTests.Common;

public class GeoDistanceTests
{
    [Fact]
    public void CalculateMeters_SameCoordinates_ReturnsZero()
    {
        var distance = GeoDistance.CalculateMeters(10.0, 106.0, 10.0, 106.0);

        Assert.Equal(0, distance, precision: 6);
    }

    [Fact]
    public void CalculateMeters_KnownDistance_ReturnsApproximatelyCorrectValue()
    {
        // Hồ Gươm (21.0285, 105.8542) và Sân bay Nội Bài (21.2187, 105.8048) - Haversine tính ra ~21.76km.
        var distance = GeoDistance.CalculateMeters(21.0285, 105.8542, 21.2187, 105.8048);

        Assert.InRange(distance, 21_000, 22_500);
    }

    [Fact]
    public void CalculateMeters_OneHundredMetersApart_ReturnsApproximatelyOneHundred()
    {
        // 0.0009 độ vĩ độ xấp xỉ 100m (1 độ vĩ độ ~ 111.320m).
        var distance = GeoDistance.CalculateMeters(10.0, 106.0, 10.0009, 106.0);

        Assert.InRange(distance, 95, 105);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd backend && dotnet test TaskMgmt.slnx --filter "FullyQualifiedName~GeoDistanceTests"`
Expected: FAIL — `GeoDistance` does not exist yet (compile error).

- [ ] **Step 3: Implement `GeoDistance`**

Create `backend/src/TaskMgmt.Application/Common/GeoDistance.cs`:

```csharp
namespace TaskMgmt.Application.Common;

public static class GeoDistance
{
    private const double EarthRadiusMeters = 6_371_000;

    public static double CalculateMeters(double lat1, double lng1, double lat2, double lng2)
    {
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLng = DegreesToRadians(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd backend && dotnet test TaskMgmt.slnx --filter "FullyQualifiedName~GeoDistanceTests"`
Expected: PASS (3 tests).

- [ ] **Step 5: Commit**

```bash
git add backend/src/TaskMgmt.Application/Common/GeoDistance.cs backend/tests/TaskMgmt.Application.UnitTests/Common/GeoDistanceTests.cs
git commit -m "feat(backend): add GeoDistance Haversine utility"
```

---

### Task 4: `CheckInCommand`

**Files:**
- Create: `backend/src/TaskMgmt.Application/Features/Attendance/Common/AttendanceRecordDto.cs`
- Create: `backend/src/TaskMgmt.Application/Features/Attendance/Commands/CheckIn/CheckInCommand.cs`
- Create: `backend/src/TaskMgmt.Application/Features/Attendance/Commands/CheckIn/CheckInCommandValidator.cs`
- Create: `backend/src/TaskMgmt.Application/Features/Attendance/Commands/CheckIn/CheckInCommandHandler.cs`
- Test: `backend/tests/TaskMgmt.Application.UnitTests/Features/Attendance/CheckInCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `AttendanceRecord`, `context.AttendanceRecords` (Task 1); `Location.CheckInRadiusMeters` (Task 1); `GeoDistance.CalculateMeters` (Task 3).
- Produces: `AttendanceRecordDto(Guid Id, DateOnly WorkDate, DateTimeOffset? CheckInAtUtc, string? CheckInLocationName, DateTimeOffset? CheckOutAtUtc, string? CheckOutLocationName)` — Task 5 and Task 6 reuse this exact record. `CheckInCommand(double Latitude, double Longitude) : IRequest<AttendanceRecordDto>` — Task 7's controller calls this.

- [ ] **Step 1: Add the shared DTO**

Create `backend/src/TaskMgmt.Application/Features/Attendance/Common/AttendanceRecordDto.cs`:

```csharp
namespace TaskMgmt.Application.Features.Attendance.Common;

public record AttendanceRecordDto(
    Guid Id,
    DateOnly WorkDate,
    DateTimeOffset? CheckInAtUtc,
    string? CheckInLocationName,
    DateTimeOffset? CheckOutAtUtc,
    string? CheckOutLocationName);
```

- [ ] **Step 2: Write the failing tests**

Create `backend/tests/TaskMgmt.Application.UnitTests/Features/Attendance/CheckInCommandHandlerTests.cs`:

```csharp
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Features.Attendance.Commands.CheckIn;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Entities;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.UnitTests.Features.Attendance;

public class CheckInCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithinRadius_CreatesRecordWithMatchedLocation()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        var location = TestDataFactory.CreateLocation();
        location.CheckInRadiusMeters = 100;
        context.Users.Add(user);
        context.Locations.Add(location);
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(user.Id, SystemRole.Member);
        var handler = new CheckInCommandHandler(context, currentUser);

        var result = await handler.Handle(new CheckInCommand(location.Latitude, location.Longitude), default);

        Assert.NotNull(result.CheckInAtUtc);
        Assert.Equal(location.Name, result.CheckInLocationName);
        var saved = await context.AttendanceRecords.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal(location.Id, saved!.CheckInLocationId);
    }

    [Fact]
    public async Task Handle_OutsideEveryLocationRadius_ThrowsValidationException()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        var location = TestDataFactory.CreateLocation();
        location.CheckInRadiusMeters = 100;
        context.Users.Add(user);
        context.Locations.Add(location);
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(user.Id, SystemRole.Member);
        var handler = new CheckInCommandHandler(context, currentUser);

        // Cách location ~11km (0.1 độ vĩ độ), vượt xa bán kính 100m cho phép.
        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new CheckInCommand(location.Latitude + 0.1, location.Longitude), default));
    }

    [Fact]
    public async Task Handle_IgnoresInactiveLocations()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        var location = TestDataFactory.CreateLocation();
        location.CheckInRadiusMeters = 100;
        location.IsActive = false;
        context.Users.Add(user);
        context.Locations.Add(location);
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(user.Id, SystemRole.Member);
        var handler = new CheckInCommandHandler(context, currentUser);

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new CheckInCommand(location.Latitude, location.Longitude), default));
    }

    [Fact]
    public async Task Validator_AlreadyCheckedInToday_IsInvalid()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        var workDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
        context.Users.Add(user);
        context.AttendanceRecords.Add(new AttendanceRecord
        {
            UserId = user.Id,
            WorkDate = workDate,
            CheckInAtUtc = DateTimeOffset.UtcNow,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(user.Id, SystemRole.Member);
        var validator = new CheckInCommandValidator(context, currentUser);

        var result = await validator.ValidateAsync(new CheckInCommand(10.0, 106.0), default);

        Assert.False(result.IsValid);
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `cd backend && dotnet test TaskMgmt.slnx --filter "FullyQualifiedName~CheckInCommandHandlerTests"`
Expected: FAIL — `CheckInCommand`/`CheckInCommandHandler`/`CheckInCommandValidator` don't exist yet.

- [ ] **Step 4: Implement the command**

Create `backend/src/TaskMgmt.Application/Features/Attendance/Commands/CheckIn/CheckInCommand.cs`:

```csharp
using MediatR;
using TaskMgmt.Application.Features.Attendance.Common;

namespace TaskMgmt.Application.Features.Attendance.Commands.CheckIn;

public record CheckInCommand(double Latitude, double Longitude) : IRequest<AttendanceRecordDto>;
```

- [ ] **Step 5: Implement the validator**

Create `backend/src/TaskMgmt.Application/Features/Attendance/Commands/CheckIn/CheckInCommandValidator.cs`:

```csharp
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;

namespace TaskMgmt.Application.Features.Attendance.Commands.CheckIn;

public class CheckInCommandValidator : AbstractValidator<CheckInCommand>
{
    public CheckInCommandValidator(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);

        RuleFor(x => x)
            .MustAsync(async (_, cancellationToken) =>
            {
                var workDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
                var alreadyCheckedIn = await context.AttendanceRecords.AnyAsync(
                    a => a.UserId == currentUser.UserId && a.WorkDate == workDate && a.CheckInAtUtc != null,
                    cancellationToken);
                return !alreadyCheckedIn;
            })
            .WithMessage("Bạn đã check-in hôm nay rồi.")
            .WithName("CheckIn");
    }
}
```

- [ ] **Step 6: Implement the handler**

Create `backend/src/TaskMgmt.Application/Features/Attendance/Commands/CheckIn/CheckInCommandHandler.cs`:

```csharp
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common;
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Attendance.Common;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.Attendance.Commands.CheckIn;

public class CheckInCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<CheckInCommand, AttendanceRecordDto>
{
    public async Task<AttendanceRecordDto> Handle(CheckInCommand request, CancellationToken cancellationToken)
    {
        var activeLocations = await context.Locations
            .Where(l => l.IsActive)
            .ToListAsync(cancellationToken);

        var matchedLocation = activeLocations.FirstOrDefault(l =>
            GeoDistance.CalculateMeters(request.Latitude, request.Longitude, l.Latitude, l.Longitude) <= l.CheckInRadiusMeters);

        if (matchedLocation is null)
        {
            throw new ValidationException(
                [new ValidationFailure(nameof(CheckInCommand.Latitude), "Ngoài phạm vi cho phép của mọi vị trí đã đăng ký.")]);
        }

        var now = DateTimeOffset.UtcNow;
        var workDate = DateOnly.FromDateTime(now.ToOffset(TimeSpan.FromHours(7)).DateTime);

        var record = new AttendanceRecord
        {
            UserId = currentUser.UserId!.Value,
            WorkDate = workDate,
            CheckInAtUtc = now,
            CheckInLatitude = request.Latitude,
            CheckInLongitude = request.Longitude,
            CheckInLocationId = matchedLocation.Id,
            CreatedAtUtc = now,
            CreatedByUserId = currentUser.UserId,
        };

        context.AttendanceRecords.Add(record);
        await context.SaveChangesAsync(cancellationToken);

        return new AttendanceRecordDto(record.Id, record.WorkDate, record.CheckInAtUtc, matchedLocation.Name, null, null);
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

Run: `cd backend && dotnet test TaskMgmt.slnx --filter "FullyQualifiedName~CheckInCommandHandlerTests"`
Expected: PASS (4 tests).

- [ ] **Step 8: Commit**

```bash
git add backend/src/TaskMgmt.Application/Features/Attendance/ backend/tests/TaskMgmt.Application.UnitTests/Features/Attendance/CheckInCommandHandlerTests.cs
git commit -m "feat(backend): add CheckInCommand with server-side radius validation"
```

---

### Task 5: `CheckOutCommand`

**Files:**
- Create: `backend/src/TaskMgmt.Application/Features/Attendance/Commands/CheckOut/CheckOutCommand.cs`
- Create: `backend/src/TaskMgmt.Application/Features/Attendance/Commands/CheckOut/CheckOutCommandValidator.cs`
- Create: `backend/src/TaskMgmt.Application/Features/Attendance/Commands/CheckOut/CheckOutCommandHandler.cs`
- Test: `backend/tests/TaskMgmt.Application.UnitTests/Features/Attendance/CheckOutCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `AttendanceRecordDto` (Task 4); `GeoDistance.CalculateMeters` (Task 3).
- Produces: `CheckOutCommand(double Latitude, double Longitude) : IRequest<AttendanceRecordDto>` — Task 7's controller calls this.

Check-out does not block on radius — it only requires an open (`CheckInAtUtc != null && CheckOutAtUtc == null`) record for today. It still attempts a non-blocking location match purely to fill `CheckOutLocationName` for display.

- [ ] **Step 1: Write the failing tests**

Create `backend/tests/TaskMgmt.Application.UnitTests/Features/Attendance/CheckOutCommandHandlerTests.cs`:

```csharp
using TaskMgmt.Application.Features.Attendance.Commands.CheckOut;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Entities;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.UnitTests.Features.Attendance;

public class CheckOutCommandHandlerTests
{
    [Fact]
    public async Task Handle_AfterCheckIn_UpdatesRecordWithCheckOutTime()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        var location = TestDataFactory.CreateLocation();
        var workDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
        context.Users.Add(user);
        context.Locations.Add(location);
        var record = new AttendanceRecord
        {
            UserId = user.Id,
            WorkDate = workDate,
            CheckInAtUtc = DateTimeOffset.UtcNow.AddHours(-8),
            CheckInLocationId = location.Id,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddHours(-8),
        };
        context.AttendanceRecords.Add(record);
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(user.Id, SystemRole.Member);
        var handler = new CheckOutCommandHandler(context, currentUser);

        var result = await handler.Handle(new CheckOutCommand(location.Latitude, location.Longitude), default);

        Assert.NotNull(result.CheckOutAtUtc);
        Assert.Equal(location.Name, result.CheckOutLocationName);
        var saved = await context.AttendanceRecords.FindAsync(record.Id);
        Assert.NotNull(saved!.CheckOutAtUtc);
    }

    [Fact]
    public async Task Handle_FarFromAnyLocation_StillSucceedsWithNullCheckOutLocation()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        var location = TestDataFactory.CreateLocation();
        var workDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
        context.Users.Add(user);
        context.Locations.Add(location);
        context.AttendanceRecords.Add(new AttendanceRecord
        {
            UserId = user.Id,
            WorkDate = workDate,
            CheckInAtUtc = DateTimeOffset.UtcNow.AddHours(-8),
            CheckInLocationId = location.Id,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddHours(-8),
        });
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(user.Id, SystemRole.Member);
        var handler = new CheckOutCommandHandler(context, currentUser);

        var result = await handler.Handle(new CheckOutCommand(location.Latitude + 0.1, location.Longitude), default);

        Assert.NotNull(result.CheckOutAtUtc);
        Assert.Null(result.CheckOutLocationName);
    }

    [Fact]
    public async Task Validator_NotCheckedInToday_IsInvalid()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(user.Id, SystemRole.Member);
        var validator = new CheckOutCommandValidator(context, currentUser);

        var result = await validator.ValidateAsync(new CheckOutCommand(10.0, 106.0), default);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validator_AlreadyCheckedOutToday_IsInvalid()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        var workDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
        context.Users.Add(user);
        context.AttendanceRecords.Add(new AttendanceRecord
        {
            UserId = user.Id,
            WorkDate = workDate,
            CheckInAtUtc = DateTimeOffset.UtcNow.AddHours(-8),
            CheckOutAtUtc = DateTimeOffset.UtcNow,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddHours(-8),
        });
        await context.SaveChangesAsync(default);

        var currentUser = new FakeCurrentUserService(user.Id, SystemRole.Member);
        var validator = new CheckOutCommandValidator(context, currentUser);

        var result = await validator.ValidateAsync(new CheckOutCommand(10.0, 106.0), default);

        Assert.False(result.IsValid);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd backend && dotnet test TaskMgmt.slnx --filter "FullyQualifiedName~CheckOutCommandHandlerTests"`
Expected: FAIL — types don't exist yet.

- [ ] **Step 3: Implement the command**

Create `backend/src/TaskMgmt.Application/Features/Attendance/Commands/CheckOut/CheckOutCommand.cs`:

```csharp
using MediatR;
using TaskMgmt.Application.Features.Attendance.Common;

namespace TaskMgmt.Application.Features.Attendance.Commands.CheckOut;

public record CheckOutCommand(double Latitude, double Longitude) : IRequest<AttendanceRecordDto>;
```

- [ ] **Step 4: Implement the validator**

Create `backend/src/TaskMgmt.Application/Features/Attendance/Commands/CheckOut/CheckOutCommandValidator.cs`:

```csharp
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;

namespace TaskMgmt.Application.Features.Attendance.Commands.CheckOut;

public class CheckOutCommandValidator : AbstractValidator<CheckOutCommand>
{
    public CheckOutCommandValidator(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);

        RuleFor(x => x)
            .MustAsync(async (_, cancellationToken) =>
            {
                var workDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
                return await context.AttendanceRecords.AnyAsync(
                    a => a.UserId == currentUser.UserId && a.WorkDate == workDate
                         && a.CheckInAtUtc != null && a.CheckOutAtUtc == null,
                    cancellationToken);
            })
            .WithMessage("Bạn chưa check-in hôm nay.")
            .WithName("CheckOut");
    }
}
```

- [ ] **Step 5: Implement the handler**

Create `backend/src/TaskMgmt.Application/Features/Attendance/Commands/CheckOut/CheckOutCommandHandler.cs`:

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Attendance.Common;

namespace TaskMgmt.Application.Features.Attendance.Commands.CheckOut;

public class CheckOutCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<CheckOutCommand, AttendanceRecordDto>
{
    public async Task<AttendanceRecordDto> Handle(CheckOutCommand request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var workDate = DateOnly.FromDateTime(now.ToOffset(TimeSpan.FromHours(7)).DateTime);

        var record = await context.AttendanceRecords
            .Include(a => a.CheckInLocation)
            .FirstAsync(a => a.UserId == currentUser.UserId && a.WorkDate == workDate, cancellationToken);

        var activeLocations = await context.Locations.Where(l => l.IsActive).ToListAsync(cancellationToken);
        var matchedLocation = activeLocations.FirstOrDefault(l =>
            GeoDistance.CalculateMeters(request.Latitude, request.Longitude, l.Latitude, l.Longitude) <= l.CheckInRadiusMeters);

        record.CheckOutAtUtc = now;
        record.CheckOutLatitude = request.Latitude;
        record.CheckOutLongitude = request.Longitude;
        record.CheckOutLocationId = matchedLocation?.Id;
        record.UpdatedAtUtc = now;
        record.UpdatedByUserId = currentUser.UserId;

        await context.SaveChangesAsync(cancellationToken);

        return new AttendanceRecordDto(
            record.Id, record.WorkDate, record.CheckInAtUtc, record.CheckInLocation?.Name,
            record.CheckOutAtUtc, matchedLocation?.Name);
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `cd backend && dotnet test TaskMgmt.slnx --filter "FullyQualifiedName~CheckOutCommandHandlerTests"`
Expected: PASS (4 tests).

- [ ] **Step 7: Commit**

```bash
git add backend/src/TaskMgmt.Application/Features/Attendance/Commands/CheckOut/ backend/tests/TaskMgmt.Application.UnitTests/Features/Attendance/CheckOutCommandHandlerTests.cs
git commit -m "feat(backend): add CheckOutCommand"
```

---

### Task 6: Attendance queries — today / history / stats

**Files:**
- Create: `backend/src/TaskMgmt.Application/Features/Attendance/Common/AttendanceStatsDto.cs`
- Create: `backend/src/TaskMgmt.Application/Features/Attendance/Queries/GetTodayAttendance/GetTodayAttendanceQuery.cs`
- Create: `backend/src/TaskMgmt.Application/Features/Attendance/Queries/GetTodayAttendance/GetTodayAttendanceQueryHandler.cs`
- Create: `backend/src/TaskMgmt.Application/Features/Attendance/Queries/GetAttendanceHistory/GetAttendanceHistoryQuery.cs`
- Create: `backend/src/TaskMgmt.Application/Features/Attendance/Queries/GetAttendanceHistory/GetAttendanceHistoryQueryHandler.cs`
- Create: `backend/src/TaskMgmt.Application/Features/Attendance/Queries/GetAttendanceStats/GetAttendanceStatsQuery.cs`
- Create: `backend/src/TaskMgmt.Application/Features/Attendance/Queries/GetAttendanceStats/GetAttendanceStatsQueryHandler.cs`
- Test: `backend/tests/TaskMgmt.Application.UnitTests/Features/Attendance/AttendanceQueriesTests.cs`

**Interfaces:**
- Consumes: `AttendanceRecordDto` (Task 4).
- Produces: `AttendanceStatsDto(int DaysCheckedIn, double TotalHoursWorked)`; `GetTodayAttendanceQuery : IRequest<AttendanceRecordDto?>`; `GetAttendanceHistoryQuery(int Year, int Month) : IRequest<List<AttendanceRecordDto>>`; `GetAttendanceStatsQuery(int Year, int Month) : IRequest<AttendanceStatsDto>` — Task 7's controller calls all three.

- [ ] **Step 1: Write the failing tests**

Create `backend/tests/TaskMgmt.Application.UnitTests/Features/Attendance/AttendanceQueriesTests.cs`:

```csharp
using TaskMgmt.Application.Features.Attendance.Queries.GetAttendanceHistory;
using TaskMgmt.Application.Features.Attendance.Queries.GetAttendanceStats;
using TaskMgmt.Application.Features.Attendance.Queries.GetTodayAttendance;
using TaskMgmt.Application.UnitTests.Common;
using TaskMgmt.Domain.Entities;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.UnitTests.Features.Attendance;

public class AttendanceQueriesTests
{
    [Fact]
    public async Task GetTodayAttendance_NoRecordToday_ReturnsNull()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync(default);

        var handler = new GetTodayAttendanceQueryHandler(context, new FakeCurrentUserService(user.Id, SystemRole.Member));

        var result = await handler.Handle(new GetTodayAttendanceQuery(), default);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetTodayAttendance_HasRecordToday_ReturnsIt()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        var workDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
        context.Users.Add(user);
        context.AttendanceRecords.Add(new AttendanceRecord
        {
            UserId = user.Id,
            WorkDate = workDate,
            CheckInAtUtc = DateTimeOffset.UtcNow,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        await context.SaveChangesAsync(default);

        var handler = new GetTodayAttendanceQueryHandler(context, new FakeCurrentUserService(user.Id, SystemRole.Member));

        var result = await handler.Handle(new GetTodayAttendanceQuery(), default);

        Assert.NotNull(result);
        Assert.Equal(workDate, result!.WorkDate);
    }

    [Fact]
    public async Task GetAttendanceHistory_ReturnsOnlyRecordsInRequestedMonth()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        context.Users.Add(user);
        context.AttendanceRecords.AddRange(
            new AttendanceRecord { UserId = user.Id, WorkDate = new DateOnly(2026, 8, 5), CheckInAtUtc = DateTimeOffset.UtcNow, CreatedAtUtc = DateTimeOffset.UtcNow },
            new AttendanceRecord { UserId = user.Id, WorkDate = new DateOnly(2026, 8, 20), CheckInAtUtc = DateTimeOffset.UtcNow, CreatedAtUtc = DateTimeOffset.UtcNow },
            new AttendanceRecord { UserId = user.Id, WorkDate = new DateOnly(2026, 7, 31), CheckInAtUtc = DateTimeOffset.UtcNow, CreatedAtUtc = DateTimeOffset.UtcNow });
        await context.SaveChangesAsync(default);

        var handler = new GetAttendanceHistoryQueryHandler(context, new FakeCurrentUserService(user.Id, SystemRole.Member));

        var result = await handler.Handle(new GetAttendanceHistoryQuery(2026, 8), default);

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(8, r.WorkDate.Month));
    }

    [Fact]
    public async Task GetAttendanceStats_CountsDaysAndSumsOnlyCompletedHours()
    {
        using var context = TestDbContextFactory.Create();
        var user = TestDataFactory.CreateUser();
        context.Users.Add(user);
        context.AttendanceRecords.AddRange(
            new AttendanceRecord
            {
                UserId = user.Id,
                WorkDate = new DateOnly(2026, 8, 1),
                CheckInAtUtc = new DateTimeOffset(2026, 8, 1, 1, 0, 0, TimeSpan.Zero),
                CheckOutAtUtc = new DateTimeOffset(2026, 8, 1, 9, 0, 0, TimeSpan.Zero),
                CreatedAtUtc = DateTimeOffset.UtcNow,
            },
            new AttendanceRecord
            {
                // Đã check-in nhưng chưa check-out - tính vào DaysCheckedIn, không cộng giờ.
                UserId = user.Id,
                WorkDate = new DateOnly(2026, 8, 2),
                CheckInAtUtc = new DateTimeOffset(2026, 8, 2, 1, 0, 0, TimeSpan.Zero),
                CreatedAtUtc = DateTimeOffset.UtcNow,
            });
        await context.SaveChangesAsync(default);

        var handler = new GetAttendanceStatsQueryHandler(context, new FakeCurrentUserService(user.Id, SystemRole.Member));

        var result = await handler.Handle(new GetAttendanceStatsQuery(2026, 8), default);

        Assert.Equal(2, result.DaysCheckedIn);
        Assert.Equal(8, result.TotalHoursWorked);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd backend && dotnet test TaskMgmt.slnx --filter "FullyQualifiedName~AttendanceQueriesTests"`
Expected: FAIL — types don't exist yet.

- [ ] **Step 3: Add `AttendanceStatsDto`**

Create `backend/src/TaskMgmt.Application/Features/Attendance/Common/AttendanceStatsDto.cs`:

```csharp
namespace TaskMgmt.Application.Features.Attendance.Common;

public record AttendanceStatsDto(int DaysCheckedIn, double TotalHoursWorked);
```

- [ ] **Step 4: Implement `GetTodayAttendanceQuery`**

Create `backend/src/TaskMgmt.Application/Features/Attendance/Queries/GetTodayAttendance/GetTodayAttendanceQuery.cs`:

```csharp
using MediatR;
using TaskMgmt.Application.Features.Attendance.Common;

namespace TaskMgmt.Application.Features.Attendance.Queries.GetTodayAttendance;

public record GetTodayAttendanceQuery : IRequest<AttendanceRecordDto?>;
```

Create `backend/src/TaskMgmt.Application/Features/Attendance/Queries/GetTodayAttendance/GetTodayAttendanceQueryHandler.cs`:

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Attendance.Common;

namespace TaskMgmt.Application.Features.Attendance.Queries.GetTodayAttendance;

public class GetTodayAttendanceQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<GetTodayAttendanceQuery, AttendanceRecordDto?>
{
    public async Task<AttendanceRecordDto?> Handle(GetTodayAttendanceQuery request, CancellationToken cancellationToken)
    {
        var workDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);

        var record = await context.AttendanceRecords
            .Where(a => a.UserId == currentUser.UserId && a.WorkDate == workDate)
            .Select(a => new AttendanceRecordDto(
                a.Id, a.WorkDate, a.CheckInAtUtc, a.CheckInLocation!.Name, a.CheckOutAtUtc, a.CheckOutLocation!.Name))
            .FirstOrDefaultAsync(cancellationToken);

        return record;
    }
}
```

- [ ] **Step 5: Implement `GetAttendanceHistoryQuery`**

Create `backend/src/TaskMgmt.Application/Features/Attendance/Queries/GetAttendanceHistory/GetAttendanceHistoryQuery.cs`:

```csharp
using MediatR;
using TaskMgmt.Application.Features.Attendance.Common;

namespace TaskMgmt.Application.Features.Attendance.Queries.GetAttendanceHistory;

public record GetAttendanceHistoryQuery(int Year, int Month) : IRequest<List<AttendanceRecordDto>>;
```

Create `backend/src/TaskMgmt.Application/Features/Attendance/Queries/GetAttendanceHistory/GetAttendanceHistoryQueryHandler.cs`:

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Attendance.Common;

namespace TaskMgmt.Application.Features.Attendance.Queries.GetAttendanceHistory;

public class GetAttendanceHistoryQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<GetAttendanceHistoryQuery, List<AttendanceRecordDto>>
{
    public async Task<List<AttendanceRecordDto>> Handle(GetAttendanceHistoryQuery request, CancellationToken cancellationToken)
    {
        return await context.AttendanceRecords
            .Where(a => a.UserId == currentUser.UserId
                        && a.WorkDate.Year == request.Year
                        && a.WorkDate.Month == request.Month)
            .OrderByDescending(a => a.WorkDate)
            .Select(a => new AttendanceRecordDto(
                a.Id, a.WorkDate, a.CheckInAtUtc, a.CheckInLocation!.Name, a.CheckOutAtUtc, a.CheckOutLocation!.Name))
            .ToListAsync(cancellationToken);
    }
}
```

- [ ] **Step 6: Implement `GetAttendanceStatsQuery`**

Create `backend/src/TaskMgmt.Application/Features/Attendance/Queries/GetAttendanceStats/GetAttendanceStatsQuery.cs`:

```csharp
using MediatR;
using TaskMgmt.Application.Features.Attendance.Common;

namespace TaskMgmt.Application.Features.Attendance.Queries.GetAttendanceStats;

public record GetAttendanceStatsQuery(int Year, int Month) : IRequest<AttendanceStatsDto>;
```

Create `backend/src/TaskMgmt.Application/Features/Attendance/Queries/GetAttendanceStats/GetAttendanceStatsQueryHandler.cs`:

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Attendance.Common;

namespace TaskMgmt.Application.Features.Attendance.Queries.GetAttendanceStats;

public class GetAttendanceStatsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<GetAttendanceStatsQuery, AttendanceStatsDto>
{
    public async Task<AttendanceStatsDto> Handle(GetAttendanceStatsQuery request, CancellationToken cancellationToken)
    {
        var records = await context.AttendanceRecords
            .Where(a => a.UserId == currentUser.UserId
                        && a.WorkDate.Year == request.Year
                        && a.WorkDate.Month == request.Month
                        && a.CheckInAtUtc != null)
            .Select(a => new { a.CheckInAtUtc, a.CheckOutAtUtc })
            .ToListAsync(cancellationToken);

        var daysCheckedIn = records.Count;
        var totalHours = records
            .Where(a => a.CheckOutAtUtc != null)
            .Sum(a => (a.CheckOutAtUtc!.Value - a.CheckInAtUtc!.Value).TotalHours);

        return new AttendanceStatsDto(daysCheckedIn, totalHours);
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

Run: `cd backend && dotnet test TaskMgmt.slnx --filter "FullyQualifiedName~AttendanceQueriesTests"`
Expected: PASS (4 tests).

- [ ] **Step 8: Commit**

```bash
git add backend/src/TaskMgmt.Application/Features/Attendance/Common/AttendanceStatsDto.cs backend/src/TaskMgmt.Application/Features/Attendance/Queries/ backend/tests/TaskMgmt.Application.UnitTests/Features/Attendance/AttendanceQueriesTests.cs
git commit -m "feat(backend): add attendance today/history/stats queries"
```

---

### Task 7: `AttendanceController`

**Files:**
- Create: `backend/src/TaskMgmt.API/Controllers/AttendanceController.cs`

**Interfaces:**
- Consumes: `CheckInCommand` (Task 4), `CheckOutCommand` (Task 5), `GetTodayAttendanceQuery`/`GetAttendanceHistoryQuery`/`GetAttendanceStatsQuery` (Task 6).
- Produces: `POST /api/v1/attendance/check-in`, `POST /api/v1/attendance/check-out`, `GET /api/v1/attendance/today`, `GET /api/v1/attendance/history?year=&month=`, `GET /api/v1/attendance/stats?year=&month=` — Task 9 (mobile datasource) calls these exact routes.

No new backend test for this task — it's thin routing over already-tested handlers, consistent with how `LocationsController`/`AttachmentsController` have no dedicated controller tests in this codebase. Verified via full build + test suite.

- [ ] **Step 1: Implement the controller**

Create `backend/src/TaskMgmt.API/Controllers/AttendanceController.cs`:

```csharp
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskMgmt.Application.Features.Attendance.Commands.CheckIn;
using TaskMgmt.Application.Features.Attendance.Commands.CheckOut;
using TaskMgmt.Application.Features.Attendance.Common;
using TaskMgmt.Application.Features.Attendance.Queries.GetAttendanceHistory;
using TaskMgmt.Application.Features.Attendance.Queries.GetAttendanceStats;
using TaskMgmt.Application.Features.Attendance.Queries.GetTodayAttendance;

namespace TaskMgmt.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/attendance")]
public class AttendanceController(ISender sender) : ControllerBase
{
    [HttpPost("check-in")]
    public async Task<ActionResult<AttendanceRecordDto>> CheckIn(CheckInCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("check-out")]
    public async Task<ActionResult<AttendanceRecordDto>> CheckOut(CheckOutCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet("today")]
    public async Task<ActionResult<AttendanceRecordDto?>> GetToday(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTodayAttendanceQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("history")]
    public async Task<ActionResult<List<AttendanceRecordDto>>> GetHistory(
        [FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAttendanceHistoryQuery(year, month), cancellationToken);
        return Ok(result);
    }

    [HttpGet("stats")]
    public async Task<ActionResult<AttendanceStatsDto>> GetStats(
        [FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAttendanceStatsQuery(year, month), cancellationToken);
        return Ok(result);
    }
}
```

- [ ] **Step 2: Verify the full backend build and test suite**

Run:
```bash
cd backend
dotnet build TaskMgmt.slnx
dotnet test TaskMgmt.slnx
```
Expected: build succeeds, all tests pass (existing + all Attendance tests from Tasks 3-6).

- [ ] **Step 3: Commit**

```bash
git add backend/src/TaskMgmt.API/Controllers/AttendanceController.cs
git commit -m "feat(backend): add AttendanceController"
```

---

### Task 8: `geolocator` dependency + location permissions

**Files:**
- Modify: `mobile/taskmgmt_app/pubspec.yaml`
- Modify: `mobile/taskmgmt_app/android/app/src/main/AndroidManifest.xml`
- Modify: `mobile/taskmgmt_app/ios/Runner/Info.plist`

**Interfaces:**
- Produces: `geolocator` package available for import (`Geolocator`, `Position`, `LocationPermission`) in Task 11.

Config-only task, no automated test — verified via `flutter pub get` succeeding and `flutter analyze` staying clean.

- [ ] **Step 1: Add the dependency**

In `mobile/taskmgmt_app/pubspec.yaml`, add this line right after `latlong2: ^0.10.1`:

```yaml
  geolocator: ^13.0.0
```

If `flutter pub get` in Step 2 reports a newer stable major version is required by another already-installed package, use whatever version `flutter pub get`/`flutter pub outdated` resolves to instead — `^13.0.0` is a floor, not a hard pin.

- [ ] **Step 2: Fetch packages**

Run: `cd mobile/taskmgmt_app && flutter pub get`
Expected: resolves successfully, no version conflicts.

- [ ] **Step 3: Declare Android location permissions**

In `mobile/taskmgmt_app/android/app/src/main/AndroidManifest.xml`, this repo currently has no `<uses-permission>` entries at all. Add these two lines as the first children of `<manifest ...>`, immediately before the `<application ...>` tag:

```xml
    <uses-permission android:name="android.permission.ACCESS_FINE_LOCATION"/>
    <uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION"/>
```

- [ ] **Step 4: Declare iOS location usage description**

In `mobile/taskmgmt_app/ios/Runner/Info.plist`, add this key/value pair inside the top-level `<dict>`, right after the `<key>CFBundleDisplayName</key><string>Taskmgmt App</string>` pair:

```xml
	<key>NSLocationWhenInUseUsageDescription</key>
	<string>Ứng dụng cần quyền truy cập vị trí để chấm công GPS.</string>
```

- [ ] **Step 5: Verify nothing broke**

Run: `cd mobile/taskmgmt_app && flutter analyze`
Expected: No errors.

- [ ] **Step 6: Commit**

```bash
git add mobile/taskmgmt_app/pubspec.yaml mobile/taskmgmt_app/pubspec.lock mobile/taskmgmt_app/android/app/src/main/AndroidManifest.xml mobile/taskmgmt_app/ios/Runner/Info.plist
git commit -m "feat(mobile): add geolocator dependency and location permissions"
```

---

### Task 9: Mobile domain/data layer — `attendance/` feature

**Files:**
- Create: `mobile/taskmgmt_app/lib/features/attendance/domain/entities/attendance_record.dart`
- Create: `mobile/taskmgmt_app/lib/features/attendance/domain/entities/attendance_stats.dart`
- Create: `mobile/taskmgmt_app/lib/features/attendance/domain/repositories/attendance_repository.dart`
- Create: `mobile/taskmgmt_app/lib/features/attendance/data/models/attendance_record_model.dart`
- Create: `mobile/taskmgmt_app/lib/features/attendance/data/models/attendance_stats_model.dart`
- Create: `mobile/taskmgmt_app/lib/features/attendance/data/datasources/attendance_remote_data_source.dart`
- Create: `mobile/taskmgmt_app/lib/features/attendance/data/repositories/attendance_repository_impl.dart`
- Modify: `mobile/taskmgmt_app/lib/core/di/injection.dart`

**Interfaces:**
- Consumes: backend routes from Task 7 (`/attendance/check-in`, `/attendance/check-out`, `/attendance/today`, `/attendance/history`, `/attendance/stats`).
- Produces: `AttendanceRecord`, `AttendanceStats` entities; `abstract class AttendanceRepository { Future<AttendanceRecord> checkIn({required double latitude, required double longitude}); Future<AttendanceRecord> checkOut({required double latitude, required double longitude}); Future<AttendanceRecord?> getToday(); Future<List<AttendanceRecord>> getHistory({required int year, required int month}); Future<AttendanceStats> getStats({required int year, required int month}); }` — Task 10's provider consumes this. `attendanceRepositoryProvider : Provider<AttendanceRepository>` (registered via `getIt`) — Task 10 reads this.

No dedicated test for this task — the Dio datasource layer isn't unit-tested directly anywhere in this codebase (same as `LocationRemoteDataSource`/`AttachmentRemoteDataSource`); Task 10/11's fakes implement `AttendanceRepository` directly. Verified via `flutter analyze` + full test suite.

- [ ] **Step 1: Domain entities**

Create `mobile/taskmgmt_app/lib/features/attendance/domain/entities/attendance_record.dart`:

```dart
class AttendanceRecord {
  const AttendanceRecord({
    required this.id,
    required this.workDate,
    required this.checkInAtUtc,
    required this.checkInLocationName,
    required this.checkOutAtUtc,
    required this.checkOutLocationName,
  });

  final String id;
  final DateTime workDate;
  final DateTime? checkInAtUtc;
  final String? checkInLocationName;
  final DateTime? checkOutAtUtc;
  final String? checkOutLocationName;

  bool get isCheckedIn => checkInAtUtc != null;
  bool get isCheckedOut => checkOutAtUtc != null;
}
```

Create `mobile/taskmgmt_app/lib/features/attendance/domain/entities/attendance_stats.dart`:

```dart
class AttendanceStats {
  const AttendanceStats({required this.daysCheckedIn, required this.totalHoursWorked});

  final int daysCheckedIn;
  final double totalHoursWorked;
}
```

- [ ] **Step 2: Repository interface**

Create `mobile/taskmgmt_app/lib/features/attendance/domain/repositories/attendance_repository.dart`:

```dart
import '../entities/attendance_record.dart';
import '../entities/attendance_stats.dart';

abstract class AttendanceRepository {
  Future<AttendanceRecord> checkIn({required double latitude, required double longitude});

  Future<AttendanceRecord> checkOut({required double latitude, required double longitude});

  Future<AttendanceRecord?> getToday();

  Future<List<AttendanceRecord>> getHistory({required int year, required int month});

  Future<AttendanceStats> getStats({required int year, required int month});
}
```

- [ ] **Step 3: Data models**

Create `mobile/taskmgmt_app/lib/features/attendance/data/models/attendance_record_model.dart`:

```dart
import '../../domain/entities/attendance_record.dart';

class AttendanceRecordModel {
  const AttendanceRecordModel({
    required this.id,
    required this.workDate,
    required this.checkInAtUtc,
    required this.checkInLocationName,
    required this.checkOutAtUtc,
    required this.checkOutLocationName,
  });

  final String id;
  final DateTime workDate;
  final DateTime? checkInAtUtc;
  final String? checkInLocationName;
  final DateTime? checkOutAtUtc;
  final String? checkOutLocationName;

  factory AttendanceRecordModel.fromJson(Map<String, dynamic> json) => AttendanceRecordModel(
        id: json['id'] as String,
        workDate: DateTime.parse(json['workDate'] as String),
        checkInAtUtc: json['checkInAtUtc'] == null ? null : DateTime.parse(json['checkInAtUtc'] as String),
        checkInLocationName: json['checkInLocationName'] as String?,
        checkOutAtUtc: json['checkOutAtUtc'] == null ? null : DateTime.parse(json['checkOutAtUtc'] as String),
        checkOutLocationName: json['checkOutLocationName'] as String?,
      );

  AttendanceRecord toDomain() => AttendanceRecord(
        id: id,
        workDate: workDate,
        checkInAtUtc: checkInAtUtc,
        checkInLocationName: checkInLocationName,
        checkOutAtUtc: checkOutAtUtc,
        checkOutLocationName: checkOutLocationName,
      );
}
```

Create `mobile/taskmgmt_app/lib/features/attendance/data/models/attendance_stats_model.dart`:

```dart
import '../../domain/entities/attendance_stats.dart';

class AttendanceStatsModel {
  const AttendanceStatsModel({required this.daysCheckedIn, required this.totalHoursWorked});

  final int daysCheckedIn;
  final double totalHoursWorked;

  factory AttendanceStatsModel.fromJson(Map<String, dynamic> json) => AttendanceStatsModel(
        daysCheckedIn: json['daysCheckedIn'] as int,
        totalHoursWorked: (json['totalHoursWorked'] as num).toDouble(),
      );

  AttendanceStats toDomain() => AttendanceStats(daysCheckedIn: daysCheckedIn, totalHoursWorked: totalHoursWorked);
}
```

- [ ] **Step 4: Remote data source**

Create `mobile/taskmgmt_app/lib/features/attendance/data/datasources/attendance_remote_data_source.dart`:

```dart
import 'package:dio/dio.dart';

import '../../../../core/network/api_exception.dart';
import '../models/attendance_record_model.dart';
import '../models/attendance_stats_model.dart';

class AttendanceRemoteDataSource {
  AttendanceRemoteDataSource(this._dio);

  final Dio _dio;

  Future<AttendanceRecordModel> checkIn({required double latitude, required double longitude}) async {
    try {
      final response = await _dio.post<Map<String, dynamic>>(
        '/attendance/check-in',
        data: {'latitude': latitude, 'longitude': longitude},
      );
      return AttendanceRecordModel.fromJson(response.data!);
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<AttendanceRecordModel> checkOut({required double latitude, required double longitude}) async {
    try {
      final response = await _dio.post<Map<String, dynamic>>(
        '/attendance/check-out',
        data: {'latitude': latitude, 'longitude': longitude},
      );
      return AttendanceRecordModel.fromJson(response.data!);
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<AttendanceRecordModel?> getToday() async {
    try {
      final response = await _dio.get<Map<String, dynamic>?>('/attendance/today');
      return response.data == null ? null : AttendanceRecordModel.fromJson(response.data!);
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<List<AttendanceRecordModel>> getHistory({required int year, required int month}) async {
    try {
      final response = await _dio.get<List<dynamic>>(
        '/attendance/history',
        queryParameters: {'year': year, 'month': month},
      );
      return response.data!.map((json) => AttendanceRecordModel.fromJson(json as Map<String, dynamic>)).toList();
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<AttendanceStatsModel> getStats({required int year, required int month}) async {
    try {
      final response = await _dio.get<Map<String, dynamic>>(
        '/attendance/stats',
        queryParameters: {'year': year, 'month': month},
      );
      return AttendanceStatsModel.fromJson(response.data!);
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }
}
```

- [ ] **Step 5: Repository implementation**

Create `mobile/taskmgmt_app/lib/features/attendance/data/repositories/attendance_repository_impl.dart`:

```dart
import '../../domain/entities/attendance_record.dart';
import '../../domain/entities/attendance_stats.dart';
import '../../domain/repositories/attendance_repository.dart';
import '../datasources/attendance_remote_data_source.dart';

class AttendanceRepositoryImpl implements AttendanceRepository {
  AttendanceRepositoryImpl(this._remoteDataSource);

  final AttendanceRemoteDataSource _remoteDataSource;

  @override
  Future<AttendanceRecord> checkIn({required double latitude, required double longitude}) async {
    final model = await _remoteDataSource.checkIn(latitude: latitude, longitude: longitude);
    return model.toDomain();
  }

  @override
  Future<AttendanceRecord> checkOut({required double latitude, required double longitude}) async {
    final model = await _remoteDataSource.checkOut(latitude: latitude, longitude: longitude);
    return model.toDomain();
  }

  @override
  Future<AttendanceRecord?> getToday() async {
    final model = await _remoteDataSource.getToday();
    return model?.toDomain();
  }

  @override
  Future<List<AttendanceRecord>> getHistory({required int year, required int month}) async {
    final models = await _remoteDataSource.getHistory(year: year, month: month);
    return models.map((model) => model.toDomain()).toList();
  }

  @override
  Future<AttendanceStats> getStats({required int year, required int month}) async {
    final model = await _remoteDataSource.getStats(year: year, month: month);
    return model.toDomain();
  }
}
```

- [ ] **Step 6: Register with `get_it`**

In `mobile/taskmgmt_app/lib/core/di/injection.dart`, add this import near the other feature imports (alphabetically after `attachments`):

```dart
import '../../features/attendance/data/datasources/attendance_remote_data_source.dart';
import '../../features/attendance/data/repositories/attendance_repository_impl.dart';
import '../../features/attendance/domain/repositories/attendance_repository.dart';
```

Then, inside `setupLocator()`, add these two lines right after the `AttachmentRepository` registration:

```dart
  getIt.registerLazySingleton<AttendanceRemoteDataSource>(() => AttendanceRemoteDataSource(getIt()));
  getIt.registerLazySingleton<AttendanceRepository>(() => AttendanceRepositoryImpl(getIt()));
```

- [ ] **Step 7: Verify**

Run:
```bash
cd mobile/taskmgmt_app
flutter analyze
flutter test --concurrency=1
```
Expected: no analyzer errors, all existing tests still pass (no new tests in this task).

- [ ] **Step 8: Commit**

```bash
git add mobile/taskmgmt_app/lib/features/attendance/domain/ mobile/taskmgmt_app/lib/features/attendance/data/ mobile/taskmgmt_app/lib/core/di/injection.dart
git commit -m "feat(mobile): add attendance domain/data layer"
```

---

### Task 10: `AttendanceController` (Riverpod provider)

**Files:**
- Create: `mobile/taskmgmt_app/lib/features/attendance/presentation/providers/attendance_provider.dart`
- Test: `mobile/taskmgmt_app/test/attendance_provider_test.dart`

**Interfaces:**
- Consumes: `AttendanceRepository` (Task 9).
- Produces: `attendanceRepositoryProvider : Provider<AttendanceRepository>`; `todayAttendanceProvider : AsyncNotifierProvider<TodayAttendanceController, AttendanceRecord?>` with `Future<void> checkIn({required double latitude, required double longitude})` and `Future<void> checkOut({required double latitude, required double longitude})` methods; `attendanceHistoryProvider(({int year, int month}) args) : AsyncNotifierProviderFamily<AttendanceHistoryController, List<AttendanceRecord>, ({int year, int month})>`; `attendanceStatsProvider(({int year, int month}) args) : AsyncNotifierProviderFamily<AttendanceStatsController, AttendanceStats, ({int year, int month})>` — Task 11's screen consumes all of these.

- [ ] **Step 1: Write the failing tests**

Create `mobile/taskmgmt_app/test/attendance_provider_test.dart`:

```dart
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:taskmgmt_app/features/attendance/domain/entities/attendance_record.dart';
import 'package:taskmgmt_app/features/attendance/domain/entities/attendance_stats.dart';
import 'package:taskmgmt_app/features/attendance/domain/repositories/attendance_repository.dart';
import 'package:taskmgmt_app/features/attendance/presentation/providers/attendance_provider.dart';

class _FakeAttendanceRepository implements AttendanceRepository {
  AttendanceRecord? today;
  bool throwOnCheckIn = false;

  @override
  Future<AttendanceRecord> checkIn({required double latitude, required double longitude}) async {
    if (throwOnCheckIn) throw Exception('Ngoài phạm vi cho phép của mọi vị trí đã đăng ký.');
    today = AttendanceRecord(
      id: 'a1',
      workDate: DateTime(2026, 8, 24),
      checkInAtUtc: DateTime.utc(2026, 8, 24, 1),
      checkInLocationName: 'Văn phòng chính',
      checkOutAtUtc: null,
      checkOutLocationName: null,
    );
    return today!;
  }

  @override
  Future<AttendanceRecord> checkOut({required double latitude, required double longitude}) async {
    today = AttendanceRecord(
      id: today!.id,
      workDate: today!.workDate,
      checkInAtUtc: today!.checkInAtUtc,
      checkInLocationName: today!.checkInLocationName,
      checkOutAtUtc: DateTime.utc(2026, 8, 24, 9),
      checkOutLocationName: 'Văn phòng chính',
    );
    return today!;
  }

  @override
  Future<AttendanceRecord?> getToday() async => today;

  @override
  Future<List<AttendanceRecord>> getHistory({required int year, required int month}) async =>
      today == null ? [] : [today!];

  @override
  Future<AttendanceStats> getStats({required int year, required int month}) async =>
      const AttendanceStats(daysCheckedIn: 1, totalHoursWorked: 8);
}

ProviderContainer _buildContainer(_FakeAttendanceRepository repo) => ProviderContainer(
      overrides: [attendanceRepositoryProvider.overrideWithValue(repo)],
    );

void main() {
  test('todayAttendanceProvider starts null when nothing checked in', () async {
    final container = _buildContainer(_FakeAttendanceRepository());
    addTearDown(container.dispose);

    final result = await container.read(todayAttendanceProvider.future);

    expect(result, isNull);
  });

  test('checkIn updates todayAttendanceProvider state', () async {
    final repo = _FakeAttendanceRepository();
    final container = _buildContainer(repo);
    addTearDown(container.dispose);
    await container.read(todayAttendanceProvider.future);

    await container.read(todayAttendanceProvider.notifier).checkIn(latitude: 10, longitude: 106);

    final state = container.read(todayAttendanceProvider).value;
    expect(state?.isCheckedIn, isTrue);
    expect(state?.checkInLocationName, 'Văn phòng chính');
  });

  test('checkIn failure leaves state as error without crashing', () async {
    final repo = _FakeAttendanceRepository()..throwOnCheckIn = true;
    final container = _buildContainer(repo);
    addTearDown(container.dispose);
    await container.read(todayAttendanceProvider.future);

    await expectLater(
      container.read(todayAttendanceProvider.notifier).checkIn(latitude: 10, longitude: 106),
      throwsException,
    );
  });

  test('checkOut updates todayAttendanceProvider state', () async {
    final repo = _FakeAttendanceRepository();
    final container = _buildContainer(repo);
    addTearDown(container.dispose);
    await container.read(todayAttendanceProvider.notifier).checkIn(latitude: 10, longitude: 106);

    await container.read(todayAttendanceProvider.notifier).checkOut(latitude: 10, longitude: 106);

    final state = container.read(todayAttendanceProvider).value;
    expect(state?.isCheckedOut, isTrue);
  });

  test('attendanceStatsProvider reads stats for the requested month', () async {
    final container = _buildContainer(_FakeAttendanceRepository());
    addTearDown(container.dispose);

    final stats = await container.read(attendanceStatsProvider((year: 2026, month: 8)).future);

    expect(stats.daysCheckedIn, 1);
    expect(stats.totalHoursWorked, 8);
  });
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd mobile/taskmgmt_app && flutter test test/attendance_provider_test.dart --concurrency=1`
Expected: FAIL — `attendance_provider.dart` does not exist yet.

- [ ] **Step 3: Implement the provider**

Create `mobile/taskmgmt_app/lib/features/attendance/presentation/providers/attendance_provider.dart`:

```dart
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/di/injection.dart';
import '../../domain/entities/attendance_record.dart';
import '../../domain/entities/attendance_stats.dart';
import '../../domain/repositories/attendance_repository.dart';

final attendanceRepositoryProvider = Provider<AttendanceRepository>((ref) => getIt<AttendanceRepository>());

final todayAttendanceProvider =
    AsyncNotifierProvider<TodayAttendanceController, AttendanceRecord?>(TodayAttendanceController.new);

class TodayAttendanceController extends AsyncNotifier<AttendanceRecord?> {
  @override
  Future<AttendanceRecord?> build() {
    return ref.read(attendanceRepositoryProvider).getToday();
  }

  Future<void> checkIn({required double latitude, required double longitude}) async {
    state = await AsyncValue.guard(
      () => ref.read(attendanceRepositoryProvider).checkIn(latitude: latitude, longitude: longitude),
    );
    if (state.hasError) {
      throw state.error!;
    }
  }

  Future<void> checkOut({required double latitude, required double longitude}) async {
    state = await AsyncValue.guard(
      () => ref.read(attendanceRepositoryProvider).checkOut(latitude: latitude, longitude: longitude),
    );
    if (state.hasError) {
      throw state.error!;
    }
  }
}

typedef _YearMonth = ({int year, int month});

final attendanceHistoryProvider =
    AsyncNotifierProviderFamily<AttendanceHistoryController, List<AttendanceRecord>, _YearMonth>(
        AttendanceHistoryController.new);

class AttendanceHistoryController extends FamilyAsyncNotifier<List<AttendanceRecord>, _YearMonth> {
  @override
  Future<List<AttendanceRecord>> build(_YearMonth arg) {
    return ref.read(attendanceRepositoryProvider).getHistory(year: arg.year, month: arg.month);
  }
}

final attendanceStatsProvider =
    AsyncNotifierProviderFamily<AttendanceStatsController, AttendanceStats, _YearMonth>(
        AttendanceStatsController.new);

class AttendanceStatsController extends FamilyAsyncNotifier<AttendanceStats, _YearMonth> {
  @override
  Future<AttendanceStats> build(_YearMonth arg) {
    return ref.read(attendanceRepositoryProvider).getStats(year: arg.year, month: arg.month);
  }
}
```

Note: `checkIn`/`checkOut` rethrow the error after storing it in `state` — this lets the screen's `try`/`catch` around the call show a SnackBar, while the provider's `AsyncError` state simultaneously reverts the UI (e.g. re-enables the button) exactly like the existing `NotificationsController`/`AttachmentsController` pattern with `AsyncValue.guard`.

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd mobile/taskmgmt_app && flutter test test/attendance_provider_test.dart --concurrency=1`
Expected: PASS (5 tests).

- [ ] **Step 5: Commit**

```bash
git add mobile/taskmgmt_app/lib/features/attendance/presentation/providers/ mobile/taskmgmt_app/test/attendance_provider_test.dart
git commit -m "feat(mobile): add attendance Riverpod providers"
```

---

### Task 11: `AttendanceScreen` UI + entry point

**Files:**
- Create: `mobile/taskmgmt_app/lib/features/attendance/presentation/screens/attendance_screen.dart`
- Modify: `mobile/taskmgmt_app/lib/features/tasks/presentation/screens/task_list_screen.dart`
- Modify: `mobile/taskmgmt_app/lib/core/routing/app_router.dart`
- Test: `mobile/taskmgmt_app/test/attendance_screen_test.dart`

**Interfaces:**
- Consumes: `todayAttendanceProvider`, `attendanceHistoryProvider`, `attendanceStatsProvider` (Task 10); `AttendanceRecord`, `AttendanceStats` (Task 9).
- Produces: nothing further (leaf UI task).

GPS reads (`Geolocator.getCurrentPosition()`) happen only inside this screen's button handlers, never in the provider/repository — this keeps the provider layer (already tested in Task 10) fully decoupled from the platform-channel plugin, matching this codebase's established precedent (`image_picker`/`file_picker` calls stay thin and untested in `AttachmentListSection`, while the logic around them is tested). The widget test below drives `todayAttendanceProvider`/`attendanceHistoryProvider`/`attendanceStatsProvider` directly through the fake repository, not through real GPS calls.

- [ ] **Step 1: Write the failing tests**

Create `mobile/taskmgmt_app/test/attendance_screen_test.dart`:

```dart
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:taskmgmt_app/features/attendance/domain/entities/attendance_record.dart';
import 'package:taskmgmt_app/features/attendance/domain/entities/attendance_stats.dart';
import 'package:taskmgmt_app/features/attendance/domain/repositories/attendance_repository.dart';
import 'package:taskmgmt_app/features/attendance/presentation/providers/attendance_provider.dart';
import 'package:taskmgmt_app/features/attendance/presentation/screens/attendance_screen.dart';

class _FakeAttendanceRepository implements AttendanceRepository {
  _FakeAttendanceRepository({this.today, this.history = const [], this.stats = const AttendanceStats(daysCheckedIn: 0, totalHoursWorked: 0)});

  AttendanceRecord? today;
  final List<AttendanceRecord> history;
  final AttendanceStats stats;

  @override
  Future<AttendanceRecord> checkIn({required double latitude, required double longitude}) =>
      throw UnimplementedError();

  @override
  Future<AttendanceRecord> checkOut({required double latitude, required double longitude}) =>
      throw UnimplementedError();

  @override
  Future<AttendanceRecord?> getToday() async => today;

  @override
  Future<List<AttendanceRecord>> getHistory({required int year, required int month}) async => history;

  @override
  Future<AttendanceStats> getStats({required int year, required int month}) async => stats;
}

Widget _buildScreen(_FakeAttendanceRepository repo) => ProviderScope(
      overrides: [attendanceRepositoryProvider.overrideWithValue(repo)],
      child: const MaterialApp(home: AttendanceScreen()),
    );

void main() {
  testWidgets('Shows "chưa check-in" when there is no record today', (tester) async {
    await tester.pumpWidget(_buildScreen(_FakeAttendanceRepository()));
    await tester.pumpAndSettle();

    expect(find.textContaining('Chưa check-in'), findsOneWidget);
  });

  testWidgets('Shows check-in time and location when checked in but not out', (tester) async {
    final repo = _FakeAttendanceRepository(
      today: AttendanceRecord(
        id: 'a1',
        workDate: DateTime(2026, 8, 24),
        checkInAtUtc: DateTime.utc(2026, 8, 24, 1, 30),
        checkInLocationName: 'Văn phòng chính',
        checkOutAtUtc: null,
        checkOutLocationName: null,
      ),
    );
    await tester.pumpWidget(_buildScreen(repo));
    await tester.pumpAndSettle();

    expect(find.textContaining('Văn phòng chính'), findsOneWidget);
  });

  testWidgets('Switching to Lịch sử tab shows history rows and stats', (tester) async {
    final repo = _FakeAttendanceRepository(
      history: [
        AttendanceRecord(
          id: 'a1',
          workDate: DateTime(2026, 8, 20),
          checkInAtUtc: DateTime.utc(2026, 8, 20, 1),
          checkInLocationName: 'Văn phòng chính',
          checkOutAtUtc: DateTime.utc(2026, 8, 20, 9),
          checkOutLocationName: 'Văn phòng chính',
        ),
      ],
      stats: const AttendanceStats(daysCheckedIn: 1, totalHoursWorked: 8),
    );
    await tester.pumpWidget(_buildScreen(repo));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Lịch sử'));
    await tester.pumpAndSettle();

    expect(find.textContaining('Văn phòng chính'), findsWidgets);
    expect(find.textContaining('8'), findsWidgets);
  });
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd mobile/taskmgmt_app && flutter test test/attendance_screen_test.dart --concurrency=1`
Expected: FAIL — `attendance_screen.dart` does not exist yet.

- [ ] **Step 3: Implement the screen**

Create `mobile/taskmgmt_app/lib/features/attendance/presentation/screens/attendance_screen.dart`:

```dart
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:geolocator/geolocator.dart';
import 'package:intl/intl.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../shared/widgets/empty_state_view.dart';
import '../../../../shared/widgets/error_state_view.dart';
import '../../domain/entities/attendance_record.dart';
import '../providers/attendance_provider.dart';

class AttendanceScreen extends ConsumerStatefulWidget {
  const AttendanceScreen({super.key});

  static const path = '/attendance';
  static const name = 'attendance';

  @override
  ConsumerState<AttendanceScreen> createState() => _AttendanceScreenState();
}

class _AttendanceScreenState extends ConsumerState<AttendanceScreen> {
  bool _isSubmitting = false;
  DateTime _selectedMonth = DateTime.now();

  Future<Position> _getCurrentPosition() async {
    var permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
    }
    if (permission == LocationPermission.denied || permission == LocationPermission.deniedForever) {
      throw Exception('Cần quyền truy cập vị trí để chấm công.');
    }
    return Geolocator.getCurrentPosition();
  }

  Future<void> _checkIn() async {
    setState(() => _isSubmitting = true);
    try {
      final position = await _getCurrentPosition();
      await ref
          .read(todayAttendanceProvider.notifier)
          .checkIn(latitude: position.latitude, longitude: position.longitude);
    } catch (e) {
      if (!mounted) return;
      final message = e is ApiException ? e.message : 'Không thể check-in.';
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  Future<void> _checkOut() async {
    setState(() => _isSubmitting = true);
    try {
      final position = await _getCurrentPosition();
      await ref
          .read(todayAttendanceProvider.notifier)
          .checkOut(latitude: position.latitude, longitude: position.longitude);
    } catch (e) {
      if (!mounted) return;
      final message = e is ApiException ? e.message : 'Không thể check-out.';
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return DefaultTabController(
      length: 2,
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Chấm công'),
          bottom: const TabBar(tabs: [Tab(text: 'Check-in'), Tab(text: 'Lịch sử')]),
        ),
        body: TabBarView(
          children: [_buildCheckInTab(), _buildHistoryTab()],
        ),
      ),
    );
  }

  Widget _buildCheckInTab() {
    final todayAsync = ref.watch(todayAttendanceProvider);

    return todayAsync.when(
      data: (today) => Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            _buildStatusCard(today),
            const SizedBox(height: 24),
            Row(
              children: [
                Expanded(
                  child: FilledButton(
                    onPressed: _isSubmitting || (today?.isCheckedIn ?? false) ? null : _checkIn,
                    child: const Text('CHECK-IN'),
                  ),
                ),
                const SizedBox(width: 16),
                Expanded(
                  child: OutlinedButton(
                    onPressed: _isSubmitting || !(today?.isCheckedIn ?? false) || (today?.isCheckedOut ?? false)
                        ? null
                        : _checkOut,
                    child: const Text('CHECK-OUT'),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
      loading: () => const Center(child: CircularProgressIndicator()),
      error: (error, _) => ErrorStateView(
        message: error is ApiException ? error.message : 'Không tải được trạng thái chấm công.',
        onRetry: () => ref.invalidate(todayAttendanceProvider),
      ),
    );
  }

  Widget _buildStatusCard(AttendanceRecord? today) {
    final format = DateFormat('HH:mm');
    String text;
    if (today == null || !today.isCheckedIn) {
      text = 'Chưa check-in hôm nay.';
    } else if (!today.isCheckedOut) {
      text = 'Đã check-in lúc ${format.format(today.checkInAtUtc!.toLocal())} tại ${today.checkInLocationName}.';
    } else {
      text = 'Hoàn thành: ${format.format(today.checkInAtUtc!.toLocal())} → '
          '${format.format(today.checkOutAtUtc!.toLocal())}';
    }
    return Card(child: Padding(padding: const EdgeInsets.all(16), child: Text(text)));
  }

  Widget _buildHistoryTab() {
    final args = (year: _selectedMonth.year, month: _selectedMonth.month);
    final historyAsync = ref.watch(attendanceHistoryProvider(args));
    final statsAsync = ref.watch(attendanceStatsProvider(args));

    return Column(
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            IconButton(
              icon: const Icon(Icons.chevron_left),
              onPressed: () => setState(
                () => _selectedMonth = DateTime(_selectedMonth.year, _selectedMonth.month - 1),
              ),
            ),
            Text('Tháng ${_selectedMonth.month}/${_selectedMonth.year}'),
            IconButton(
              icon: const Icon(Icons.chevron_right),
              onPressed: () => setState(
                () => _selectedMonth = DateTime(_selectedMonth.year, _selectedMonth.month + 1),
              ),
            ),
          ],
        ),
        statsAsync.when(
          data: (stats) => Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: Text('${stats.daysCheckedIn} ngày đã check-in · ${stats.totalHoursWorked.toStringAsFixed(1)} giờ làm'),
          ),
          loading: () => const SizedBox.shrink(),
          error: (_, _) => const SizedBox.shrink(),
        ),
        Expanded(
          child: historyAsync.when(
            data: (records) {
              if (records.isEmpty) {
                return const EmptyStateView(icon: Icons.event_busy_outlined, message: 'Chưa có dữ liệu chấm công.');
              }
              final format = DateFormat('HH:mm');
              return ListView.separated(
                padding: const EdgeInsets.all(8),
                itemCount: records.length,
                separatorBuilder: (context, index) => const Divider(height: 1),
                itemBuilder: (context, index) {
                  final record = records[index];
                  final checkOutText = record.isCheckedOut ? format.format(record.checkOutAtUtc!.toLocal()) : '--:--';
                  return ListTile(
                    title: Text(
                      '${DateFormat('dd/MM/yyyy').format(record.workDate)}: '
                      '${format.format(record.checkInAtUtc!.toLocal())} → $checkOutText',
                    ),
                    subtitle: Text(record.checkInLocationName ?? ''),
                  );
                },
              );
            },
            loading: () => const Center(child: CircularProgressIndicator()),
            error: (error, _) => ErrorStateView(
              message: error is ApiException ? error.message : 'Không tải được lịch sử chấm công.',
              onRetry: () => ref.invalidate(attendanceHistoryProvider(args)),
            ),
          ),
        ),
      ],
    );
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd mobile/taskmgmt_app && flutter test test/attendance_screen_test.dart --concurrency=1`
Expected: PASS (3 tests).

- [ ] **Step 5: Add the entry point icon**

In `mobile/taskmgmt_app/lib/features/tasks/presentation/screens/task_list_screen.dart`, add this import:

```dart
import '../../../attendance/presentation/screens/attendance_screen.dart';
```

Then, in the `AppBar.actions` list, add this `IconButton` right before the `Icons.location_on_outlined` one:

```dart
          IconButton(
            icon: const Icon(Icons.fingerprint),
            tooltip: 'Chấm công',
            onPressed: () => context.push(AttendanceScreen.path),
          ),
```

- [ ] **Step 6: Register the route**

In `mobile/taskmgmt_app/lib/core/routing/app_router.dart`, add this import:

```dart
import '../../features/attendance/presentation/screens/attendance_screen.dart';
```

Then add this `GoRoute` to the `routes` list, after the `DashboardScreen.path` entry:

```dart
      GoRoute(
        path: AttendanceScreen.path,
        name: AttendanceScreen.name,
        builder: (context, state) => const AttendanceScreen(),
      ),
```

- [ ] **Step 7: Run the full suite**

Run:
```bash
cd mobile/taskmgmt_app
flutter analyze
flutter test --concurrency=1
```
Expected: no analyzer errors, all tests pass (existing + Tasks 10-11's new tests).

- [ ] **Step 8: Commit**

```bash
git add mobile/taskmgmt_app/lib/features/attendance/presentation/screens/ mobile/taskmgmt_app/lib/features/tasks/presentation/screens/task_list_screen.dart mobile/taskmgmt_app/lib/core/routing/app_router.dart mobile/taskmgmt_app/test/attendance_screen_test.dart
git commit -m "feat(mobile): add AttendanceScreen UI and entry point"
```

---

### Task 12: Manual verification

**Files:** none (verification only).

- [ ] **Step 1: Run the full backend and mobile suites one more time**

```bash
cd backend && dotnet build TaskMgmt.slnx && dotnet test TaskMgmt.slnx
cd mobile/taskmgmt_app && flutter analyze && flutter test --concurrency=1
```
Expected: everything green.

- [ ] **Step 2: Manually verify on an emulator/device against a running backend**

With Docker (Postgres/Redis/MinIO) and the backend API running locally, and at least one `Location` seeded in the database near the emulator's mock GPS coordinates (Android emulators default to a fixed mock location, adjustable via the emulator's extended controls):

- Open the app, tap the fingerprint icon, confirm the "Check-in" tab shows "Chưa check-in hôm nay."
- Tap CHECK-IN with the emulator's mock location within the seeded `Location`'s radius — confirm it succeeds and shows the check-in time + location name.
- Tap CHECK-IN again — confirm it's disabled (already checked in).
- Move the emulator's mock location far away (Extended Controls → Location) and try check-in on a second test account/day if possible, or verify the 400 error path directly via `curl`/Swagger against `/api/v1/attendance/check-in` with out-of-range coordinates — confirm the Vietnamese error message surfaces.
- Tap CHECK-OUT — confirm it succeeds and the status updates to "Hoàn thành".
- Switch to "Lịch sử" tab, confirm today's record appears with correct check-in/check-out times and the stats line shows `1 ngày đã check-in`.

This step has no automated equivalent (real GPS/emulator location + a live backend) — record the outcome in the SDD ledger's deviations/notes section rather than skipping it, consistent with how Task 6 of the Upload UX plan handled the same kind of gap.

---

## Self-Review Notes

**Spec coverage:**
- 1 record/user/day, unique `(UserId, WorkDate)` → Task 1.
- `WorkDate` in Vietnam time (UTC+7) → Tasks 4/5/6 all compute it identically via the same formula.
- Server-side Haversine radius validation against any active `Location` → Task 3 (`GeoDistance`) + Task 4 (`CheckInCommandHandler`).
- Check-out doesn't block on radius, best-effort location match for display → Task 5.
- History + stats (days checked in, total hours) in one screen, no separate stats tab → Task 6 (backend) + Task 11 (mobile, `_buildHistoryTab`).
- Self-only data (no userId route param, no manager view) → Task 7's controller reads `ICurrentUserService.UserId` exclusively in every handler.
- Attempt-then-error, no continuous GPS watching → Task 11's `_getCurrentPosition()` is called only inside `_checkIn`/`_checkOut`, never on build/watch.
- `geolocator` dependency + Android/iOS permissions → Task 8.
- Out-of-scope items (shifts, late/absent, leave/holiday, per-user Location assignment, manager view, approval workflow) → none implemented anywhere in this plan, matches spec §2.

**Placeholder scan:** No TBD/TODO/"add appropriate handling" phrases; every step has complete code. Task 2 Step 6 and Task 8 Step 1 give conditional-but-concrete instructions (grep-and-fix, version-floor-not-hard-pin) rather than vague hand-waving, because the exact existing call sites/latest package version can't be pinned from the spec alone — both give the exact command/rule to apply.

**Type consistency:** `AttendanceRecordDto` (Task 4) is reused with identical fields by Task 5, Task 6, and Task 7. `AttendanceStatsDto` (Task 6) matches Task 7's controller return type. `GeoDistance.CalculateMeters(double, double, double, double)` (Task 3) is called identically in Task 4 and Task 5. On mobile, `AttendanceRecord`/`AttendanceStats` (Task 9) flow unchanged through `AttendanceRepository` (Task 9) → `attendance_provider.dart` (Task 10) → `AttendanceScreen` (Task 11) with no field renames. `attendanceRepositoryProvider` is defined once in Task 10 and consumed by both its own controllers and Task 11's screen via `ref.watch`/`ref.read`, never redefined.
