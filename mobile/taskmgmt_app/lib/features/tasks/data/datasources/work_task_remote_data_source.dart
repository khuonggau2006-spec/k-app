import 'package:dio/dio.dart';

import '../../../../core/network/api_exception.dart';
import '../../domain/entities/work_task.dart';
import '../models/work_task_model.dart';

class PagedModelResult<T> {
  const PagedModelResult({
    required this.items,
    required this.pageNumber,
    required this.pageSize,
    required this.totalCount,
    required this.totalPages,
  });

  final List<T> items;
  final int pageNumber;
  final int pageSize;
  final int totalCount;
  final int totalPages;
}

class WorkTaskRemoteDataSource {
  WorkTaskRemoteDataSource(this._dio);

  final Dio _dio;

  Future<PagedModelResult<WorkTaskModel>> getTasks({
    WorkTaskStatus? status,
    String? locationId,
    String? parentTaskId,
    int pageNumber = 1,
    int pageSize = 50,
  }) async {
    try {
      final response = await _dio.get<Map<String, dynamic>>(
        '/worktasks',
        queryParameters: {
          'pageNumber': pageNumber,
          'pageSize': pageSize,
          if (status != null) 'status': workTaskStatusToApiString(status),
          'locationId': ?locationId,
          'parentTaskId': ?parentTaskId,
        },
      );
      final data = response.data!;
      return PagedModelResult(
        items: (data['items'] as List<dynamic>)
            .map((json) => WorkTaskModel.fromJson(json as Map<String, dynamic>))
            .toList(),
        pageNumber: data['pageNumber'] as int,
        pageSize: data['pageSize'] as int,
        totalCount: data['totalCount'] as int,
        totalPages: data['totalPages'] as int,
      );
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<WorkTaskModel> getTaskById(String id) async {
    try {
      final response = await _dio.get<Map<String, dynamic>>('/worktasks/$id');
      return WorkTaskModel.fromJson(response.data!);
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<WorkTaskModel> createTask({
    required String title,
    String? description,
    DateTime? dueDateUtc,
    String? locationId,
    String? parentTaskId,
  }) async {
    try {
      final response = await _dio.post<Map<String, dynamic>>(
        '/worktasks',
        data: {
          'title': title,
          'description': description,
          'dueDateUtc': dueDateUtc?.toIso8601String(),
          'locationId': locationId,
          'parentTaskId': parentTaskId,
        },
      );
      return WorkTaskModel.fromJson(response.data!);
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<WorkTaskModel> updateTask({
    required String id,
    required String title,
    String? description,
    required WorkTaskStatus status,
    DateTime? dueDateUtc,
    String? locationId,
  }) async {
    try {
      final response = await _dio.put<Map<String, dynamic>>(
        '/worktasks/$id',
        data: {
          'id': id,
          'title': title,
          'description': description,
          'status': workTaskStatusToApiString(status),
          'dueDateUtc': dueDateUtc?.toIso8601String(),
          'locationId': locationId,
        },
      );
      return WorkTaskModel.fromJson(response.data!);
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<void> deleteTask(String id) async {
    try {
      await _dio.delete<void>('/worktasks/$id');
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }
}
