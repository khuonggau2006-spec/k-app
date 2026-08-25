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
