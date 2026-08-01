import '../entities/task_history.dart';

abstract class TaskHistoryRepository {
  Future<List<TaskHistory>> getHistory(String workTaskId);
}
