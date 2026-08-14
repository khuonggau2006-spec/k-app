import 'package:dio/dio.dart';

import '../../../../core/network/api_exception.dart';
import '../models/dashboard_stats_model.dart';

class DashboardRemoteDataSource {
  DashboardRemoteDataSource(this._dio);

  final Dio _dio;

  Future<DashboardStatsModel> getStats({String? locationId}) async {
    try {
      final response = await _dio.get<Map<String, dynamic>>(
        '/dashboard/stats',
        queryParameters: {'locationId': ?locationId},
      );
      return DashboardStatsModel.fromJson(response.data!);
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }
}
