import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import 'package:open_filex/open_filex.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../shared/widgets/inline_empty_state.dart';
import '../../domain/entities/attachment.dart';
import '../providers/attachment_provider.dart';
import '../utils/attachment_temp_file.dart';

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

class AttachmentListSection extends ConsumerStatefulWidget {
  const AttachmentListSection({super.key, required this.taskId});

  final String taskId;

  @override
  ConsumerState<AttachmentListSection> createState() => _AttachmentListSectionState();
}

class _AttachmentListSectionState extends ConsumerState<AttachmentListSection> {
  bool _isUploading = false;
  String? _openingAttachmentId;

  Future<void> _upload() async {
    final result = await FilePicker.platform.pickFiles(withData: true);
    final file = result?.files.singleOrNull;
    if (file?.bytes == null) return;

    setState(() => _isUploading = true);
    try {
      await ref
          .read(attachmentsProvider(widget.taskId).notifier)
          .uploadAttachment(fileName: file!.name, bytes: file.bytes!);
    } catch (e) {
      if (!mounted) return;
      final message = e is ApiException ? e.message : 'Không thể tải lên tệp.';
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
    } finally {
      if (mounted) setState(() => _isUploading = false);
    }
  }

  Future<void> _openAttachment(Attachment attachment) async {
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
                  icon: _isUploading
                      ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                      : const Icon(Icons.attach_file),
                  tooltip: 'Tải tệp lên',
                  onPressed: _isUploading ? null : _upload,
                ),
              ],
            ),
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
