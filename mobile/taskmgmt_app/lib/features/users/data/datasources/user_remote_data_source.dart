import 'dart:typed_data';

import 'package:dio/dio.dart';

import '../../../../core/network/api_exception.dart';
import '../../../auth/data/models/user_model.dart';

class UserRemoteDataSource {
  UserRemoteDataSource(this._dio);

  final Dio _dio;

  Future<List<UserModel>> getUsers() async {
    try {
      final response = await _dio.get<List<dynamic>>('/users');
      return response.data!.map((json) => UserModel.fromJson(json as Map<String, dynamic>)).toList();
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<Uint8List?> downloadAvatar(String userId) async {
    try {
      final response = await _dio.get<List<int>>(
        '/users/$userId/avatar',
        options: Options(responseType: ResponseType.bytes),
      );
      return Uint8List.fromList(response.data!);
    } on DioException catch (e) {
      if (e.response?.statusCode == 404) return null;
      throw mapDioException(e);
    }
  }

  Future<UserModel> uploadAvatar({required Uint8List bytes, required String fileName}) async {
    try {
      final formData = FormData.fromMap({
        'file': MultipartFile.fromBytes(bytes, filename: fileName),
      });
      final response = await _dio.post<Map<String, dynamic>>('/users/me/avatar', data: formData);
      return UserModel.fromJson(response.data!);
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<UserModel> deleteAvatar() async {
    try {
      final response = await _dio.delete<Map<String, dynamic>>('/users/me/avatar');
      return UserModel.fromJson(response.data!);
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }
}
