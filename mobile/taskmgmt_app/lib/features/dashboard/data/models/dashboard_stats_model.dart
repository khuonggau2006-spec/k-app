import '../../domain/entities/dashboard_stats.dart';

class DashboardStatsModel {
  const DashboardStatsModel({
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

  factory DashboardStatsModel.fromJson(Map<String, dynamic> json) => DashboardStatsModel(
        totalActive: json['totalActive'] as int,
        toDoCount: json['toDoCount'] as int,
        inProgressCount: json['inProgressCount'] as int,
        inReviewCount: json['inReviewCount'] as int,
        doneCount: json['doneCount'] as int,
        cancelledCount: json['cancelledCount'] as int,
        overdueCount: json['overdueCount'] as int,
        dueSoonCount: json['dueSoonCount'] as int,
      );

  DashboardStats toDomain() => DashboardStats(
        totalActive: totalActive,
        toDoCount: toDoCount,
        inProgressCount: inProgressCount,
        inReviewCount: inReviewCount,
        doneCount: doneCount,
        cancelledCount: cancelledCount,
        overdueCount: overdueCount,
        dueSoonCount: dueSoonCount,
      );
}
