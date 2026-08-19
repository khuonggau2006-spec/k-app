import 'dart:typed_data';

class PickedUpload {
  const PickedUpload({required this.fileName, required this.bytes});

  final String fileName;
  final Uint8List bytes;
}

enum UploadItemStatus { uploading, done, error }

class UploadQueueItem {
  UploadQueueItem({
    required this.fileName,
    this.status = UploadItemStatus.uploading,
    this.progress = 0,
    this.errorMessage,
  });

  final String fileName;
  UploadItemStatus status;
  double progress;
  String? errorMessage;
}

List<UploadQueueItem> _snapshot(List<UploadQueueItem> queue) => queue
    .map(
      (item) => UploadQueueItem(
        fileName: item.fileName,
        status: item.status,
        progress: item.progress,
        errorMessage: item.errorMessage,
      ),
    )
    .toList();

/// Tải lần lượt từng file trong [files] qua [upload]; lỗi ở 1 file không dừng hàng đợi,
/// các file còn lại vẫn tiếp tục. Gọi [onUpdate] với 1 bản sao (snapshot) độc lập của hàng đợi
/// sau mỗi lần tiến trình đổi và sau mỗi file hoàn tất/lỗi, để bên gọi (widget) hiển thị lại UI
/// bằng setState mà không lo dữ liệu đã hiển thị bị đổi ngầm bởi file tiếp theo.
Future<List<UploadQueueItem>> runUploadQueue({
  required List<PickedUpload> files,
  required Future<void> Function(PickedUpload file, void Function(int sent, int total) onSendProgress) upload,
  required void Function(List<UploadQueueItem> queue) onUpdate,
}) async {
  final queue = files.map((file) => UploadQueueItem(fileName: file.fileName)).toList();

  for (var i = 0; i < queue.length; i++) {
    final item = queue[i];
    try {
      await upload(files[i], (sent, total) {
        item.progress = total > 0 ? sent / total : 0;
        onUpdate(_snapshot(queue));
      });
      item
        ..status = UploadItemStatus.done
        ..progress = 1;
    } catch (e) {
      item
        ..status = UploadItemStatus.error
        ..errorMessage = e.toString();
    }
    onUpdate(_snapshot(queue));
  }

  return queue;
}

/// Gộp thông báo lỗi cho SnackBar sau khi hàng đợi chạy xong; null nếu không có file nào lỗi.
String? uploadFailureSnackBarMessage(List<UploadQueueItem> queue) {
  final failed = queue.where((item) => item.status == UploadItemStatus.error).toList();
  if (failed.isEmpty) return null;
  return 'Tải lên thất bại: ${failed.map((item) => item.fileName).join(', ')}';
}
