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
}
