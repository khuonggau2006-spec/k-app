import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../features/auth/presentation/providers/auth_provider.dart';
import '../di/injection.dart';
import '../routing/app_router.dart';
import 'push_notification_service.dart';

/// Đọc provider này một lần ở gốc widget tree (xem [TaskMgmtApp]) để kích hoạt việc lắng nghe
/// vòng đời đăng nhập - có user thì khởi tạo push + đăng ký FCM token, mất user (đăng xuất hoặc
/// phiên hết hạn) thì gỡ token khỏi backend.
final pushNotificationServiceProvider = Provider<PushNotificationService>((ref) {
  final service = PushNotificationService(getIt(), ref.read(routerProvider));

  ref.listen(authControllerProvider, (previous, next) {
    final user = next.valueOrNull;
    if (user != null) {
      service.initialize();
    } else if (previous?.valueOrNull != null) {
      service.unregisterCurrentToken();
    }
  });

  return service;
});
