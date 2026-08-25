import 'dart:typed_data';

import 'package:image_picker_platform_interface/image_picker_platform_interface.dart';

/// Thay thế ImagePickerPlatform.instance trong widget test - bản mặc định
/// (MethodChannelImagePicker) gọi platform channel thật, không có sẵn dưới flutter_test.
///
/// ImagePicker().pickImage(source: ...) uỷ quyền cho getImageFromSource(), nên chỉ cần override
/// đúng hàm này. XFile.fromData() giữ dữ liệu trong bộ nhớ nên readAsBytes() chạy được mà không
/// cần file thật trên đĩa; phải truyền cả `path` vì bản dart:io của XFile suy ra `name` từ path
/// và bỏ qua tham số `name` (tham số đó chỉ dùng cho bản web).
class FakeImagePickerPlatform extends ImagePickerPlatform {
  /// Tên file sẽ được "chụp/chọn"; null nghĩa là người dùng huỷ.
  String? fileName;

  /// Nếu khác null, getImageFromSource() ném lỗi này thay vì trả kết quả.
  Object? error;

  /// Nguồn (camera/gallery) của lần gọi gần nhất - để kiểm tra widget gọi đúng nhánh.
  ImageSource? lastSource;

  @override
  Future<XFile?> getImageFromSource({
    required ImageSource source,
    ImagePickerOptions options = const ImagePickerOptions(),
  }) async {
    lastSource = source;
    if (error != null) throw error!;
    final name = fileName;
    if (name == null) return null;
    return XFile.fromData(Uint8List.fromList([1, 2, 3]), path: name, name: name);
  }
}
