import 'package:firebase_remote_config/firebase_remote_config.dart';

/// Đọc `api_base_url` từ Firebase Remote Config - cho phép đổi backend production đang trỏ tới
/// mà không cần build lại/phát hành lại app (xem mục 5.4 G5 trong KE-HOACH-TRIEN-KHAI.md).
class RemoteConfigService {
  static const _apiBaseUrlKey = 'api_base_url';

  /// Trả về giá trị `api_base_url` đã fetch được, hoặc null nếu chưa cấu hình Remote Config
  /// (key rỗng) hoặc fetch thất bại (mất mạng, Firebase chưa khởi tạo...) - không bao giờ throw,
  /// để không chặn app khởi động khi Remote Config gặp sự cố.
  Future<String?> fetchApiBaseUrl() async {
    try {
      final remoteConfig = FirebaseRemoteConfig.instance;
      await remoteConfig.setConfigSettings(
        RemoteConfigSettings(
          fetchTimeout: const Duration(seconds: 10),
          minimumFetchInterval: const Duration(hours: 1),
        ),
      );
      await remoteConfig.fetchAndActivate();

      final value = remoteConfig.getString(_apiBaseUrlKey);
      return value.isEmpty ? null : value;
    } catch (_) {
      return null;
    }
  }
}
