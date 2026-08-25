import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:taskmgmt_app/core/models/paged_result.dart';
import 'package:taskmgmt_app/features/dashboard/domain/entities/dashboard_stats.dart';
import 'package:taskmgmt_app/features/dashboard/domain/entities/weekly_completion.dart';
import 'package:taskmgmt_app/features/dashboard/domain/repositories/dashboard_repository.dart';
import 'package:taskmgmt_app/features/dashboard/presentation/providers/dashboard_provider.dart';
import 'package:taskmgmt_app/features/dashboard/presentation/screens/dashboard_screen.dart';
import 'package:taskmgmt_app/features/tasks/domain/entities/work_task.dart';
import 'package:taskmgmt_app/features/tasks/domain/repositories/work_task_repository.dart';
import 'package:taskmgmt_app/features/tasks/presentation/providers/work_task_provider.dart';

const _stats = DashboardStats(
  totalActive: 5,
  toDoCount: 2,
  inProgressCount: 1,
  inReviewCount: 0,
  doneCount: 1,
  cancelledCount: 1,
  overdueCount: 1,
  dueSoonCount: 1,
);

class _FakeDashboardRepository implements DashboardRepository {
  @override
  Future<DashboardStats> getStats({String? locationId}) async => _stats;

  @override
  Future<List<WeeklyCompletion>> getWeeklyCompletion({String? locationId}) async => [
        WeeklyCompletion(weekStartDate: DateTime(2026, 8, 3), completedCount: 1),
      ];
}

class _FakeWorkTaskRepository implements WorkTaskRepository {
  final List<WorkTask> tasks = [
    const WorkTask(
      id: 't1',
      title: 'Việc cần làm',
      description: null,
      status: WorkTaskStatus.toDo,
      dueDateUtc: null,
      parentTaskId: null,
      locationId: null,
    ),
    const WorkTask(
      id: 't2',
      title: 'Việc đang làm',
      description: null,
      status: WorkTaskStatus.inProgress,
      dueDateUtc: null,
      parentTaskId: null,
      locationId: null,
    ),
  ];

  @override
  Future<PagedResult<WorkTask>> getTasks({WorkTaskStatus? status, String? locationId, String? parentTaskId}) async {
    final filtered = status == null ? tasks : tasks.where((t) => t.status == status).toList();
    return PagedResult(items: filtered, pageNumber: 1, pageSize: 50, totalCount: filtered.length, totalPages: 1);
  }

  @override
  Future<WorkTask> getTaskById(String id) async => tasks.firstWhere((t) => t.id == id);

  @override
  Future<WorkTask> createTask({
    required String title,
    String? description,
    DateTime? dueDateUtc,
    String? locationId,
    String? parentTaskId,
  }) async =>
      throw UnimplementedError();

  @override
  Future<WorkTask> updateTask({
    required String id,
    required String title,
    String? description,
    required WorkTaskStatus status,
    DateTime? dueDateUtc,
    String? locationId,
  }) async =>
      throw UnimplementedError();

  @override
  Future<void> deleteTask(String id) async => throw UnimplementedError();
}

Widget _buildScreen() => ProviderScope(
      overrides: [
        dashboardRepositoryProvider.overrideWithValue(_FakeDashboardRepository()),
        workTaskRepositoryProvider.overrideWithValue(_FakeWorkTaskRepository()),
      ],
      child: const MaterialApp(home: DashboardScreen()),
    );

void main() {
  testWidgets('Shows quick stats and kanban columns grouped by status', (tester) async {
    // Màn hình test mặc định không đủ cao để dựng hết ListView (thống kê + biểu đồ tuần +
    // kanban) trong 1 lần build - tăng chiều cao viewport ảo để mọi phần đều được dựng.
    tester.view.physicalSize = const Size(400, 1600);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(_buildScreen());
    await tester.pumpAndSettle();

    expect(find.text('5'), findsOneWidget);
    expect(find.text('Tổng cộng'), findsOneWidget);
    expect(find.text('Quá hạn'), findsOneWidget);
    expect(find.text('Sắp đến hạn'), findsOneWidget);

    expect(find.text('Cần làm'), findsOneWidget);
    expect(find.text('Đang làm'), findsOneWidget);
    expect(find.text('Việc cần làm'), findsOneWidget);
    expect(find.text('Việc đang làm'), findsOneWidget);
  });
}
