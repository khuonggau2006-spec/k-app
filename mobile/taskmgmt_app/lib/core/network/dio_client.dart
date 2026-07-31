import 'package:dio/dio.dart';

import '../config/app_config.dart';
import '../storage/token_storage.dart';
import 'auth_event_bus.dart';
import 'auth_interceptor.dart';

Dio createDioClient(TokenStorage tokenStorage, AuthEventBus authEventBus) {
  final dio = Dio(
    BaseOptions(
      baseUrl: AppConfig.apiBaseUrl,
      connectTimeout: const Duration(seconds: 15),
      receiveTimeout: const Duration(seconds: 15),
    ),
  );

  dio.interceptors.add(AuthInterceptor(tokenStorage, authEventBus));

  return dio;
}
