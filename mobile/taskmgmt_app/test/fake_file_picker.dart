import 'dart:typed_data';

import 'package:file_picker/file_picker.dart';

/// Thay thế FilePicker.platform trong widget test - bản mặc định gọi platform channel thật,
/// không có sẵn dưới flutter_test. Cấu hình [files] để giả lập kết quả chọn tệp, [result] = null
/// khi người dùng huỷ, hoặc [error] để giả lập PlatformException (ví dụ bị từ chối quyền).
class FakeFilePicker extends FilePicker {
  /// Danh sách file sẽ được "chọn"; null nghĩa là người dùng huỷ hộp thoại.
  List<PlatformFile>? files = [];

  /// Nếu khác null, pickFiles() ném lỗi này thay vì trả kết quả.
  Object? error;

  /// Số lần pickFiles() được gọi - để kiểm tra widget có gọi đúng nhánh "Tệp" hay không.
  int pickFilesCallCount = 0;

  /// Tạo nhanh một PlatformFile có sẵn bytes (widget chỉ dùng name + bytes).
  static PlatformFile file(String name, [List<int> bytes = const [1, 2, 3]]) => PlatformFile(
        name: name,
        size: bytes.length,
        bytes: Uint8List.fromList(bytes),
      );

  @override
  Future<FilePickerResult?> pickFiles({
    String? dialogTitle,
    String? initialDirectory,
    FileType type = FileType.any,
    List<String>? allowedExtensions,
    Function(FilePickerStatus)? onFileLoading,
    bool allowCompression = true,
    int compressionQuality = 30,
    bool allowMultiple = false,
    bool withData = false,
    bool withReadStream = false,
    bool lockParentWindow = false,
    bool readSequential = false,
  }) async {
    pickFilesCallCount++;
    if (error != null) throw error!;
    final picked = files;
    return picked == null ? null : FilePickerResult(picked);
  }
}
