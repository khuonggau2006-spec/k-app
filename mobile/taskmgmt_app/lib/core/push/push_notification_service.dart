import 'dart:convert';
import 'dart:io';

import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:go_router/go_router.dart';

import '../../features/notifications/data/datasources/device_token_remote_data_source.dart';
import '../../firebase_options.dart';

// Background handler PHẢI là hàm top-level (không phải method trong class) - chạy trên isolate
// riêng khi app ở background/terminated, nên phải tự initializeApp() lại, không dùng chung state
// với isolate chính. Không cần làm gì thêm ở đây: hệ điều hành tự hiển thị system notification từ
// trường `Notification` mà backend gửi kèm (xem FirebaseFcmPushNotificationService.cs) - handler
// này chỉ là điểm bắt buộc phải đăng ký để FCM cho phép nhận message khi app không ở foreground.
@pragma('vm:entry-point')
Future<void> firebaseMessagingBackgroundHandler(RemoteMessage message) async {
  await Firebase.initializeApp(options: DefaultFirebaseOptions.currentPlatform);
}

const _androidChannel = AndroidNotificationChannel(
  'task_updates',
  'Cập nhật công việc',
  description: 'Thông báo khi công việc bạn tham gia có thay đổi.',
  importance: Importance.high,
);

/// Điều phối toàn bộ luồng push: khởi tạo Firebase, xin quyền, hiển thị local notification khi
/// app đang mở (foreground), và điều hướng deep-link tới đúng công việc khi người dùng tap vào
/// thông báo (dù app đang background, terminated, hay đang mở).
///
/// Chưa cấu hình Firebase thật (thiếu google-services.json/GoogleService-Info.plist) thì
/// initialize() thất bại êm ái - app vẫn chạy bình thường, chỉ không có push, giống cách backend
/// graceful-degrade khi thiếu Firebase:CredentialsPath.
class PushNotificationService {
  PushNotificationService(this._deviceTokenRemoteDataSource, this._router);

  final DeviceTokenRemoteDataSource _deviceTokenRemoteDataSource;
  final GoRouter _router;
  final _localNotifications = FlutterLocalNotificationsPlugin();

  bool _initialized = false;

  Future<void> initialize() async {
    if (_initialized || kIsWeb) return;

    try {
      await Firebase.initializeApp(options: DefaultFirebaseOptions.currentPlatform);
    } catch (_) {
      return;
    }

    final settings = await FirebaseMessaging.instance.requestPermission();
    if (settings.authorizationStatus == AuthorizationStatus.denied) {
      return;
    }

    _initialized = true;

    await _initLocalNotifications();

    FirebaseMessaging.onBackgroundMessage(firebaseMessagingBackgroundHandler);
    FirebaseMessaging.onMessage.listen(_handleForegroundMessage);
    FirebaseMessaging.onMessageOpenedApp.listen((message) => _navigateFromData(message.data));

    final initialMessage = await FirebaseMessaging.instance.getInitialMessage();
    if (initialMessage != null) {
      _navigateFromData(initialMessage.data);
    }

    FirebaseMessaging.instance.onTokenRefresh.listen(_registerToken);
    final token = await FirebaseMessaging.instance.getToken();
    if (token != null) {
      await _registerToken(token);
    }
  }

  Future<void> _initLocalNotifications() async {
    const androidInit = AndroidInitializationSettings('@mipmap/ic_launcher');
    const iosInit = DarwinInitializationSettings();

    await _localNotifications.initialize(
      settings: const InitializationSettings(android: androidInit, iOS: iosInit),
      onDidReceiveNotificationResponse: (response) {
        final payload = response.payload;
        if (payload == null) return;
        _navigateFromData(jsonDecode(payload) as Map<String, dynamic>);
      },
    );

    await _localNotifications
        .resolvePlatformSpecificImplementation<AndroidFlutterLocalNotificationsPlugin>()
        ?.createNotificationChannel(_androidChannel);
  }

  // FCM chỉ tự hiển thị system notification khi app ở background/terminated - lúc app đang mở
  // (foreground) phải tự show bằng flutter_local_notifications, nếu không người dùng sẽ không
  // thấy gì cả.
  void _handleForegroundMessage(RemoteMessage message) {
    final notification = message.notification;
    if (notification == null) return;

    _localNotifications.show(
      id: notification.hashCode,
      title: notification.title,
      body: notification.body,
      notificationDetails: NotificationDetails(
        android: AndroidNotificationDetails(
          _androidChannel.id,
          _androidChannel.name,
          channelDescription: _androidChannel.description,
          importance: Importance.high,
          priority: Priority.high,
        ),
        iOS: const DarwinNotificationDetails(),
      ),
      payload: jsonEncode(message.data),
    );
  }

  void _navigateFromData(Map<String, dynamic> data) {
    final workTaskId = data['workTaskId'] as String?;
    if (workTaskId != null) {
      _router.push('/tasks/$workTaskId');
    }
  }

  Future<void> _registerToken(String token) async {
    try {
      await _deviceTokenRemoteDataSource.register(token, platform: _currentPlatform);
    } catch (_) {
      // Lỗi mạng/server lúc đăng ký token không nên chặn người dùng dùng app - bỏ qua, lần tới
      // onTokenRefresh hoặc initialize() kế tiếp (mở app lại) sẽ tự thử lại.
    }
  }

  /// Gọi khi đăng xuất - gỡ token khỏi backend để không còn nhận push cho tài khoản đã thoát.
  Future<void> unregisterCurrentToken() async {
    if (!_initialized) return;

    try {
      final token = await FirebaseMessaging.instance.getToken();
      if (token != null) {
        await _deviceTokenRemoteDataSource.unregister(token);
      }
    } catch (_) {}
  }

  String get _currentPlatform {
    if (Platform.isIOS) return 'Ios';
    return 'Android';
  }
}
