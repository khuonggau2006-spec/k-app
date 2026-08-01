import 'dart:typed_data';

import '../../domain/entities/attachment.dart';
import '../../domain/repositories/attachment_repository.dart';
import '../datasources/attachment_remote_data_source.dart';

class AttachmentRepositoryImpl implements AttachmentRepository {
  AttachmentRepositoryImpl(this._remoteDataSource);

  final AttachmentRemoteDataSource _remoteDataSource;

  @override
  Future<List<Attachment>> getAttachments(String workTaskId) async {
    final models = await _remoteDataSource.getAttachments(workTaskId);
    return models.map((model) => model.toDomain()).toList();
  }

  @override
  Future<Attachment> uploadAttachment({
    required String workTaskId,
    required String fileName,
    required Uint8List bytes,
  }) async {
    final model = await _remoteDataSource.uploadAttachment(workTaskId: workTaskId, fileName: fileName, bytes: bytes);
    return model.toDomain();
  }

  @override
  Future<Uint8List> downloadAttachment({required String workTaskId, required String attachmentId}) =>
      _remoteDataSource.downloadAttachment(workTaskId: workTaskId, attachmentId: attachmentId);

  @override
  Future<void> deleteAttachment({required String workTaskId, required String attachmentId}) =>
      _remoteDataSource.deleteAttachment(workTaskId: workTaskId, attachmentId: attachmentId);
}
