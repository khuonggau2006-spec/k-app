import 'dart:io';
import 'dart:typed_data';

import 'package:path_provider/path_provider.dart';

/// Tải bytes tệp đính kèm (qua [download]) rồi ghi ra thư mục tạm của thiết bị - dùng chung cho
/// luồng "mở bằng app ngoài" (OpenFilex, xem AttachmentListSection) và các viewer trong app
/// (PDF/video) cần 1 file path cục bộ. Nhận [download] dạng closure thay vì WidgetRef/Riverpod
/// trực tiếp để hàm này test được độc lập, không cần dựng widget tree.
Future<File> downloadAttachmentToTempFile({
  required Future<Uint8List> Function() download,
  required String attachmentId,
  required String fileName,
}) async {
  final bytes = await download();

  final dir = await getTemporaryDirectory();
  final file = File('${dir.path}/attachments/${attachmentId}_$fileName');
  await file.create(recursive: true);
  await file.writeAsBytes(bytes);
  return file;
}
