import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:taskmgmt_app/app.dart';
import 'package:taskmgmt_app/core/models/paged_result.dart';
import 'package:taskmgmt_app/core/push/push_notification_provider.dart';
import 'package:taskmgmt_app/core/push/push_notification_service.dart';
import 'package:taskmgmt_app/core/realtime/realtime_provider.dart';
import 'package:taskmgmt_app/core/realtime/realtime_service.dart';
import 'package:taskmgmt_app/core/realtime/task_updated_event.dart';
import 'package:taskmgmt_app/features/auth/domain/entities/user.dart';
import 'package:taskmgmt_app/features/auth/domain/repositories/auth_repository.dart';
import 'package:taskmgmt_app/features/auth/presentation/providers/auth_provider.dart';
import 'package:taskmgmt_app/features/locations/domain/entities/location.dart';
import 'package:taskmgmt_app/features/locations/domain/repositories/location_repository.dart';
import 'package:taskmgmt_app/features/locations/presentation/providers/location_provider.dart';
import 'package:taskmgmt_app/features/notifications/domain/entities/notification.dart';
import 'package:taskmgmt_app/features/notifications/domain/entities/notification_preference.dart';
import 'package:taskmgmt_app/features/notifications/domain/repositories/notification_repository.dart';
import 'package:taskmgmt_app/features/notifications/presentation/providers/notification_provider.dart';
import 'package:taskmgmt_app/features/tasks/domain/entities/work_task.dart';
import 'package:taskmgmt_app/features/tasks/domain/repositories/work_task_repository.dart';
import 'package:taskmgmt_app/features/tasks/presentation/providers/work_task_provider.dart';

class _FakeAuthRepository implements AuthRepository {
  @override
  Future<User?> restoreSession() async => null;

  @override
  Future<User> login({required String email, required String password}) async =>
      User(id: '1', email: email, fullName: 'Test User', systemRole: SystemRole.member, hasAvatar: false);

  @override
  Future<User> register({required String email, required String fullName, required String password}) async =>
      User(id: '1', email: email, fullName: fullName, systemRole: SystemRole.member, hasAvatar: false);

  @override
  Future<void> logout() async {}
}

class _FakeLocationRepository implements LocationRepository {
  @override
  Future<List<Location>> getLocations() async => [];
}

class _FakePushNotificationService implements PushNotificationService {
  @override
  Future<void> initialize() async {}

  @override
  Future<void> unregisterCurrentToken() async {}
}

class _FakeRealtimeService implements RealtimeService {
  @override
  Future<void> connect() async {}

  @override
  Future<void> disconnect() async {}

  @override
  Future<void> joinTaskGroup(String taskId) async {}

  @override
  Future<void> leaveTaskGroup(String taskId) async {}

  @override
  Stream<TaskUpdatedEvent> get taskUpdates => const Stream.empty();
}

class _FakeWorkTaskRepository implements WorkTaskRepository {
  @override
  Future<PagedResult<WorkTask>> getTasks({WorkTaskStatus? status, String? locationId, String? parentTaskId}) async =>
      const PagedResult(items: [], pageNumber: 1, pageSize: 50, totalCount: 0, totalPages: 0);

  @override
  Future<WorkTask> getTaskById(String id) async => throw UnimplementedError();

  @override
  Future<WorkTask> createTask({
    required String title,
    String? description,
    DateTime? dueDateUtc,
    String? locationId,
    String? parentTaskId,
  }) async =>
      throw UnimplementedError();

  @override
  Future<WorkTask> updateTask({
    required String id,
    required String title,
    String? description,
    required WorkTaskStatus status,
    DateTime? dueDateUtc,
    String? locationId,
  }) async =>
      throw UnimplementedError();

  @override
  Future<void> deleteTask(String id) async {}
}

// Mô phỏng: server có thêm thông báo mới trong lúc app ở nền (curl từ "thiết bị khác" cập nhật
// task) - unreadCount đổi từ 2 lên 9 giữa 2 lần app gọi, dù không nhận được sự kiện SignalR nào.
class _FakeNotificationRepository implements NotificationRepository {
  int callCount = 0;

  @override
  Future<int> getUnreadCount() async {
    callCount += 1;
    return callCount == 1 ? 2 : 9;
  }

  @override
  Future<PagedResult<AppNotification>> getNotifications({bool unreadOnly = false}) async =>
      const PagedResult(items: [], pageNumber: 1, pageSize: 50, totalCount: 0, totalPages: 0);

  @override
  Future<void> markAsRead(String id) async {}

  @override
  Future<void> markAllAsRead() async {}

  @override
  Future<List<NotificationPreference>> getPreferences() async => [];

  @override
  Future<void> updatePreference(String type, bool isEnabled) async {}
}

Widget _buildApp(_FakeNotificationRepository notificationRepo) => ProviderScope(
      overrides: [
        authRepositoryProvider.overrideWithValue(_FakeAuthRepository()),
        locationRepositoryProvider.overrideWithValue(_FakeLocationRepository()),
        workTaskRepositoryProvider.overrideWithValue(_FakeWorkTaskRepository()),
        pushNotificationServiceProvider.overrideWithValue(_FakePushNotificationService()),
        realtimeServiceProvider.overrideWithValue(_FakeRealtimeService()),
        notificationRepositoryProvider.overrideWithValue(notificationRepo),
      ],
      child: const TaskMgmtApp(),
    );

void main() {
  testWidgets('Resuming the app from background refreshes the unread notification badge', (tester) async {
    final notificationRepo = _FakeNotificationRepository();
    await tester.pumpWidget(_buildApp(notificationRepo));
    await tester.pumpAndSettle();

    await tester.enterText(find.widgetWithText(TextFormField, 'Email'), 'user@example.com');
    await tester.enterText(find.widgetWithText(TextFormField, 'Mật khẩu'), 'any-password');
    await tester.tap(find.widgetWithText(FilledButton, 'Đăng nhập'));
    await tester.pumpAndSettle();

    // Badge số thông báo chưa đọc giờ nằm trên ô "Thông báo" của màn Home (thay vì AppBar của
    // Công việc) - lưới 5 ô cao hơn viewport test mặc định nên nới kích thước ảo để dựng hết.
    tester.view.physicalSize = const Size(800, 1400);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    await tester.pumpAndSettle();

    Finder badgeLabel(String text) => find.descendant(of: find.byType(Badge), matching: find.text(text));

    expect(badgeLabel('2'), findsOneWidget);

    tester.binding.handleAppLifecycleStateChanged(AppLifecycleState.paused);
    tester.binding.handleAppLifecycleStateChanged(AppLifecycleState.resumed);
    await tester.pumpAndSettle();

    expect(badgeLabel('9'), findsOneWidget);
  });
}
