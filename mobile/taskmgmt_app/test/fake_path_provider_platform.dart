import 'dart:io';

import 'package:path_provider_platform_interface/path_provider_platform_interface.dart';

/// Trả về thư mục temp thật của máy chạy test (Directory.systemTemp) thay vì gọi platform
/// channel - path_provider không có sẵn platform channel trong flutter_test.
class FakePathProviderPlatform extends PathProviderPlatform {
  @override
  Future<String?> getTemporaryPath() async => Directory.systemTemp.path;
}
