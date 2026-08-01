import 'dart:typed_data';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/di/injection.dart';
import '../../domain/entities/attachment.dart';
import '../../domain/repositories/attachment_repository.dart';

final attachmentRepositoryProvider = Provider<AttachmentRepository>((ref) => getIt<AttachmentRepository>());

final attachmentsProvider =
    AsyncNotifierProvider.family<AttachmentsController, List<Attachment>, String>(AttachmentsController.new);

class AttachmentsController extends FamilyAsyncNotifier<List<Attachment>, String> {
  @override
  Future<List<Attachment>> build(String workTaskId) {
    return ref.read(attachmentRepositoryProvider).getAttachments(workTaskId);
  }

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() => ref.read(attachmentRepositoryProvider).getAttachments(arg));
  }

  Future<void> uploadAttachment({required String fileName, required Uint8List bytes}) async {
    await ref.read(attachmentRepositoryProvider).uploadAttachment(workTaskId: arg, fileName: fileName, bytes: bytes);
    await refresh();
  }

  Future<Uint8List> downloadAttachment(String attachmentId) =>
      ref.read(attachmentRepositoryProvider).downloadAttachment(workTaskId: arg, attachmentId: attachmentId);

  Future<void> deleteAttachment(String attachmentId) async {
    await ref.read(attachmentRepositoryProvider).deleteAttachment(workTaskId: arg, attachmentId: attachmentId);
    await refresh();
  }
}
