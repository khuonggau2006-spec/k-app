import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:photo_view/photo_view.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../shared/widgets/error_state_view.dart';
import '../../domain/entities/attachment.dart';
import '../providers/attachment_provider.dart';

class ImageGalleryView extends ConsumerStatefulWidget {
  const ImageGalleryView({super.key, required this.taskId, required this.images, required this.initialIndex});

  final String taskId;
  final List<Attachment> images;
  final int initialIndex;

  @override
  ConsumerState<ImageGalleryView> createState() => _ImageGalleryViewState();
}

class _ImageGalleryViewState extends ConsumerState<ImageGalleryView> {
  late final PageController _pageController = PageController(initialPage: widget.initialIndex);
  final Map<String, Uint8List> _cache = {};
  final Map<String, Future<Uint8List>> _inFlight = {};

  @override
  void dispose() {
    _pageController.dispose();
    super.dispose();
  }

  // Cache theo id + gộp các lần gọi trùng nhau khi widget rebuild - tránh tải lại khi lướt qua
  // lướt lại (FutureBuilder coi 2 Future khác instance là 2 lần tải khác nhau dù cùng ảnh).
  Future<Uint8List> _load(Attachment image) {
    final cached = _cache[image.id];
    if (cached != null) return Future.value(cached);

    return _inFlight.putIfAbsent(image.id, () async {
      try {
        final bytes = await ref.read(attachmentsProvider(widget.taskId).notifier).downloadAttachment(image.id);
        _cache[image.id] = bytes;
        return bytes;
      } finally {
        _inFlight.remove(image.id);
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return PageView.builder(
      controller: _pageController,
      itemCount: widget.images.length,
      itemBuilder: (context, index) {
        final image = widget.images[index];
        return FutureBuilder<Uint8List>(
          key: ValueKey(image.id),
          future: _load(image),
          builder: (context, snapshot) {
            if (snapshot.connectionState != ConnectionState.done) {
              return const Center(child: CircularProgressIndicator());
            }
            if (snapshot.hasError) {
              final error = snapshot.error;
              return ErrorStateView(
                message: error is ApiException ? error.message : 'Không tải được ảnh.',
                onRetry: () => setState(() {}),
              );
            }
            return PhotoView(
              imageProvider: MemoryImage(snapshot.data!),
              // PhotoView tự giải mã pixel (bước riêng, sau khi bytes đã tải xong) và mặc định hiện
              // CircularProgressIndicator (animation vô hạn) trong lúc chờ - animation này không bao giờ
              // dừng tự nhiên nên chặn SchedulerBinding chạy tác vụ giải mã nền (Priority.animation),
              // gây treo trong widget test (pumpAndSettle không bao giờ settle). Bước giải mã 1 ảnh vốn
              // rất nhanh trong thực tế nên bỏ qua chỉ báo loading riêng của PhotoView là an toàn.
              loadingBuilder: (context, event) => const SizedBox.shrink(),
            );
          },
        );
      },
    );
  }
}
