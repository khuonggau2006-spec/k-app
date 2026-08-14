import 'package:firebase_core/firebase_core.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'app.dart';
import 'core/config/app_config.dart';
import 'core/config/remote_config_service.dart';
import 'core/di/injection.dart';
import 'firebase_options.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // Đọc api_base_url từ Firebase Remote Config TRƯỚC setupLocator() - DioClient/RealtimeService
  // đọc AppConfig.apiBaseUrl/hubBaseUrl ngay lúc khởi tạo. Lỗi mạng, chưa cấu hình Firebase, hay
  // chưa set key trên Remote Config đều rơi về mặc định dev, không chặn app khởi động.
  try {
    await Firebase.initializeApp(options: DefaultFirebaseOptions.currentPlatform);
    final baseUrl = await RemoteConfigService().fetchApiBaseUrl();
    AppConfig.applyRemoteBaseUrl(baseUrl);
  } catch (_) {}

  setupLocator();
  runApp(const ProviderScope(child: TaskMgmtApp()));
}
