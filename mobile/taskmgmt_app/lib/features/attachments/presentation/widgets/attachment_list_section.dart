import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:image_picker/image_picker.dart';
import 'package:intl/intl.dart';
import 'package:open_filex/open_filex.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../shared/widgets/inline_empty_state.dart';
import '../../domain/entities/attachment.dart';
import '../providers/attachment_provider.dart';
import '../utils/attachment_temp_file.dart';
import '../utils/upload_queue.dart';
import 'upload_queue_list.dart';

String _formatSize(int bytes) {
  if (bytes < 1024) return '$bytes B';
  if (bytes < 1024 * 1024) return '${(bytes / 1024).toStringAsFixed(1)} KB';
  return '${(bytes / (1024 * 1024)).toStringAsFixed(1)} MB';
}

IconData _iconFor(String contentType) {
  if (contentType.startsWith('image/')) return Icons.image_outlined;
  if (contentType == 'application/pdf') return Icons.picture_as_pdf_outlined;
  if (contentType.contains('word')) return Icons.description_outlined;
  if (contentType.contains('sheet') || contentType.contains('excel')) return Icons.table_chart_outlined;
  if (contentType.contains('presentation') || contentType.contains('powerpoint')) return Icons.slideshow_outlined;
  return Icons.insert_drive_file_outlined;
}

/// Lỗi tải lên đã được dịch sang thông báo thân thiện. [runUploadQueue] hiển thị lỗi bằng
/// `e.toString()`, nên toString() ở đây trả về đúng thông báo, không kèm tiền tố "Exception:".
class _UploadFailure implements Exception {
  _UploadFailure(this.message);

  final String message;

  @override
  String toString() => message;
}

class AttachmentListSection extends ConsumerStatefulWidget {
  const AttachmentListSection({super.key, required this.taskId});

  final String taskId;

  @override
  ConsumerState<AttachmentListSection> createState() => _AttachmentListSectionState();
}

class _AttachmentListSectionState extends ConsumerState<AttachmentListSection> {
  List<UploadQueueItem> _queue = [];
  String? _openingAttachmentId;

  /// SnackBar hiện trên Scaffold phía sau bottom sheet, nên gọi được ngay cả khi sheet còn mở.
  void _showPickerError(String message) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
  }

  // Ba hàm chọn tệp đều nuốt lỗi và trả về [] thay vì ném: PlatformException (thường gặp nhất là
  // người dùng từ chối quyền máy ảnh/bộ nhớ) thoát khỏi onTap sẽ khiến Navigator.pop() không chạy
  // và bottom sheet kẹt lại không có lời giải thích nào.
  Future<List<PickedUpload>> _pickFromCamera() async {
    try {
      final xfile = await ImagePicker().pickImage(source: ImageSource.camera);
      if (xfile == null) return [];
      return [PickedUpload(fileName: xfile.name, bytes: await xfile.readAsBytes())];
    } catch (_) {
      _showPickerError('Không thể truy cập máy ảnh.');
      return [];
    }
  }

  Future<List<PickedUpload>> _pickFromGallery() async {
    try {
      final xfile = await ImagePicker().pickImage(source: ImageSource.gallery);
      if (xfile == null) return [];
      return [PickedUpload(fileName: xfile.name, bytes: await xfile.readAsBytes())];
    } catch (_) {
      _showPickerError('Không thể truy cập thư viện ảnh.');
      return [];
    }
  }

  Future<List<PickedUpload>> _pickFiles() async {
    try {
      final result = await FilePicker.platform.pickFiles(withData: true, allowMultiple: true);
      if (result == null) return [];
      return result.files
          .where((platformFile) => platformFile.bytes != null)
          .map((platformFile) => PickedUpload(fileName: platformFile.name, bytes: platformFile.bytes!))
          .toList();
    } catch (_) {
      _showPickerError('Không thể chọn tệp.');
      return [];
    }
  }

  Future<List<PickedUpload>?> _showSourceSheet() {
    return showModalBottomSheet<List<PickedUpload>>(
      context: context,
      builder: (sheetContext) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              leading: const Icon(Icons.photo_camera_outlined),
              title: const Text('Máy ảnh'),
              onTap: () async {
                final picked = await _pickFromCamera();
                if (sheetContext.mounted) Navigator.of(sheetContext).pop(picked);
              },
            ),
            ListTile(
              leading: const Icon(Icons.photo_library_outlined),
              title: const Text('Thư viện ảnh'),
              onTap: () async {
                final picked = await _pickFromGallery();
                if (sheetContext.mounted) Navigator.of(sheetContext).pop(picked);
              },
            ),
            ListTile(
              leading: const Icon(Icons.insert_drive_file_outlined),
              title: const Text('Tệp'),
              onTap: () async {
                final picked = await _pickFiles();
                if (sheetContext.mounted) Navigator.of(sheetContext).pop(picked);
              },
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _upload() async {
    final picked = await _showSourceSheet();
    if (picked == null || picked.isEmpty) return;

    final finalQueue = await runUploadQueue(
      files: picked,
      // refreshAfter: false - refresh 1 lần sau cả hàng đợi (bên dưới) thay vì sau từng file, để
      // danh sách bên dưới không sập thành spinner rồi bung lại theo từng file, và để một lần
      // refresh hỏng giữa chừng không đẩy danh sách sang trạng thái lỗi khi hàng đợi vẫn đang chạy.
      upload: (file, onSendProgress) async {
        try {
          await ref.read(attachmentsProvider(widget.taskId).notifier).uploadAttachment(
                fileName: file.fileName,
                bytes: file.bytes,
                onSendProgress: onSendProgress,
                refreshAfter: false,
              );
        } catch (e) {
          // runUploadQueue hiển thị e.toString() - dịch sang thông báo thân thiện để không lộ
          // thông báo lỗi thô của Dart/framework ra dòng tiến trình.
          throw _UploadFailure(e is ApiException ? e.message : 'Không thể tải lên tệp.');
        }
      },
      onUpdate: (queue) {
        if (mounted) setState(() => _queue = queue);
      },
    );

    if (!mounted) return;
    setState(() => _queue = []);
    final message = uploadFailureSnackBarMessage(finalQueue);
    if (message != null) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
    }

    // Refresh 1 lần cho cả hàng đợi để danh sách phản ánh những file đã tải lên thành công.
    // refresh() dùng AsyncValue.guard nên không ném; nếu hỏng, attachmentsProvider ở trạng thái
    // AsyncError và nhánh error: trong build() hiển thị thông báo.
    await ref.read(attachmentsProvider(widget.taskId).notifier).refresh();
  }

  Future<void> _openAttachment(Attachment attachment) async {
    if (attachment.canPreviewInApp) {
      context.push('/tasks/${widget.taskId}/attachments/${attachment.id}');
      return;
    }

    setState(() => _openingAttachmentId = attachment.id);
    try {
      final file = await downloadAttachmentToTempFile(
        download: () => ref.read(attachmentsProvider(widget.taskId).notifier).downloadAttachment(attachment.id),
        attachmentId: attachment.id,
        fileName: attachment.fileName,
      );

      final result = await OpenFilex.open(file.path);
      if (result.type != ResultType.done && mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Không thể mở tệp: ${result.message}')));
      }
    } catch (e) {
      if (!mounted) return;
      final message = e is ApiException ? e.message : 'Không thể mở tệp.';
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
    } finally {
      if (mounted) setState(() => _openingAttachmentId = null);
    }
  }

  Future<void> _delete(Attachment attachment) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Xoá tệp đính kèm?'),
        content: Text('Xoá "${attachment.fileName}"?'),
        actions: [
          TextButton(onPressed: () => Navigator.of(context).pop(false), child: const Text('Huỷ')),
          FilledButton(onPressed: () => Navigator.of(context).pop(true), child: const Text('Xoá')),
        ],
      ),
    );
    if (confirmed != true) return;

    try {
      await ref.read(attachmentsProvider(widget.taskId).notifier).deleteAttachment(attachment.id);
    } catch (e) {
      if (!mounted) return;
      final message = e is ApiException ? e.message : 'Không thể xoá tệp.';
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
    }
  }

  @override
  Widget build(BuildContext context) {
    final attachmentsAsync = ref.watch(attachmentsProvider(widget.taskId));

    return Card(
      margin: EdgeInsets.zero,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text('Tệp đính kèm', style: Theme.of(context).textTheme.titleMedium),
                IconButton(
                  icon: _queue.isNotEmpty
                      ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                      : const Icon(Icons.attach_file),
                  tooltip: 'Tải tệp lên',
                  onPressed: _queue.isNotEmpty ? null : _upload,
                ),
              ],
            ),
            if (_queue.isNotEmpty) ...[
              const SizedBox(height: 8),
              UploadQueueList(queue: _queue),
            ],
            attachmentsAsync.when(
              data: (attachments) {
                if (attachments.isEmpty) {
                  return const InlineEmptyState(icon: Icons.attach_file, message: 'Chưa có tệp đính kèm nào.');
                }
                return Column(
                  children: attachments
                      .map(
                        (attachment) => ListTile(
                          contentPadding: EdgeInsets.zero,
                          leading: Icon(_iconFor(attachment.contentType)),
                          title: Text(attachment.fileName, overflow: TextOverflow.ellipsis),
                          subtitle: Text(
                            '${_formatSize(attachment.sizeBytes)} • ${attachment.uploadedByFullName} • '
                            '${DateFormat('dd/MM/yyyy HH:mm').format(attachment.createdAtUtc.toLocal())}',
                          ),
                          trailing: _openingAttachmentId == attachment.id
                              ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                              : IconButton(
                                  icon: const Icon(Icons.delete_outline),
                                  tooltip: 'Xoá',
                                  onPressed: () => _delete(attachment),
                                ),
                          onTap: _openingAttachmentId == null ? () => _openAttachment(attachment) : null,
                        ),
                      )
                      .toList(),
                );
              },
              loading: () => const Padding(
                padding: EdgeInsets.symmetric(vertical: 16),
                child: Center(child: CircularProgressIndicator()),
              ),
              error: (error, _) => Padding(
                padding: const EdgeInsets.symmetric(vertical: 8),
                child: Text(
                  error is ApiException ? error.message : 'Không tải được danh sách tệp đính kèm.',
                  style: TextStyle(color: Theme.of(context).colorScheme.error),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
