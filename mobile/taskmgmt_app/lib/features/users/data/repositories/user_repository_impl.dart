import '../../../auth/domain/entities/user.dart';
import '../../domain/repositories/user_repository.dart';
import '../datasources/user_remote_data_source.dart';

class UserRepositoryImpl implements UserRepository {
  UserRepositoryImpl(this._remoteDataSource);

  final UserRemoteDataSource _remoteDataSource;

  @override
  Future<List<User>> getUsers() async {
    final models = await _remoteDataSource.getUsers();
    return models.map((model) => model.toDomain()).toList();
  }
}
