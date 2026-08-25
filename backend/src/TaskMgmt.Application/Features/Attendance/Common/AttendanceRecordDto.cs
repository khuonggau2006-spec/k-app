namespace TaskMgmt.Application.Features.Attendance.Common;

public record AttendanceRecordDto(
    Guid Id,
    DateOnly WorkDate,
    DateTimeOffset? CheckInAtUtc,
    string? CheckInLocationName,
    DateTimeOffset? CheckOutAtUtc,
    string? CheckOutLocationName);
