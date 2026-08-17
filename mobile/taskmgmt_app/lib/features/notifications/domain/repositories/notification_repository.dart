import '../../../../core/models/paged_result.dart';
import '../entities/notification.dart';
import '../entities/notification_preference.dart';

abstract class NotificationRepository {
  Future<PagedResult<AppNotification>> getNotifications({bool unreadOnly = false});

  Future<int> getUnreadCount();

  Future<void> markAsRead(String id);

  Future<void> markAllAsRead();

  Future<List<NotificationPreference>> getPreferences();

  Future<void> updatePreference(String type, bool isEnabled);
}
