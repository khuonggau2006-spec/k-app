import 'package:dio/dio.dart';

import '../../../../core/network/api_exception.dart';
import '../models/task_history_model.dart';

class TaskHistoryRemoteDataSource {
  TaskHistoryRemoteDataSource(this._dio);

  final Dio _dio;

  Future<List<TaskHistoryModel>> getHistory(String workTaskId, {int pageSize = 100}) async {
    try {
      final response = await _dio.get<Map<String, dynamic>>(
        '/worktasks/$workTaskId/history',
        queryParameters: {'pageSize': pageSize},
      );
      final items = response.data!['items'] as List<dynamic>;
      return items.map((json) => TaskHistoryModel.fromJson(json as Map<String, dynamic>)).toList();
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }
}
