import '../../../../core/models/paged_result.dart';
import '../../domain/entities/notification.dart';
import '../../domain/repositories/notification_repository.dart';
import '../datasources/notification_remote_data_source.dart';

class NotificationRepositoryImpl implements NotificationRepository {
  NotificationRepositoryImpl(this._remoteDataSource);

  final NotificationRemoteDataSource _remoteDataSource;

  @override
  Future<PagedResult<AppNotification>> getNotifications({bool unreadOnly = false}) async {
    final result = await _remoteDataSource.getNotifications(unreadOnly: unreadOnly);
    return PagedResult(
      items: result.items.map((model) => model.toDomain()).toList(),
      pageNumber: result.pageNumber,
      pageSize: result.pageSize,
      totalCount: result.totalCount,
      totalPages: result.totalPages,
    );
  }

  @override
  Future<int> getUnreadCount() => _remoteDataSource.getUnreadCount();

  @override
  Future<void> markAsRead(String id) => _remoteDataSource.markAsRead(id);

  @override
  Future<void> markAllAsRead() => _remoteDataSource.markAllAsRead();
}
