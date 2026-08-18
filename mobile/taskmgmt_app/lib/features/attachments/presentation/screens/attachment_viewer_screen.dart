import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../shared/widgets/error_state_view.dart';
import '../../domain/entities/attachment.dart';
import '../providers/attachment_provider.dart';
import '../widgets/image_gallery_view.dart';
import '../widgets/pdf_file_view.dart';
import '../widgets/video_file_view.dart';

class AttachmentViewerScreen extends ConsumerWidget {
  const AttachmentViewerScreen({super.key, required this.taskId, required this.attachmentId});

  final String taskId;
  final String attachmentId;

  static const path = '/tasks/:taskId/attachments/:attachmentId';
  static const name = 'attachment-viewer';

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final attachmentsAsync = ref.watch(attachmentsProvider(taskId));

    return Scaffold(
      appBar: AppBar(),
      body: attachmentsAsync.when(
        data: (attachments) {
          final attachment = attachments.firstWhere((a) => a.id == attachmentId);
          return _buildContent(attachment);
        },
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => ErrorStateView(
          message: error is ApiException ? error.message : 'Không tải được tệp đính kèm.',
          onRetry: () => ref.invalidate(attachmentsProvider(taskId)),
        ),
      ),
    );
  }

  Widget _buildContent(Attachment attachment) {
    if (attachment.isImage) {
      return _ImageGalleryFor(taskId: taskId, attachmentId: attachment.id);
    }
    if (attachment.contentType == 'application/pdf') {
      return PdfFileView(taskId: taskId, attachment: attachment);
    }
    if (attachment.contentType.startsWith('video/')) {
      return VideoFileView(taskId: taskId, attachment: attachment);
    }
    return ErrorStateView(message: 'Loại tệp này không hỗ trợ xem trước.', onRetry: () {});
  }
}

/// Lọc riêng đúng danh sách ảnh trong task (không lẫn PDF/video) và tìm vị trí ảnh đang bấm
/// trong danh sách đó - tách hàm riêng để không lặp lại logic firstWhere/where 2 lần khi build.
class _ImageGalleryFor extends ConsumerWidget {
  const _ImageGalleryFor({required this.taskId, required this.attachmentId});

  final String taskId;
  final String attachmentId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final attachments = ref.watch(attachmentsProvider(taskId)).value!;
    final images = attachments.where((a) => a.isImage).toList();
    final initialIndex = images.indexWhere((a) => a.id == attachmentId);

    return ImageGalleryView(taskId: taskId, images: images, initialIndex: initialIndex);
  }
}
