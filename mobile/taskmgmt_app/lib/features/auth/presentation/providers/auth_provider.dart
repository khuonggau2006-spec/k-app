import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/di/injection.dart';
import '../../../../core/network/auth_event_bus.dart';
import '../../domain/entities/user.dart';
import '../../domain/repositories/auth_repository.dart';

final authRepositoryProvider = Provider<AuthRepository>((ref) => getIt<AuthRepository>());

final authControllerProvider = AsyncNotifierProvider<AuthController, User?>(AuthController.new);

class AuthController extends AsyncNotifier<User?> {
  @override
  Future<User?> build() {
    final subscription = getIt<AuthEventBus>().onSessionExpired.listen((_) => _handleSessionExpired());
    ref.onDispose(subscription.cancel);

    return ref.read(authRepositoryProvider).restoreSession();
  }

  // Access token hết hạn giữa phiên làm việc (401 không body từ JWT Bearer middleware):
  // dọn session cục bộ để router tự điều hướng người dùng về màn hình đăng nhập.
  Future<void> _handleSessionExpired() async {
    await ref.read(authRepositoryProvider).logout();
    state = const AsyncData(null);
  }

  Future<void> login({required String email, required String password}) async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(
      () => ref.read(authRepositoryProvider).login(email: email, password: password),
    );
  }

  Future<void> register({required String email, required String fullName, required String password}) async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(
      () => ref.read(authRepositoryProvider).register(email: email, fullName: fullName, password: password),
    );
  }

  Future<void> logout() async {
    await ref.read(authRepositoryProvider).logout();
    state = const AsyncData(null);
  }

  // Gọi sau khi upload/xoá avatar thành công (users_provider.dart) để phản ánh ngay hasAvatar
  // mới trên Home/Profile mà không cần gọi lại API xác thực.
  void updateUser(User user) {
    state = AsyncData(user);
  }
}
