import 'package:dio/dio.dart';

import '../../../../core/network/api_exception.dart';
import '../../domain/entities/task_assignee.dart';
import '../models/task_assignee_model.dart';

class TaskAssigneeRemoteDataSource {
  TaskAssigneeRemoteDataSource(this._dio);

  final Dio _dio;

  Future<List<TaskAssigneeModel>> getAssignees(String workTaskId) async {
    try {
      final response = await _dio.get<List<dynamic>>('/worktasks/$workTaskId/assignees');
      return response.data!.map((json) => TaskAssigneeModel.fromJson(json as Map<String, dynamic>)).toList();
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<TaskAssigneeModel> addAssignee({
    required String workTaskId,
    required String userId,
    required TaskAssigneeRole role,
  }) async {
    try {
      final response = await _dio.post<Map<String, dynamic>>(
        '/worktasks/$workTaskId/assignees',
        data: {'userId': userId, 'role': taskAssigneeRoleToApiString(role)},
      );
      return TaskAssigneeModel.fromJson(response.data!);
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<TaskAssigneeModel> changeRole({
    required String workTaskId,
    required String userId,
    required TaskAssigneeRole role,
  }) async {
    try {
      final response = await _dio.put<Map<String, dynamic>>(
        '/worktasks/$workTaskId/assignees/$userId',
        data: {'role': taskAssigneeRoleToApiString(role)},
      );
      return TaskAssigneeModel.fromJson(response.data!);
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<void> removeAssignee({required String workTaskId, required String userId}) async {
    try {
      await _dio.delete<void>('/worktasks/$workTaskId/assignees/$userId');
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }
}
