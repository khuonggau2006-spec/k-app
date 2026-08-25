import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/di/injection.dart';
import '../../../tasks/domain/entities/work_task.dart';
import '../../../tasks/presentation/providers/work_task_provider.dart';
import '../../domain/entities/dashboard_stats.dart';
import '../../domain/entities/weekly_completion.dart';
import '../../domain/repositories/dashboard_repository.dart';

final dashboardRepositoryProvider = Provider<DashboardRepository>((ref) => getIt<DashboardRepository>());

final dashboardStatsProvider = AsyncNotifierProvider<DashboardStatsController, DashboardStats>(
  DashboardStatsController.new,
);

class DashboardStatsController extends AsyncNotifier<DashboardStats> {
  @override
  Future<DashboardStats> build() => ref.read(dashboardRepositoryProvider).getStats();

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() => ref.read(dashboardRepositoryProvider).getStats());
  }
}

final weeklyCompletionStatsProvider =
    AsyncNotifierProvider<WeeklyCompletionStatsController, List<WeeklyCompletion>>(
  WeeklyCompletionStatsController.new,
);

class WeeklyCompletionStatsController extends AsyncNotifier<List<WeeklyCompletion>> {
  @override
  Future<List<WeeklyCompletion>> build() => ref.read(dashboardRepositoryProvider).getWeeklyCompletion();

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() => ref.read(dashboardRepositoryProvider).getWeeklyCompletion());
  }
}

/// Danh sách công việc cho board Kanban - độc lập với bộ lọc của màn hình danh sách
/// (workTaskFilterProvider) để không bị ảnh hưởng lẫn nhau.
final dashboardTasksProvider = AsyncNotifierProvider<DashboardTasksController, List<WorkTask>>(
  DashboardTasksController.new,
);

class DashboardTasksController extends AsyncNotifier<List<WorkTask>> {
  @override
  Future<List<WorkTask>> build() async {
    final result = await ref.read(workTaskRepositoryProvider).getTasks();
    return result.items;
  }

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() async {
      final result = await ref.read(workTaskRepositoryProvider).getTasks();
      return result.items;
    });
  }
}
