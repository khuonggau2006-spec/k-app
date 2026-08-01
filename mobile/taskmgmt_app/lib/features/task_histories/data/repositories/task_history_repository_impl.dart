import '../../domain/entities/task_history.dart';
import '../../domain/repositories/task_history_repository.dart';
import '../datasources/task_history_remote_data_source.dart';

class TaskHistoryRepositoryImpl implements TaskHistoryRepository {
  TaskHistoryRepositoryImpl(this._remoteDataSource);

  final TaskHistoryRemoteDataSource _remoteDataSource;

  @override
  Future<List<TaskHistory>> getHistory(String workTaskId) async {
    final models = await _remoteDataSource.getHistory(workTaskId);
    return models.map((model) => model.toDomain()).toList();
  }
}
