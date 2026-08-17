import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/models/paged_result.dart';
import '../../../../core/network/api_exception.dart';
import '../../../../shared/widgets/empty_state_view.dart';
import '../../../../shared/widgets/error_state_view.dart';
import '../../domain/entities/notification.dart';
import '../providers/notification_provider.dart';
import '../widgets/notification_list_item.dart';
import 'notification_preferences_screen.dart';

class NotificationCenterScreen extends ConsumerWidget {
  const NotificationCenterScreen({super.key});

  static const path = '/notifications';
  static const name = 'notifications';

  Future<void> _handleTap(BuildContext context, WidgetRef ref, AppNotification notification) async {
    if (!notification.isRead) {
      try {
        await ref.read(notificationsProvider.notifier).markAsRead(notification.id);
      } catch (e) {
        if (!context.mounted) return;
        final message = e is ApiException ? e.message : 'Không thể đánh dấu đã đọc.';
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
        return;
      }
    }

    if (notification.workTaskId != null && context.mounted) {
      context.push('/tasks/${notification.workTaskId}');
    }
  }

  Future<void> _handleMarkAllAsRead(BuildContext context, WidgetRef ref) async {
    try {
      await ref.read(notificationsProvider.notifier).markAllAsRead();
    } catch (e) {
      if (!context.mounted) return;
      final message = e is ApiException ? e.message : 'Không thể đánh dấu tất cả đã đọc.';
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final notificationsAsync = ref.watch(notificationsProvider);
    final unreadOnly = ref.watch(notificationsUnreadOnlyProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Thông báo'),
        actions: [
          IconButton(
            icon: Icon(unreadOnly ? Icons.filter_alt : Icons.filter_alt_outlined),
            tooltip: unreadOnly ? 'Đang lọc: chưa đọc' : 'Lọc: chưa đọc',
            onPressed: () => ref.read(notificationsUnreadOnlyProvider.notifier).state = !unreadOnly,
          ),
          IconButton(
            icon: const Icon(Icons.done_all),
            tooltip: 'Đánh dấu tất cả đã đọc',
            onPressed: () => _handleMarkAllAsRead(context, ref),
          ),
          IconButton(
            icon: const Icon(Icons.settings_outlined),
            tooltip: 'Cài đặt thông báo',
            onPressed: () => context.push(NotificationPreferencesScreen.path),
          ),
        ],
      ),
      body: notificationsAsync.when(
        data: (result) => _buildList(context, ref, result),
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => ErrorStateView(
          message: error is ApiException ? error.message : 'Không thể tải danh sách thông báo.',
          onRetry: () => ref.read(notificationsProvider.notifier).refresh(),
        ),
      ),
    );
  }

  Widget _buildList(BuildContext context, WidgetRef ref, PagedResult<AppNotification> result) {
    if (result.items.isEmpty) {
      return const EmptyStateView(icon: Icons.notifications_none, message: 'Chưa có thông báo nào.');
    }

    return RefreshIndicator(
      onRefresh: () => ref.read(notificationsProvider.notifier).refresh(),
      child: ListView.separated(
        physics: const AlwaysScrollableScrollPhysics(),
        itemCount: result.items.length,
        separatorBuilder: (context, index) => const Divider(height: 1),
        itemBuilder: (context, index) {
          final notification = result.items[index];
          return NotificationListItem(
            notification: notification,
            onTap: () => _handleTap(context, ref, notification),
          );
        },
      ),
    );
  }
}
