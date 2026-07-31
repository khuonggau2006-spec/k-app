import '../entities/task_assignee.dart';

abstract class TaskAssigneeRepository {
  Future<List<TaskAssignee>> getAssignees(String workTaskId);

  Future<TaskAssignee> addAssignee({
    required String workTaskId,
    required String userId,
    required TaskAssigneeRole role,
  });

  Future<TaskAssignee> changeRole({
    required String workTaskId,
    required String userId,
    required TaskAssigneeRole role,
  });

  Future<void> removeAssignee({required String workTaskId, required String userId});
}
