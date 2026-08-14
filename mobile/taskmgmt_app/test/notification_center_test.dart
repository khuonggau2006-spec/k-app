import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:taskmgmt_app/core/models/paged_result.dart';
import 'package:taskmgmt_app/features/notifications/domain/entities/notification.dart';
import 'package:taskmgmt_app/features/notifications/domain/repositories/notification_repository.dart';
import 'package:taskmgmt_app/features/notifications/presentation/providers/notification_provider.dart';
import 'package:taskmgmt_app/features/notifications/presentation/screens/notification_center_screen.dart';

class _FakeNotificationRepository implements NotificationRepository {
  final List<AppNotification> items = [
    AppNotification(
      id: 'n1',
      title: 'Bình luận mới',
      body: 'Có bình luận mới trong "Task A".',
      type: 'CommentAdded',
      workTaskId: null,
      isRead: false,
      readAtUtc: null,
      createdAtUtc: DateTime.utc(2026, 8, 1, 10),
    ),
    AppNotification(
      id: 'n2',
      title: 'Bạn đã được gán vào công việc',
      body: 'Bạn đã được thêm vào "Task B".',
      type: 'AssigneeAdded',
      workTaskId: 'task-b',
      isRead: true,
      readAtUtc: DateTime.utc(2026, 8, 1, 9),
      createdAtUtc: DateTime.utc(2026, 8, 1, 9),
    ),
  ];

  @override
  Future<PagedResult<AppNotification>> getNotifications({bool unreadOnly = false}) async {
    final result = unreadOnly ? items.where((n) => !n.isRead).toList() : List.of(items);
    return PagedResult(items: result, pageNumber: 1, pageSize: 50, totalCount: result.length, totalPages: 1);
  }

  @override
  Future<int> getUnreadCount() async => items.where((n) => !n.isRead).length;

  @override
  Future<void> markAsRead(String id) async {
    final index = items.indexWhere((n) => n.id == id);
    if (index != -1) items[index] = items[index].copyWith(isRead: true, readAtUtc: DateTime.now());
  }

  @override
  Future<void> markAllAsRead() async {
    for (var i = 0; i < items.length; i++) {
      items[i] = items[i].copyWith(isRead: true, readAtUtc: DateTime.now());
    }
  }
}

Widget _buildScreen(_FakeNotificationRepository repo) => ProviderScope(
      overrides: [notificationRepositoryProvider.overrideWithValue(repo)],
      child: const MaterialApp(home: NotificationCenterScreen()),
    );

void main() {
  testWidgets('Shows notification list with unread visually marked', (tester) async {
    final repo = _FakeNotificationRepository();
    await tester.pumpWidget(_buildScreen(repo));
    await tester.pumpAndSettle();

    expect(find.text('Bình luận mới'), findsOneWidget);
    expect(find.text('Bạn đã được gán vào công việc'), findsOneWidget);
    // Chấm tròn chỉ báo chưa đọc chỉ xuất hiện cho thông báo n1 (isRead=false).
    expect(find.byIcon(Icons.circle), findsOneWidget);
  });

  testWidgets('Tapping an unread notification marks it as read', (tester) async {
    final repo = _FakeNotificationRepository();
    await tester.pumpWidget(_buildScreen(repo));
    await tester.pumpAndSettle();

    expect(repo.items.firstWhere((n) => n.id == 'n1').isRead, isFalse);

    await tester.tap(find.text('Bình luận mới'));
    await tester.pumpAndSettle();

    expect(repo.items.firstWhere((n) => n.id == 'n1').isRead, isTrue);
    expect(find.byIcon(Icons.circle), findsNothing);
  });

  testWidgets('Mark-all-as-read clears every unread indicator', (tester) async {
    final repo = _FakeNotificationRepository();
    await tester.pumpWidget(_buildScreen(repo));
    await tester.pumpAndSettle();

    await tester.tap(find.byIcon(Icons.done_all));
    await tester.pumpAndSettle();

    expect(repo.items.every((n) => n.isRead), isTrue);
    expect(find.byIcon(Icons.circle), findsNothing);
  });

  testWidgets('Empty state shows when there are no notifications', (tester) async {
    final repo = _FakeNotificationRepository()..items.clear();
    await tester.pumpWidget(_buildScreen(repo));
    await tester.pumpAndSettle();

    expect(find.text('Chưa có thông báo nào.'), findsOneWidget);
  });
}
