import 'package:dio/dio.dart';

import '../../../../core/network/api_exception.dart';
import '../models/comment_model.dart';

class CommentRemoteDataSource {
  CommentRemoteDataSource(this._dio);

  final Dio _dio;

  Future<List<CommentModel>> getComments(String workTaskId) async {
    try {
      final response = await _dio.get<List<dynamic>>('/worktasks/$workTaskId/comments');
      return response.data!.map((json) => CommentModel.fromJson(json as Map<String, dynamic>)).toList();
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<CommentModel> addComment({
    required String workTaskId,
    required String content,
    required List<String> mentionedUserIds,
  }) async {
    try {
      final response = await _dio.post<Map<String, dynamic>>(
        '/worktasks/$workTaskId/comments',
        data: {'content': content, 'mentionedUserIds': mentionedUserIds},
      );
      return CommentModel.fromJson(response.data!);
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }
}
