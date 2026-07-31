import '../../domain/entities/location.dart';
import '../../domain/repositories/location_repository.dart';
import '../datasources/location_remote_data_source.dart';

class LocationRepositoryImpl implements LocationRepository {
  LocationRepositoryImpl(this._remoteDataSource);

  final LocationRemoteDataSource _remoteDataSource;

  @override
  Future<List<Location>> getLocations() async {
    final models = await _remoteDataSource.getLocations();
    return models.map((model) => model.toDomain()).toList();
  }
}
