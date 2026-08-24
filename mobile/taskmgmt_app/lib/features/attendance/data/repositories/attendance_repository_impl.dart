import '../../domain/entities/attendance_record.dart';
import '../../domain/entities/attendance_stats.dart';
import '../../domain/repositories/attendance_repository.dart';
import '../datasources/attendance_remote_data_source.dart';

class AttendanceRepositoryImpl implements AttendanceRepository {
  AttendanceRepositoryImpl(this._remoteDataSource);

  final AttendanceRemoteDataSource _remoteDataSource;

  @override
  Future<AttendanceRecord> checkIn({required double latitude, required double longitude}) async {
    final model = await _remoteDataSource.checkIn(latitude: latitude, longitude: longitude);
    return model.toDomain();
  }

  @override
  Future<AttendanceRecord> checkOut({required double latitude, required double longitude}) async {
    final model = await _remoteDataSource.checkOut(latitude: latitude, longitude: longitude);
    return model.toDomain();
  }

  @override
  Future<AttendanceRecord?> getToday() async {
    final model = await _remoteDataSource.getToday();
    return model?.toDomain();
  }

  @override
  Future<List<AttendanceRecord>> getHistory({required int year, required int month}) async {
    final models = await _remoteDataSource.getHistory(year: year, month: month);
    return models.map((model) => model.toDomain()).toList();
  }

  @override
  Future<AttendanceStats> getStats({required int year, required int month}) async {
    final model = await _remoteDataSource.getStats(year: year, month: month);
    return model.toDomain();
  }
}
