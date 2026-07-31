import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/di/injection.dart';
import '../../domain/entities/task_assignee.dart';
import '../../domain/repositories/task_assignee_repository.dart';

final taskAssigneeRepositoryProvider =
    Provider<TaskAssigneeRepository>((ref) => getIt<TaskAssigneeRepository>());

final taskAssigneesProvider =
    AsyncNotifierProvider.family<TaskAssigneesController, List<TaskAssignee>, String>(TaskAssigneesController.new);

class TaskAssigneesController extends FamilyAsyncNotifier<List<TaskAssignee>, String> {
  @override
  Future<List<TaskAssignee>> build(String workTaskId) {
    return ref.read(taskAssigneeRepositoryProvider).getAssignees(workTaskId);
  }

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() => ref.read(taskAssigneeRepositoryProvider).getAssignees(arg));
  }

  Future<void> addAssignee({required String userId, required TaskAssigneeRole role}) async {
    await ref.read(taskAssigneeRepositoryProvider).addAssignee(workTaskId: arg, userId: userId, role: role);
    await refresh();
  }

  Future<void> changeRole({required String userId, required TaskAssigneeRole role}) async {
    await ref.read(taskAssigneeRepositoryProvider).changeRole(workTaskId: arg, userId: userId, role: role);
    await refresh();
  }

  Future<void> removeAssignee(String userId) async {
    await ref.read(taskAssigneeRepositoryProvider).removeAssignee(workTaskId: arg, userId: userId);
    await refresh();
  }
}
