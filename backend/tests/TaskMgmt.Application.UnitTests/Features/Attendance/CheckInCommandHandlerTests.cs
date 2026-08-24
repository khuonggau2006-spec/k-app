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
