class DashboardStats {
  const DashboardStats({
    required this.totalActive,
    required this.toDoCount,
    required this.inProgressCount,
    required this.inReviewCount,
    required this.doneCount,
    required this.cancelledCount,
    required this.overdueCount,
    required this.dueSoonCount,
  });

  final int totalActive;
  final int toDoCount;
  final int inProgressCount;
  final int inReviewCount;
  final int doneCount;
  final int cancelledCount;
  final int overdueCount;
  final int dueSoonCount;
}
