import 'package:dio/dio.dart';

import '../../../../core/network/api_exception.dart';

class DeviceTokenRemoteDataSource {
  DeviceTokenRemoteDataSource(this._dio);

  final Dio _dio;

  Future<void> register(String token, {required String platform}) async {
    try {
      await _dio.post<void>('/device-tokens', data: {'token': token, 'platform': platform});
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<void> unregister(String token) async {
    try {
      await _dio.delete<void>('/device-tokens/${Uri.encodeComponent(token)}');
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }
}
