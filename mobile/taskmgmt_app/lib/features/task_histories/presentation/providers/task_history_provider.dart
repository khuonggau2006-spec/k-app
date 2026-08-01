import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/di/injection.dart';
import '../../domain/entities/task_history.dart';
import '../../domain/repositories/task_history_repository.dart';

final taskHistoryRepositoryProvider = Provider<TaskHistoryRepository>((ref) => getIt<TaskHistoryRepository>());

final taskHistoryProvider =
    AsyncNotifierProvider.family<TaskHistoryController, List<TaskHistory>, String>(TaskHistoryController.new);

class TaskHistoryController extends FamilyAsyncNotifier<List<TaskHistory>, String> {
  @override
  Future<List<TaskHistory>> build(String workTaskId) {
    return ref.read(taskHistoryRepositoryProvider).getHistory(workTaskId);
  }

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() => ref.read(taskHistoryRepositoryProvider).getHistory(arg));
  }
}
