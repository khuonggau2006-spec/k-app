import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/di/injection.dart';
import '../../../../core/models/paged_result.dart';
import '../../domain/entities/work_task.dart';
import '../../domain/repositories/work_task_repository.dart';

class WorkTaskFilter {
  const WorkTaskFilter({this.status, this.locationId});

  final WorkTaskStatus? status;
  final String? locationId;

  WorkTaskFilter copyWith({
    WorkTaskStatus? status,
    bool clearStatus = false,
    String? locationId,
    bool clearLocationId = false,
  }) =>
      WorkTaskFilter(
        status: clearStatus ? null : (status ?? this.status),
        locationId: clearLocationId ? null : (locationId ?? this.locationId),
      );
}

final workTaskFilterProvider = StateProvider<WorkTaskFilter>((ref) => const WorkTaskFilter());

final workTaskRepositoryProvider = Provider<WorkTaskRepository>((ref) => getIt<WorkTaskRepository>());

final workTasksProvider = AsyncNotifierProvider<WorkTasksController, PagedResult<WorkTask>>(WorkTasksController.new);

class WorkTasksController extends AsyncNotifier<PagedResult<WorkTask>> {
  @override
  Future<PagedResult<WorkTask>> build() {
    final filter = ref.watch(workTaskFilterProvider);
    return ref.read(workTaskRepositoryProvider).getTasks(status: filter.status, locationId: filter.locationId);
  }

  Future<void> refresh() async {
    final filter = ref.read(workTaskFilterProvider);
    state = const AsyncLoading();
    state = await AsyncValue.guard(
      () => ref.read(workTaskRepositoryProvider).getTasks(status: filter.status, locationId: filter.locationId),
    );
  }

  Future<void> createTask({
    required String title,
    String? description,
    DateTime? dueDateUtc,
    String? locationId,
    String? parentTaskId,
  }) async {
    await ref.read(workTaskRepositoryProvider).createTask(
          title: title,
          description: description,
          dueDateUtc: dueDateUtc,
          locationId: locationId,
          parentTaskId: parentTaskId,
        );
    await refresh();
  }

  Future<void> updateTask({
    required String id,
    required String title,
    String? description,
    required WorkTaskStatus status,
    DateTime? dueDateUtc,
    String? locationId,
  }) async {
    await ref.read(workTaskRepositoryProvider).updateTask(
          id: id,
          title: title,
          description: description,
          status: status,
          dueDateUtc: dueDateUtc,
          locationId: locationId,
        );
    await refresh();
  }

  Future<void> deleteTask(String id) async {
    await ref.read(workTaskRepositoryProvider).deleteTask(id);
    await refresh();
  }
}

/// Chi tiết 1 công việc theo id — dùng cho màn hình chi tiết.
final taskDetailProvider = AsyncNotifierProvider.family<TaskDetailController, WorkTask, String>(TaskDetailController.new);

class TaskDetailController extends FamilyAsyncNotifier<WorkTask, String> {
  @override
  Future<WorkTask> build(String taskId) {
    return ref.read(workTaskRepositoryProvider).getTaskById(taskId);
  }

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() => ref.read(workTaskRepositoryProvider).getTaskById(arg));
  }
}

/// Danh sách công việc con trực tiếp của 1 công việc — dùng cho "cây công việc con".
final taskChildrenProvider =
    AsyncNotifierProvider.family<TaskChildrenController, List<WorkTask>, String>(TaskChildrenController.new);

class TaskChildrenController extends FamilyAsyncNotifier<List<WorkTask>, String> {
  @override
  Future<List<WorkTask>> build(String parentTaskId) async {
    final result = await ref.read(workTaskRepositoryProvider).getTasks(parentTaskId: parentTaskId);
    return result.items;
  }

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() async {
      final result = await ref.read(workTaskRepositoryProvider).getTasks(parentTaskId: arg);
      return result.items;
    });
  }
}
