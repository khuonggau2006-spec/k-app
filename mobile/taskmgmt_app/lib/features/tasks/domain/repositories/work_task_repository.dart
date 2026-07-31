import '../../../../core/models/paged_result.dart';
import '../entities/work_task.dart';

abstract class WorkTaskRepository {
  Future<PagedResult<WorkTask>> getTasks({WorkTaskStatus? status, String? locationId, String? parentTaskId});

  Future<WorkTask> getTaskById(String id);

  Future<WorkTask> createTask({
    required String title,
    String? description,
    DateTime? dueDateUtc,
    String? locationId,
    String? parentTaskId,
  });

  Future<WorkTask> updateTask({
    required String id,
    required String title,
    String? description,
    required WorkTaskStatus status,
    DateTime? dueDateUtc,
    String? locationId,
  });

  Future<void> deleteTask(String id);
}
