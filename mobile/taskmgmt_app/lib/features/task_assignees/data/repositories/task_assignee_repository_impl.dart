import '../../domain/entities/task_assignee.dart';
import '../../domain/repositories/task_assignee_repository.dart';
import '../datasources/task_assignee_remote_data_source.dart';

class TaskAssigneeRepositoryImpl implements TaskAssigneeRepository {
  TaskAssigneeRepositoryImpl(this._remoteDataSource);

  final TaskAssigneeRemoteDataSource _remoteDataSource;

  @override
  Future<List<TaskAssignee>> getAssignees(String workTaskId) async {
    final models = await _remoteDataSource.getAssignees(workTaskId);
    return models.map((model) => model.toDomain()).toList();
  }

  @override
  Future<TaskAssignee> addAssignee({
    required String workTaskId,
    required String userId,
    required TaskAssigneeRole role,
  }) async {
    final model = await _remoteDataSource.addAssignee(workTaskId: workTaskId, userId: userId, role: role);
    return model.toDomain();
  }

  @override
  Future<TaskAssignee> changeRole({
    required String workTaskId,
    required String userId,
    required TaskAssigneeRole role,
  }) async {
    final model = await _remoteDataSource.changeRole(workTaskId: workTaskId, userId: userId, role: role);
    return model.toDomain();
  }

  @override
  Future<void> removeAssignee({required String workTaskId, required String userId}) =>
      _remoteDataSource.removeAssignee(workTaskId: workTaskId, userId: userId);
}
