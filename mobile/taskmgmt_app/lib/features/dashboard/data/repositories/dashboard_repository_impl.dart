import '../../domain/entities/dashboard_stats.dart';
import '../../domain/entities/weekly_completion.dart';
import '../../domain/repositories/dashboard_repository.dart';
import '../datasources/dashboard_remote_data_source.dart';

class DashboardRepositoryImpl implements DashboardRepository {
  DashboardRepositoryImpl(this._remoteDataSource);

  final DashboardRemoteDataSource _remoteDataSource;

  @override
  Future<DashboardStats> getStats({String? locationId}) async {
    final model = await _remoteDataSource.getStats(locationId: locationId);
    return model.toDomain();
  }

  @override
  Future<List<WeeklyCompletion>> getWeeklyCompletion({String? locationId}) async {
    final models = await _remoteDataSource.getWeeklyCompletion(locationId: locationId);
    return models.map((model) => model.toDomain()).toList();
  }
}
