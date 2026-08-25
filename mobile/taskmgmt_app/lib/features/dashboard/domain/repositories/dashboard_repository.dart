import '../entities/dashboard_stats.dart';
import '../entities/weekly_completion.dart';

abstract class DashboardRepository {
  Future<DashboardStats> getStats({String? locationId});

  Future<List<WeeklyCompletion>> getWeeklyCompletion({String? locationId});
}
