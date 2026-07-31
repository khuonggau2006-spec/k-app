import 'package:dio/dio.dart';

import '../../../../core/network/api_exception.dart';
import '../models/location_model.dart';

class LocationRemoteDataSource {
  LocationRemoteDataSource(this._dio);

  final Dio _dio;

  Future<List<LocationModel>> getLocations() async {
    try {
      final response = await _dio.get<List<dynamic>>('/locations');
      return response.data!
          .map((json) => LocationModel.fromJson(json as Map<String, dynamic>))
          .toList();
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }
}
