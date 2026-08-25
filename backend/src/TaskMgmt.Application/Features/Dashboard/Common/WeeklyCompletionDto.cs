namespace TaskMgmt.Application.Features.Dashboard.Common;

public record WeeklyCompletionDto(DateOnly WeekStartDate, int CompletedCount);
