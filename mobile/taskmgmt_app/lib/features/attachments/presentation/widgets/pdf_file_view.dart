import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter_pdfview/flutter_pdfview.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../shared/widgets/error_state_view.dart';
import '../../domain/entities/attachment.dart';
import '../providers/attachment_provider.dart';
import '../utils/attachment_temp_file.dart';

class PdfFileView extends ConsumerStatefulWidget {
  const PdfFileView({super.key, required this.taskId, required this.attachment});

  final String taskId;
  final Attachment attachment;

  @override
  ConsumerState<PdfFileView> createState() => _PdfFileViewState();
}

class _PdfFileViewState extends ConsumerState<PdfFileView> {
  late Future<File> _fileFuture;

  @override
  void initState() {
    super.initState();
    _fileFuture = _download();
  }

  Future<File> _download() => downloadAttachmentToTempFile(
        download: () => ref.read(attachmentsProvider(widget.taskId).notifier).downloadAttachment(widget.attachment.id),
        attachmentId: widget.attachment.id,
        fileName: widget.attachment.fileName,
      );

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<File>(
      future: _fileFuture,
      builder: (context, snapshot) {
        if (snapshot.connectionState != ConnectionState.done) {
          return const Center(child: CircularProgressIndicator());
        }
        if (snapshot.hasError) {
          final error = snapshot.error;
          return ErrorStateView(
            message: error is ApiException ? error.message : 'Không tải được tệp PDF.',
            onRetry: () {
              final future = _download();
              // Bắt lỗi giả (no-op) ngay để Future không bị coi là "unhandled" trong khoảng thời
              // gian giữa lúc tạo và lúc FutureBuilder gắn listener thật ở lần build kế tiếp -
              // lỗi thật vẫn được FutureBuilder xử lý bình thường qua snapshot.hasError.
              future.then((_) {}, onError: (_) {});
              setState(() {
                _fileFuture = future;
              });
            },
          );
        }
        return PDFView(
          filePath: snapshot.data!.path,
          onError: (error) => debugPrint('PDFView error: $error'),
          onPageError: (page, error) => debugPrint('PDFView page $page error: $error'),
        );
      },
    );
  }
}
