import 'package:dio/dio.dart';

import '../storage/token_storage.dart';
import 'auth_event_bus.dart';

class AuthInterceptor extends Interceptor {
  AuthInterceptor(this._tokenStorage, this._authEventBus);

  final TokenStorage _tokenStorage;
  final AuthEventBus _authEventBus;

  @override
  Future<void> onRequest(RequestOptions options, RequestInterceptorHandler handler) async {
    final session = await _tokenStorage.readSession();
    if (session != null) {
      options.headers['Authorization'] = 'Bearer ${session.accessToken}';
    }
    handler.next(options);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) {
    if (_isSessionExpired(err)) {
      _authEventBus.notifySessionExpired();
    }
    handler.next(err);
  }

  // Phân biệt 401 do JWT hết hạn/không hợp lệ (challenge của middleware, không có
  // body JSON) với 401 nghiệp vụ có body (ví dụ sai mật khẩu lúc đăng nhập).
  bool _isSessionExpired(DioException err) {
    if (err.response?.statusCode != 401) return false;
    return err.response?.data is! Map;
  }
}
