import 'dart:typed_data';

import 'package:flutter_test/flutter_test.dart';
import 'package:path_provider_platform_interface/path_provider_platform_interface.dart';

import 'package:taskmgmt_app/features/attachments/presentation/utils/attachment_temp_file.dart';

import 'fake_path_provider_platform.dart';

void main() {
  setUpAll(() {
    PathProviderPlatform.instance = FakePathProviderPlatform();
  });

  test('Downloads bytes and writes them to a temp file named after the attachment', () async {
    final bytes = Uint8List.fromList([1, 2, 3, 4]);

    final file = await downloadAttachmentToTempFile(
      download: () async => bytes,
      attachmentId: 'a1',
      fileName: 'bao-cao.pdf',
    );

    expect(file.path, endsWith('attachments/a1_bao-cao.pdf'));
    expect(await file.readAsBytes(), bytes);

    await file.parent.delete(recursive: true);
  });

  test('Propagates the download error without writing any file', () async {
    await expectLater(
      downloadAttachmentToTempFile(
        download: () async => throw Exception('network error'),
        attachmentId: 'a2',
        fileName: 'x.pdf',
      ),
      throwsException,
    );
  });
}
