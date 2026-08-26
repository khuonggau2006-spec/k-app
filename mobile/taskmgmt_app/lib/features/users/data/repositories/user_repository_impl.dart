import 'dart:typed_data';

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

  @override
  Future<Uint8List?> downloadAvatar(String userId) => _remoteDataSource.downloadAvatar(userId);

  @override
  Future<User> uploadAvatar({required Uint8List bytes, required String fileName}) async {
    final model = await _remoteDataSource.uploadAvatar(bytes: bytes, fileName: fileName);
    return model.toDomain();
  }

  @override
  Future<User> deleteAvatar() async {
    final model = await _remoteDataSource.deleteAvatar();
    return model.toDomain();
  }
}
