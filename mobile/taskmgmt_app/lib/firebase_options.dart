// File cấu hình Firebase - giá trị lấy từ android/app/google-services.json (project
// "taskmgmt-dev"). Chỉ cấu hình Android vì đây là nền tảng duy nhất được thêm trên Firebase
// Console. Nếu sau này thêm iOS/Web trên Firebase Console, chạy `flutterfire configure` để sinh
// lại file này đầy đủ thay vì tự thêm tay.
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
    apiKey: 'AIzaSyAeYAbVYs3vv8h9NBtpji5RXYxw9V3Jue4',
    appId: '1:967367739767:android:5f4c099b35c4f45b4fb972',
    messagingSenderId: '967367739767',
    projectId: 'taskmgmt-dev',
    storageBucket: 'taskmgmt-dev.firebasestorage.app',
  );
}
