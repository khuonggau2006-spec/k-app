import 'package:chewie/chewie.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:video_player/video_player.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../shared/widgets/error_state_view.dart';
import '../../domain/entities/attachment.dart';
import '../providers/attachment_provider.dart';
import '../utils/attachment_temp_file.dart';

class VideoFileView extends ConsumerStatefulWidget {
  const VideoFileView({super.key, required this.taskId, required this.attachment});

  final String taskId;
  final Attachment attachment;

  @override
  ConsumerState<VideoFileView> createState() => _VideoFileViewState();
}

class _VideoFileViewState extends ConsumerState<VideoFileView> {
  late Future<ChewieController> _controllerFuture;

  @override
  void initState() {
    super.initState();
    _controllerFuture = _load();
  }

  Future<ChewieController> _load() async {
    final file = await downloadAttachmentToTempFile(
      download: () => ref.read(attachmentsProvider(widget.taskId).notifier).downloadAttachment(widget.attachment.id),
      attachmentId: widget.attachment.id,
      fileName: widget.attachment.fileName,
    );

    final videoController = VideoPlayerController.file(file);
    await videoController.initialize();
    return ChewieController(videoPlayerController: videoController, autoPlay: false, looping: false);
  }

  @override
  void dispose() {
    _controllerFuture.then((controller) {
      controller.videoPlayerController.dispose();
      controller.dispose();
    }, onError: (_) {});
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<ChewieController>(
      future: _controllerFuture,
      builder: (context, snapshot) {
        if (snapshot.connectionState != ConnectionState.done) {
          return const Center(child: CircularProgressIndicator());
        }
        if (snapshot.hasError) {
          final error = snapshot.error;
          return ErrorStateView(
            message: error is ApiException ? error.message : 'Không tải được video.',
            onRetry: () {
              final future = _load();
              // Bắt lỗi giả (no-op) ngay để Future không bị coi là "unhandled" trong khoảng thời
              // gian giữa lúc tạo và lúc FutureBuilder gắn listener thật ở lần build kế tiếp -
              // lỗi thật vẫn được FutureBuilder xử lý bình thường qua snapshot.hasError.
              future.then((_) {}, onError: (_) {});
              setState(() {
                _controllerFuture = future;
              });
            },
          );
        }
        return Chewie(controller: snapshot.data!);
      },
    );
  }
}
