import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../shared/widgets/error_state_view.dart';
import '../providers/dashboard_provider.dart';
import '../widgets/dashboard_stat_grid.dart';
import '../widgets/work_task_kanban_board.dart';

class DashboardScreen extends ConsumerWidget {
  const DashboardScreen({super.key});

  static const path = '/dashboard';
  static const name = 'dashboard';

  Future<void> _refresh(WidgetRef ref) async {
    await Future.wait([
      ref.read(dashboardStatsProvider.notifier).refresh(),
      ref.read(dashboardTasksProvider.notifier).refresh(),
    ]);
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final statsAsync = ref.watch(dashboardStatsProvider);
    final tasksAsync = ref.watch(dashboardTasksProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Dashboard')),
      body: RefreshIndicator(
        onRefresh: () => _refresh(ref),
        child: ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.all(16),
          children: [
            statsAsync.when(
              data: (stats) => DashboardStatGrid(stats: stats),
              loading: () => const SizedBox(height: 120, child: Center(child: CircularProgressIndicator())),
              error: (error, _) => ErrorStateView(
                message: error is ApiException ? error.message : 'Không thể tải thống kê.',
                onRetry: () => ref.read(dashboardStatsProvider.notifier).refresh(),
              ),
            ),
            const SizedBox(height: 24),
            Text('Công việc theo trạng thái', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 12),
            tasksAsync.when(
              data: (tasks) => SingleChildScrollView(
                scrollDirection: Axis.horizontal,
                child: WorkTaskKanbanBoard(
                  tasks: tasks,
                  onTaskTap: (task) => context.push('/tasks/${task.id}'),
                ),
              ),
              loading: () => const SizedBox(height: 200, child: Center(child: CircularProgressIndicator())),
              error: (error, _) => ErrorStateView(
                message: error is ApiException ? error.message : 'Không thể tải danh sách công việc.',
                onRetry: () => ref.read(dashboardTasksProvider.notifier).refresh(),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
