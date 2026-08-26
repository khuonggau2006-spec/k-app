import 'dart:typed_data';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:taskmgmt_app/features/auth/domain/entities/user.dart';
import 'package:taskmgmt_app/features/users/domain/repositories/user_repository.dart';
import 'package:taskmgmt_app/features/users/presentation/providers/user_provider.dart';

class _FakeUserRepository implements UserRepository {
  @override
  Future<List<User>> getUsers() async => [];

  @override
  Future<Uint8List?> downloadAvatar(String userId) async =>
      userId == 'has-avatar' ? Uint8List.fromList([1, 2, 3]) : null;

  @override
  Future<User> uploadAvatar({required Uint8List bytes, required String fileName}) async => throw UnimplementedError();

  @override
  Future<User> deleteAvatar() async => throw UnimplementedError();
}

void main() {
  test('avatarBytesProvider returns bytes for a user with an avatar', () async {
    final container = ProviderContainer(
      overrides: [userRepositoryProvider.overrideWithValue(_FakeUserRepository())],
    );
    addTearDown(container.dispose);

    final bytes = await container.read(avatarBytesProvider('has-avatar').future);

    expect(bytes, Uint8List.fromList([1, 2, 3]));
  });

  test('avatarBytesProvider returns null for a user without an avatar', () async {
    final container = ProviderContainer(
      overrides: [userRepositoryProvider.overrideWithValue(_FakeUserRepository())],
    );
    addTearDown(container.dispose);

    final bytes = await container.read(avatarBytesProvider('no-avatar').future);

    expect(bytes, isNull);
  });
}
