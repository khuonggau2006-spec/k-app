import '../../../../core/models/paged_result.dart';
import '../../domain/entities/work_task.dart';
import '../../domain/repositories/work_task_repository.dart';
import '../datasources/work_task_remote_data_source.dart';

class WorkTaskRepositoryImpl implements WorkTaskRepository {
  WorkTaskRepositoryImpl(this._remoteDataSource);

  final WorkTaskRemoteDataSource _remoteDataSource;

  @override
  Future<PagedResult<WorkTask>> getTasks({WorkTaskStatus? status, String? locationId, String? parentTaskId}) async {
    final result = await _remoteDataSource.getTasks(status: status, locationId: locationId, parentTaskId: parentTaskId);
    return PagedResult(
      items: result.items.map((model) => model.toDomain()).toList(),
      pageNumber: result.pageNumber,
      pageSize: result.pageSize,
      totalCount: result.totalCount,
      totalPages: result.totalPages,
    );
  }

  @override
  Future<WorkTask> getTaskById(String id) async {
    final model = await _remoteDataSource.getTaskById(id);
    return model.toDomain();
  }

  @override
  Future<WorkTask> createTask({
    required String title,
    String? description,
    DateTime? dueDateUtc,
    String? locationId,
    String? parentTaskId,
  }) async {
    final model = await _remoteDataSource.createTask(
      title: title,
      description: description,
      dueDateUtc: dueDateUtc,
      locationId: locationId,
      parentTaskId: parentTaskId,
    );
    return model.toDomain();
  }

  @override
  Future<WorkTask> updateTask({
    required String id,
    required String title,
    String? description,
    required WorkTaskStatus status,
    DateTime? dueDateUtc,
    String? locationId,
  }) async {
    final model = await _remoteDataSource.updateTask(
      id: id,
      title: title,
      description: description,
      status: status,
      dueDateUtc: dueDateUtc,
      locationId: locationId,
    );
    return model.toDomain();
  }

  @override
  Future<void> deleteTask(String id) => _remoteDataSource.deleteTask(id);
}
