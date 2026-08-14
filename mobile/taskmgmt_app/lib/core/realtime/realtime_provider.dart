import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../features/auth/presentation/providers/auth_provider.dart';
import '../../features/notifications/presentation/providers/notification_provider.dart';
import '../di/injection.dart';
import 'realtime_service.dart';

final realtimeServiceProvider = Provider<RealtimeService>((ref) => getIt<RealtimeService>());

/// Đọc provider này một lần ở gốc widget tree (xem [TaskMgmtApp]) để kích hoạt việc lắng nghe
/// vòng đời đăng nhập - có user thì connect tới Hub, mất user (đăng xuất hoặc phiên hết hạn)
/// thì disconnect, cùng cách làm với pushNotificationServiceProvider. Đồng thời lắng nghe MỌI
/// TaskUpdated (không lọc theo task cụ thể như workTaskRealtimeProvider) để làm mới số đếm chưa
/// đọc - server ghi Notification cùng lúc phát TaskUpdated (xem TaskNotificationHelper.NotifyAsync
/// phía backend) nên đây là tín hiệu hợp lý để badge cập nhật gần như tức thời khi app đang mở.
final realtimeConnectionProvider = Provider<void>((ref) {
  final service = ref.watch(realtimeServiceProvider);

  ref.listen(authControllerProvider, (previous, next) {
    final user = next.valueOrNull;
    if (user != null) {
      service.connect();
    } else if (previous?.valueOrNull != null) {
      service.disconnect();
    }
  });

  final subscription = service.taskUpdates.listen((_) {
    ref.invalidate(unreadNotificationCountProvider);
  });
  ref.onDispose(subscription.cancel);
});
