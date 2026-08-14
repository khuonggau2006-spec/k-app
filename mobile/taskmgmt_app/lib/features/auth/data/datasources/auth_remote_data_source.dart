import 'package:dio/dio.dart';

import '../../../../core/network/api_exception.dart';
import '../models/auth_result_model.dart';

class AuthRemoteDataSource {
  AuthRemoteDataSource(this._dio);

  final Dio _dio;

  Future<AuthResultModel> register({
    required String email,
    required String fullName,
    required String password,
  }) async {
    try {
      final response = await _dio.post<Map<String, dynamic>>(
        '/auth/register',
        data: {'email': email, 'fullName': fullName, 'password': password},
      );
      return AuthResultModel.fromJson(response.data!);
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<AuthResultModel> login({required String email, required String password}) async {
    try {
      final response = await _dio.post<Map<String, dynamic>>(
        '/auth/login',
        data: {'email': email, 'password': password},
      );
      return AuthResultModel.fromJson(response.data!);
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<AuthResultModel> refreshToken(String refreshToken) async {
    try {
      final response = await _dio.post<Map<String, dynamic>>(
        '/auth/refresh-token',
        data: {'refreshToken': refreshToken},
      );
      return AuthResultModel.fromJson(response.data!);
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<void> logout(String refreshToken) async {
    try {
      await _dio.post<void>('/auth/logout', data: {'refreshToken': refreshToken});
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }
}
