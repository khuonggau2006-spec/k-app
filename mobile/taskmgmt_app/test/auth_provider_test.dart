import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:get_it/get_it.dart';

import 'package:taskmgmt_app/core/network/auth_event_bus.dart';
import 'package:taskmgmt_app/features/auth/domain/entities/user.dart';
import 'package:taskmgmt_app/features/auth/domain/repositories/auth_repository.dart';
import 'package:taskmgmt_app/features/auth/presentation/providers/auth_provider.dart';

class _FakeAuthRepository implements AuthRepository {
  @override
  Future<User?> restoreSession() async => null;

  @override
  Future<User> login({required String email, required String password}) async => throw UnimplementedError();

  @override
  Future<User> register({required String email, required String fullName, required String password}) async =>
      throw UnimplementedError();

  @override
  Future<void> logout() async {}
}

void main() {
  setUp(() {
    // AuthController.build() gọi getIt<AuthEventBus>() - đăng ký thủ công vì test không gọi
    // setupLocator() (tránh phải fake toàn bộ cây phụ thuộc DI thật).
    GetIt.instance.registerLazySingleton<AuthEventBus>(() => AuthEventBus());
  });

  tearDown(() => GetIt.instance.reset());

  test('updateUser replaces auth state with the given user', () async {
    final container = ProviderContainer(
      overrides: [authRepositoryProvider.overrideWithValue(_FakeAuthRepository())],
    );
    addTearDown(container.dispose);

    await container.read(authControllerProvider.future);

    const updated = User(id: '1', email: 'a@b.com', fullName: 'A', systemRole: SystemRole.member, hasAvatar: true);
    container.read(authControllerProvider.notifier).updateUser(updated);

    expect(container.read(authControllerProvider).valueOrNull, updated);
  });
}
