import '../../../../core/models/paged_result.dart';
import '../entities/notification.dart';

abstract class NotificationRepository {
  Future<PagedResult<AppNotification>> getNotifications({bool unreadOnly = false});

  Future<int> getUnreadCount();

  Future<void> markAsRead(String id);

  Future<void> markAllAsRead();
}
