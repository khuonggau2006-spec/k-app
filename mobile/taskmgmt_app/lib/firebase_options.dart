// File cấu hình Firebase - giá trị lấy từ android/app/google-services.json (project
// "taskmgmt-production"). Chỉ cấu hình Android vì đây là nền tảng duy nhất được thêm trên
// Firebase Console. Nếu sau này thêm iOS/Web trên Firebase Console, chạy `flutterfire configure`
// để sinh lại file này đầy đủ thay vì tự thêm tay.
//
// LƯU Ý: file này giờ trỏ tới project PRODUCTION - mọi build (kể cả `flutter run` lúc dev) đều
// gửi device token/push tới project production, chưa tách biệt dev/prod qua build flavor. Nếu
// cần tách biệt để test không làm nhiễu dữ liệu production, cân nhắc thêm Flutter build flavors
// (mỗi flavor 1 google-services.json + FirebaseOptions riêng) - hiện chưa làm vì ngoài phạm vi
// yêu cầu ban đầu.
import 'package:firebase_core/firebase_core.dart' show FirebaseOptions;
import 'package:flutter/foundation.dart' show defaultTargetPlatform, kIsWeb, TargetPlatform;

class DefaultFirebaseOptions {
  static FirebaseOptions get currentPlatform {
    if (kIsWeb) {
      throw UnsupportedError(
        'DefaultFirebaseOptions chưa cấu hình cho Web - chạy `flutterfire configure` để thêm.',
      );
    }
    switch (defaultTargetPlatform) {
      case TargetPlatform.android:
        return android;
      default:
        throw UnsupportedError(
          'DefaultFirebaseOptions chưa cấu hình cho nền tảng $defaultTargetPlatform - '
          'chạy `flutterfire configure` để thêm.',
        );
    }
  }

  static const FirebaseOptions android = FirebaseOptions(
    apiKey: 'AIzaSyBaESdOmwwcP8SB7MuCsLO5uz0x0vYxfKY',
    appId: '1:78252548716:android:112cf357e3e61affb86e49',
    messagingSenderId: '78252548716',
    projectId: 'taskmgmt-production',
    storageBucket: 'taskmgmt-production.firebasestorage.app',
  );
}
