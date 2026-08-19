import 'dart:typed_data';

import '../entities/attachment.dart';

abstract class AttachmentRepository {
  Future<List<Attachment>> getAttachments(String workTaskId);

  Future<Attachment> uploadAttachment({
    required String workTaskId,
    required String fileName,
    required Uint8List bytes,
    void Function(int sent, int total)? onSendProgress,
  });

  Future<Uint8List> downloadAttachment({required String workTaskId, required String attachmentId});

  Future<void> deleteAttachment({required String workTaskId, required String attachmentId});
}
