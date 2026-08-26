import 'dart:typed_data';

import '../../../auth/domain/entities/user.dart';

abstract class UserRepository {
  Future<List<User>> getUsers();

  Future<Uint8List?> downloadAvatar(String userId);

  Future<User> uploadAvatar({required Uint8List bytes, required String fileName});

  Future<User> deleteAvatar();
}
