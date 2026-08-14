namespace TaskMgmt.Application.Features.Dashboard.Common;

public record DashboardStatsDto(
    int TotalActive,
    int ToDoCount,
    int InProgressCount,
    int InReviewCount,
    int DoneCount,
    int CancelledCount,
    int OverdueCount,
    int DueSoonCount);
