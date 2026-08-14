import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'core/push/push_notification_provider.dart';
import 'core/realtime/realtime_provider.dart';
import 'core/routing/app_router.dart';
import 'features/notifications/presentation/providers/notification_provider.dart';

class TaskMgmtApp extends ConsumerStatefulWidget {
  const TaskMgmtApp({super.key});

  @override
  ConsumerState<TaskMgmtApp> createState() => _TaskMgmtAppState();
}

class _TaskMgmtAppState extends ConsumerState<TaskMgmtApp> with WidgetsBindingObserver {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    // SignalR chỉ đẩy cập nhật khi đang kết nối (app ở foreground); nếu có thông báo mới phát
    // sinh trong lúc app ở nền, badge sẽ bị lỡ mất sự kiện đó. Làm mới lại đây để bù, phòng khi
    // người dùng mở app lại mà không có sự kiện realtime nào để tự trigger.
    if (state == AppLifecycleState.resumed) {
      ref.invalidate(unreadNotificationCountProvider);
    }
  }

  @override
  Widget build(BuildContext context) {
    final router = ref.watch(routerProvider);
    // Kích hoạt lắng nghe vòng đời đăng nhập để đăng ký/gỡ FCM token và connect/disconnect
    // SignalR đúng lúc - xem pushNotificationServiceProvider/realtimeConnectionProvider.
    ref.watch(pushNotificationServiceProvider);
    ref.watch(realtimeConnectionProvider);

    return MaterialApp.router(
      title: 'TaskMgmt',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(colorSchemeSeed: Colors.indigo, useMaterial3: true),
      routerConfig: router,
    );
  }
}
