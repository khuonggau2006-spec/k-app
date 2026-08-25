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
