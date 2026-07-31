import 'dart:async';

/// Kênh thông báo toàn cục khi phiên đăng nhập hết hạn (401 từ JWT Bearer
/// middleware, không có body), để AuthController có thể phản ứng và đưa
/// người dùng về màn hình đăng nhập mà không cần phụ thuộc trực tiếp vào Dio.
class AuthEventBus {
  final _controller = StreamController<void>.broadcast();

  Stream<void> get onSessionExpired => _controller.stream;

  void notifySessionExpired() => _controller.add(null);

  void dispose() => _controller.close();
}
