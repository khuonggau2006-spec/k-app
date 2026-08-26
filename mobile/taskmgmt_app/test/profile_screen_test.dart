import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:get_it/get_it.dart';

import 'package:taskmgmt_app/core/network/auth_event_bus.dart';
import 'package:taskmgmt_app/features/auth/domain/entities/user.dart';
import 'package:taskmgmt_app/features/auth/domain/repositories/auth_repository.dart';
import 'package:taskmgmt_app/features/auth/presentation/providers/auth_provider.dart';
import 'package:taskmgmt_app/features/users/domain/repositories/user_repository.dart';
import 'package:taskmgmt_app/features/users/presentation/providers/user_provider.dart';
import 'package:taskmgmt_app/features/users/presentation/screens/profile_screen.dart';

const _userWithAvatar = User(
  id: '1',
  email: 'a@b.com',
  fullName: 'Nguyễn Test',
  systemRole: SystemRole.member,
  hasAvatar: true,
);

const _userWithoutAvatar = User(
  id: '1',
  email: 'a@b.com',
  fullName: 'Nguyễn Test',
  systemRole: SystemRole.member,
  hasAvatar: false,
);

class _FakeAuthRepository implements AuthRepository {
  _FakeAuthRepository(this.initialUser);

  final User initialUser;

  @override
  Future<User?> restoreSession() async => initialUser;

  @override
  Future<User> login({required String email, required String password}) async => throw UnimplementedError();

  @override
  Future<User> register({required String email, required String fullName, required String password}) async =>
      throw UnimplementedError();

  @override
  Future<void> logout() async {}
}

class _FakeUserRepository implements UserRepository {
  _FakeUserRepository();

  bool deleteAvatarCalled = false;

  @override
  Future<List<User>> getUsers() async => [];

  @override
  Future<Uint8List?> downloadAvatar(String userId) async => null;

  @override
  Future<User> uploadAvatar({required Uint8List bytes, required String fileName}) async => throw UnimplementedError();

  @override
  Future<User> deleteAvatar() async {
    deleteAvatarCalled = true;
    return _userWithoutAvatar;
  }
}

Widget _buildScreen(User user, UserRepository userRepository) => ProviderScope(
      overrides: [
        authRepositoryProvider.overrideWithValue(_FakeAuthRepository(user)),
        userRepositoryProvider.overrideWithValue(userRepository),
      ],
      child: const MaterialApp(home: ProfileScreen()),
    );

void main() {
  setUp(() {
    // AuthController.build() gọi getIt<AuthEventBus>() - đăng ký thủ công vì test không gọi
    // setupLocator() (tránh phải fake toàn bộ cây phụ thuộc DI thật).
    GetIt.instance.registerLazySingleton<AuthEventBus>(() => AuthEventBus());
  });

  tearDown(() => GetIt.instance.reset());

  testWidgets('Shows full name, email, and delete button when user has an avatar', (tester) async {
    await tester.pumpWidget(_buildScreen(_userWithAvatar, _FakeUserRepository()));
    await tester.pumpAndSettle();

    expect(find.text('Nguyễn Test'), findsOneWidget);
    expect(find.text('a@b.com'), findsOneWidget);
    expect(find.text('Xoá avatar'), findsOneWidget);
  });

  testWidgets('Hides delete button when user has no avatar', (tester) async {
    await tester.pumpWidget(_buildScreen(_userWithoutAvatar, _FakeUserRepository()));
    await tester.pumpAndSettle();

    expect(find.text('Xoá avatar'), findsNothing);
  });

  testWidgets('Tapping delete avatar calls the repository', (tester) async {
    final userRepository = _FakeUserRepository();
    await tester.pumpWidget(_buildScreen(_userWithAvatar, userRepository));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Xoá avatar'));
    await tester.pumpAndSettle();

    expect(userRepository.deleteAvatarCalled, isTrue);
    expect(find.text('Xoá avatar'), findsNothing);
  });
}
