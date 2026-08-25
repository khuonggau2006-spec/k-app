import 'dart:io' show Platform;

import 'package:flutter/foundation.dart' show kIsWeb;

class AppConfig {
  const AppConfig._();

  static String? _remoteBaseUrl;

  /// Gọi 1 lần ở `main()` sau khi fetch xong Firebase Remote Config, TRƯỚC `runApp` - cho phép
  /// đổi backend đang trỏ tới (staging/production) mà không cần build lại app. Giá trị null hoặc
  /// rỗng (chưa cấu hình Remote Config, fetch lỗi mạng...) thì quay lại mặc định dev bên dưới.
  static void applyRemoteBaseUrl(String? baseUrl) {
    _remoteBaseUrl = (baseUrl != null && baseUrl.isNotEmpty) ? baseUrl : null;
  }

  /// Backend dev chạy tại `dotnet run --urls "http://localhost:5299"`.
  ///
  /// Trên Android, mặc định trỏ về **IP LAN của laptop** (`192.168.1.143`) - dùng khi cài APK
  /// lên điện thoại THẬT, cùng chung Wi-Fi với laptop đang chạy backend (không cần domain/VPS).
  /// Đổi IP này nếu laptop đổi mạng/đổi IP (`ipconfig` để xem IP hiện tại).
  ///
  /// Nếu quay lại test trên MÁY ẢO, đổi tạm thành `10.0.2.2` - địa chỉ đặc biệt Android emulator
  /// ánh xạ tới `localhost` của máy host qua card mạng ảo, khác với đường hầm `adb reverse` (bỏ
  /// qua trạng thái mạng của máy) nên bật/tắt Wi-Fi trên máy ảo sẽ thật sự cắt kết nối.
  static String get _host => !kIsWeb && Platform.isAndroid ? '192.168.1.143' : 'localhost';

  static String get _defaultBaseUrl => 'http://$_host:5299';

  static String get apiBaseUrl => '${_remoteBaseUrl ?? _defaultBaseUrl}/api/v1';

  /// SignalR Hub không nằm dưới `/api/v1` - dùng chung host với [apiBaseUrl].
  static String get hubBaseUrl => _remoteBaseUrl ?? _defaultBaseUrl;
}
