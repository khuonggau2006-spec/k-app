import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:get_it/get_it.dart';
import 'package:go_router/go_router.dart';

import 'package:taskmgmt_app/core/models/paged_result.dart';
import 'package:taskmgmt_app/core/network/auth_event_bus.dart';
import 'package:taskmgmt_app/features/auth/domain/entities/user.dart';
import 'package:taskmgmt_app/features/auth/domain/repositories/auth_repository.dart';
import 'package:taskmgmt_app/features/auth/presentation/providers/auth_provider.dart';
import 'package:taskmgmt_app/features/home/presentation/screens/home_screen.dart';
import 'package:taskmgmt_app/features/notifications/domain/entities/notification.dart';
import 'package:taskmgmt_app/features/notifications/domain/entities/notification_preference.dart';
import 'package:taskmgmt_app/features/notifications/domain/repositories/notification_repository.dart';
import 'package:taskmgmt_app/features/notifications/presentation/providers/notification_provider.dart';

class _FakeAuthRepository implements AuthRepository {
  @override
  Future<User?> restoreSession() async =>
      const User(
        id: '1',
        email: 'user@example.com',
        fullName: 'Nguyễn Test',
        systemRole: SystemRole.member,
        hasAvatar: false,
      );

  @override
  Future<User> login({required String email, required String password}) async => throw UnimplementedError();

  @override
  Future<User> register({required String email, required String fullName, required String password}) async =>
      throw UnimplementedError();

  @override
  Future<void> logout() async {}
}

class _FakeNotificationRepository implements NotificationRepository {
  @override
  Future<PagedResult<AppNotification>> getNotifications({bool unreadOnly = false}) async =>
      const PagedResult(items: [], pageNumber: 1, pageSize: 50, totalCount: 0, totalPages: 0);

  @override
  Future<int> getUnreadCount() async => 3;

  @override
  Future<void> markAsRead(String id) async {}

  @override
  Future<void> markAllAsRead() async {}

  @override
  Future<List<NotificationPreference>> getPreferences() async => [];

  @override
  Future<void> updatePreference(String type, bool isEnabled) async {}
}

Widget _buildApp() {
  final router = GoRouter(
    initialLocation: HomeScreen.path,
    routes: [
      GoRoute(path: HomeScreen.path, name: HomeScreen.name, builder: (context, state) => const HomeScreen()),
      GoRoute(path: '/tasks', builder: (context, state) => const Scaffold(body: Text('Màn Công việc'))),
      GoRoute(path: '/dashboard', builder: (context, state) => const Scaffold(body: Text('Màn Dashboard'))),
      GoRoute(path: '/attendance', builder: (context, state) => const Scaffold(body: Text('Màn Chấm công'))),
      GoRoute(path: '/locations', builder: (context, state) => const Scaffold(body: Text('Màn Vị trí'))),
      GoRoute(path: '/notifications', builder: (context, state) => const Scaffold(body: Text('Màn Thông báo'))),
      GoRoute(path: '/profile', builder: (context, state) => const Scaffold(body: Text('Màn Hồ sơ'))),
    ],
  );

  return ProviderScope(
    overrides: [
      authRepositoryProvider.overrideWithValue(_FakeAuthRepository()),
      notificationRepositoryProvider.overrideWithValue(_FakeNotificationRepository()),
    ],
    child: MaterialApp.router(routerConfig: router),
  );
}

void main() {
  setUp(() {
    // AuthController.build() gọi getIt<AuthEventBus>() - đăng ký thủ công vì test không gọi
    // setupLocator() (tránh phải fake toàn bộ cây phụ thuộc DI thật).
    GetIt.instance.registerLazySingleton<AuthEventBus>(() => AuthEventBus());
  });

  tearDown(() => GetIt.instance.reset());

  testWidgets('Shows greeting and all 5 feature tiles with unread badge', (tester) async {
    // Lưới 5 ô (3 hàng) cao hơn viewport test mặc định - nới kích thước ảo để dựng hết.
    tester.view.physicalSize = const Size(800, 1400);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(_buildApp());
    await tester.pumpAndSettle();

    expect(find.text('Xin chào, Nguyễn Test'), findsOneWidget);
    expect(find.text('Công việc'), findsOneWidget);
    expect(find.text('Dashboard'), findsOneWidget);
    expect(find.text('Chấm công'), findsOneWidget);
    expect(find.text('Vị trí'), findsOneWidget);
    expect(find.text('Thông báo'), findsOneWidget);
    expect(find.text('3'), findsOneWidget);
  });

  testWidgets('Tapping a tile navigates to its feature screen', (tester) async {
    tester.view.physicalSize = const Size(800, 1400);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(_buildApp());
    await tester.pumpAndSettle();

    await tester.tap(find.text('Chấm công'));
    await tester.pumpAndSettle();

    expect(find.text('Màn Chấm công'), findsOneWidget);
  });

  testWidgets('Logout icon signs the user out', (tester) async {
    await tester.pumpWidget(_buildApp());
    await tester.pumpAndSettle();

    await tester.tap(find.byIcon(Icons.logout));
    await tester.pumpAndSettle();

    expect(find.textContaining('Xin chào'), findsNothing);
  });

  testWidgets('Tapping the avatar navigates to the profile screen', (tester) async {
    tester.view.physicalSize = const Size(800, 1400);
    tester.view.devicePixelRatio = 1.0;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);

    await tester.pumpWidget(_buildApp());
    await tester.pumpAndSettle();

    await tester.tap(find.byType(GestureDetector).first);
    await tester.pumpAndSettle();

    expect(find.text('Màn Hồ sơ'), findsOneWidget);
  });
}
