import 'package:dio/dio.dart';

import '../../../../core/network/api_exception.dart';
import '../models/attendance_record_model.dart';
import '../models/attendance_stats_model.dart';

class AttendanceRemoteDataSource {
  AttendanceRemoteDataSource(this._dio);

  final Dio _dio;

  Future<AttendanceRecordModel> checkIn({required double latitude, required double longitude}) async {
    try {
      final response = await _dio.post<Map<String, dynamic>>(
        '/attendance/check-in',
        data: {'latitude': latitude, 'longitude': longitude},
      );
      return AttendanceRecordModel.fromJson(response.data!);
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<AttendanceRecordModel> checkOut({required double latitude, required double longitude}) async {
    try {
      final response = await _dio.post<Map<String, dynamic>>(
        '/attendance/check-out',
        data: {'latitude': latitude, 'longitude': longitude},
      );
      return AttendanceRecordModel.fromJson(response.data!);
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<AttendanceRecordModel?> getToday() async {
    try {
      final response = await _dio.get<Map<String, dynamic>?>('/attendance/today');
      return response.data == null ? null : AttendanceRecordModel.fromJson(response.data!);
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<List<AttendanceRecordModel>> getHistory({required int year, required int month}) async {
    try {
      final response = await _dio.get<List<dynamic>>(
        '/attendance/history',
        queryParameters: {'year': year, 'month': month},
      );
      return response.data!.map((json) => AttendanceRecordModel.fromJson(json as Map<String, dynamic>)).toList();
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<AttendanceStatsModel> getStats({required int year, required int month}) async {
    try {
      final response = await _dio.get<Map<String, dynamic>>(
        '/attendance/stats',
        queryParameters: {'year': year, 'month': month},
      );
      return AttendanceStatsModel.fromJson(response.data!);
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }
}
