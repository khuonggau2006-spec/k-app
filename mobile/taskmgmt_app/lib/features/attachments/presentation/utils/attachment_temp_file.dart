import 'dart:io';
import 'dart:typed_data';

import 'package:path_provider/path_provider.dart';

/// Tải bytes tệp đính kèm (qua [download]) rồi ghi ra thư mục tạm của thiết bị - dùng chung cho
/// luồng "mở bằng app ngoài" (OpenFilex, xem AttachmentListSection) và các viewer trong app
/// (PDF/video) cần 1 file path cục bộ. Nhận [download] dạng closure thay vì WidgetRef/Riverpod
/// trực tiếp để hàm này test được độc lập, không cần dựng widget tree.
/// [fileName] đến từ server (Attachment.FileName) và chỉ được validate non-empty/length/extension
/// ở backend, không chặn ký tự path traversal - nên chỉ giữ lại phần tên tệp cuối cùng (bỏ mọi
/// thư mục cha "../") trước khi ghép vào đường dẫn ghi file cục bộ.
Future<File> downloadAttachmentToTempFile({
  required Future<Uint8List> Function() download,
  required String attachmentId,
  required String fileName,
}) async {
  final bytes = await download();

  final dir = await getTemporaryDirectory();
  final safeFileName = fileName.split(RegExp(r'[/\\]')).last;
  final file = File('${dir.path}/attachments/${attachmentId}_$safeFileName');
  await file.create(recursive: true);
  await file.writeAsBytes(bytes);
  return file;
}
