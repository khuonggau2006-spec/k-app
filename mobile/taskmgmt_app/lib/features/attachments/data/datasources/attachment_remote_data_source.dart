import 'dart:typed_data';

import 'package:dio/dio.dart';

import '../../../../core/network/api_exception.dart';
import '../models/attachment_model.dart';

class AttachmentRemoteDataSource {
  AttachmentRemoteDataSource(this._dio);

  final Dio _dio;

  Future<List<AttachmentModel>> getAttachments(String workTaskId) async {
    try {
      final response = await _dio.get<List<dynamic>>('/worktasks/$workTaskId/attachments');
      return response.data!.map((json) => AttachmentModel.fromJson(json as Map<String, dynamic>)).toList();
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<AttachmentModel> uploadAttachment({
    required String workTaskId,
    required String fileName,
    required Uint8List bytes,
  }) async {
    try {
      final formData = FormData.fromMap({
        'file': MultipartFile.fromBytes(bytes, filename: fileName),
      });
      final response = await _dio.post<Map<String, dynamic>>(
        '/worktasks/$workTaskId/attachments',
        data: formData,
      );
      return AttachmentModel.fromJson(response.data!);
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<Uint8List> downloadAttachment({required String workTaskId, required String attachmentId}) async {
    try {
      final response = await _dio.get<List<int>>(
        '/worktasks/$workTaskId/attachments/$attachmentId/download',
        options: Options(responseType: ResponseType.bytes),
      );
      return Uint8List.fromList(response.data!);
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<void> deleteAttachment({required String workTaskId, required String attachmentId}) async {
    try {
      await _dio.delete<void>('/worktasks/$workTaskId/attachments/$attachmentId');
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }
}
