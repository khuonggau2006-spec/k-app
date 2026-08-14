import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/di/injection.dart';
import '../../../../core/models/paged_result.dart';
import '../../domain/entities/notification.dart';
import '../../domain/repositories/notification_repository.dart';

final notificationRepositoryProvider = Provider<NotificationRepository>((ref) => getIt<NotificationRepository>());

final notificationsUnreadOnlyProvider = StateProvider<bool>((ref) => false);

final notificationsProvider =
    AsyncNotifierProvider<NotificationsController, PagedResult<AppNotification>>(NotificationsController.new);

class NotificationsController extends AsyncNotifier<PagedResult<AppNotification>> {
  @override
  Future<PagedResult<AppNotification>> build() {
    final unreadOnly = ref.watch(notificationsUnreadOnlyProvider);
    return ref.read(notificationRepositoryProvider).getNotifications(unreadOnly: unreadOnly);
  }

  Future<void> refresh() async {
    final unreadOnly = ref.read(notificationsUnreadOnlyProvider);
    state = const AsyncLoading();
    state = await AsyncValue.guard(
      () => ref.read(notificationRepositoryProvider).getNotifications(unreadOnly: unreadOnly),
    );
  }

  Future<void> markAsRead(String id) async {
    await ref.read(notificationRepositoryProvider).markAsRead(id);
    await refresh();
    ref.invalidate(unreadNotificationCountProvider);
  }

  Future<void> markAllAsRead() async {
    await ref.read(notificationRepositoryProvider).markAllAsRead();
    await refresh();
    ref.invalidate(unreadNotificationCountProvider);
  }
}

/// Đếm chưa đọc dùng cho badge chuông thông báo - refresh() cố tình KHÔNG chuyển qua AsyncLoading
/// trước, để số trên badge không nhấp nháy biến mất trong lúc gọi lại API nền.
final unreadNotificationCountProvider =
    AsyncNotifierProvider<UnreadNotificationCountController, int>(UnreadNotificationCountController.new);

class UnreadNotificationCountController extends AsyncNotifier<int> {
  @override
  Future<int> build() => ref.read(notificationRepositoryProvider).getUnreadCount();

  Future<void> refresh() async {
    state = await AsyncValue.guard(() => ref.read(notificationRepositoryProvider).getUnreadCount());
  }
}
