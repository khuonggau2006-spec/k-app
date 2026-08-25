import 'dart:async';

import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:image_picker_platform_interface/image_picker_platform_interface.dart';

import 'package:taskmgmt_app/core/network/api_exception.dart';
import 'package:taskmgmt_app/features/attachments/domain/entities/attachment.dart';
import 'package:taskmgmt_app/features/attachments/domain/repositories/attachment_repository.dart';
import 'package:taskmgmt_app/features/attachments/presentation/providers/attachment_provider.dart';
import 'package:taskmgmt_app/features/attachments/presentation/widgets/attachment_list_section.dart';
import 'package:taskmgmt_app/features/attachments/presentation/widgets/upload_queue_list.dart';

import 'fake_file_picker.dart';
import 'fake_image_picker_platform.dart';

const _taskId = 'task-1';

Attachment _attachment(String fileName) => Attachment(
      id: fileName,
      workTaskId: _taskId,
      fileName: fileName,
      contentType: 'image/png',
      sizeBytes: 3,
      uploadedByUserId: 'u1',
      uploadedByFullName: 'Người Dùng A',
      uploadedByEmail: 'a@example.com',
      createdAtUtc: DateTime.utc(2026, 8, 18),
    );

/// Repository giả điều khiển được: [gates] giữ 1 lần tải lên ở trạng thái đang chạy để test có
/// thể pump giữa chừng, [uploadErrors] ép 1 file cụ thể lỗi, [getAttachmentsError] ép lần
/// getAttachments thứ N trở đi lỗi (mô phỏng refresh hỏng sau khi POST đã thành công).
class _FakeAttachmentRepository implements AttachmentRepository {
  List<Attachment> attachments = [];
  final List<String> uploadedFileNames = [];
  final Map<String, Completer<void>> gates = {};
  final Map<String, Object> uploadErrors = {};
  int getAttachmentsCallCount = 0;
  int? failGetAttachmentsFromCall;

  @override
  Future<List<Attachment>> getAttachments(String workTaskId) async {
    getAttachmentsCallCount++;
    final failFrom = failGetAttachmentsFromCall;
    if (failFrom != null && getAttachmentsCallCount >= failFrom) {
      throw const ApiException('Không tải được danh sách.');
    }
    return List.of(attachments);
  }

  @override
  Future<Attachment> uploadAttachment({
    required String workTaskId,
    required String fileName,
    required Uint8List bytes,
    void Function(int sent, int total)? onSendProgress,
  }) async {
    uploadedFileNames.add(fileName);
    // Báo tiến trình ngay để widget dựng UploadQueueList trước khi lần tải lên kết thúc.
    onSendProgress?.call(1, 2);
    await gates[fileName]?.future;
    final error = uploadErrors[fileName];
    if (error != null) throw error;
    attachments = [...attachments, _attachment(fileName)];
    return _attachment(fileName);
  }

  @override
  Future<Uint8List> downloadAttachment({required String workTaskId, required String attachmentId}) =>
      throw UnimplementedError();

  @override
  Future<void> deleteAttachment({required String workTaskId, required String attachmentId}) async {}
}

Widget _buildSection(AttachmentRepository repository) => ProviderScope(
      overrides: [attachmentRepositoryProvider.overrideWithValue(repository)],
      child: const MaterialApp(
        home: Scaffold(body: AttachmentListSection(taskId: _taskId)),
      ),
    );

/// Mở bottom sheet chọn nguồn và chờ nó hiện xong.
Future<void> _openSourceSheet(WidgetTester tester) async {
  // byTooltip chứ không byIcon: InlineEmptyState của danh sách rỗng cũng dùng Icons.attach_file.
  await tester.tap(find.byTooltip('Tải tệp lên'));
  await tester.pumpAndSettle();
}

/// Chạm 1 lựa chọn trong bottom sheet rồi bơm qua animation đóng sheet, để Future của
/// showModalBottomSheet hoàn tất và _upload() chạy tiếp. Không dùng pumpAndSettle vì
/// LinearProgressIndicator của hàng đợi chạy animation vô hạn.
Future<void> _tapSourceOption(WidgetTester tester, String label) async {
  await tester.tap(find.text(label));
  await tester.pump();
  await tester.pump(const Duration(milliseconds: 500));
  await tester.pump();
}

void main() {
  late FakeFilePicker filePicker;
  late FakeImagePickerPlatform imagePicker;

  setUp(() {
    filePicker = FakeFilePicker();
    imagePicker = FakeImagePickerPlatform();
    FilePicker.platform = filePicker;
    ImagePickerPlatform.instance = imagePicker;
  });

  testWidgets('Attach icon opens a bottom sheet with exactly the 3 pick sources', (tester) async {
    await tester.pumpWidget(_buildSection(_FakeAttachmentRepository()));
    await tester.pumpAndSettle();

    await _openSourceSheet(tester);

    expect(find.text('Máy ảnh'), findsOneWidget);
    expect(find.text('Thư viện ảnh'), findsOneWidget);
    expect(find.text('Tệp'), findsOneWidget);
    // Danh sách đính kèm đang rỗng (InlineEmptyState) nên mọi ListTile đều thuộc bottom sheet.
    expect(find.byType(ListTile), findsNWidgets(3));
  });

  testWidgets('Picking 2 files queues both and renders UploadQueueList while uploading', (tester) async {
    final repository = _FakeAttachmentRepository();
    repository.gates['a.png'] = Completer<void>();
    filePicker.files = [FakeFilePicker.file('a.png'), FakeFilePicker.file('b.png')];

    await tester.pumpWidget(_buildSection(repository));
    await tester.pumpAndSettle();

    await _openSourceSheet(tester);
    await _tapSourceOption(tester, 'Tệp');

    expect(filePicker.pickFilesCallCount, 1);
    expect(find.byType(UploadQueueList), findsOneWidget);
    expect(find.text('a.png'), findsOneWidget);
    expect(find.text('b.png'), findsOneWidget);
    expect(find.text('50%'), findsOneWidget);

    repository.gates['a.png']!.complete();
    await tester.pumpAndSettle();

    expect(repository.uploadedFileNames, ['a.png', 'b.png']);
    expect(find.byType(UploadQueueList), findsNothing);
    expect(find.text('Chưa có tệp đính kèm nào.'), findsNothing);
  });

  testWidgets('A failed file clears the queue and shows the aggregate failure SnackBar', (tester) async {
    final repository = _FakeAttachmentRepository();
    repository.uploadErrors['b.png'] = const ApiException('Tệp quá lớn.');
    filePicker.files = [FakeFilePicker.file('a.png'), FakeFilePicker.file('b.png')];

    await tester.pumpWidget(_buildSection(repository));
    await tester.pumpAndSettle();

    await _openSourceSheet(tester);
    await _tapSourceOption(tester, 'Tệp');
    await tester.pumpAndSettle();

    expect(find.byType(UploadQueueList), findsNothing);
    expect(find.text('Tải lên thất bại: b.png'), findsOneWidget);
  });

  testWidgets('Maps a non-ApiException upload error to the friendly message in the progress row', (tester) async {
    final repository = _FakeAttachmentRepository();
    repository.gates['a.png'] = Completer<void>();
    // b.png bị giữ lại để hàng đợi chưa kết thúc, nhờ đó quan sát được dòng lỗi của a.png.
    repository.gates['b.png'] = Completer<void>();
    repository.uploadErrors['a.png'] = ArgumentError('Null check operator used on a null value');
    filePicker.files = [FakeFilePicker.file('a.png'), FakeFilePicker.file('b.png')];

    await tester.pumpWidget(_buildSection(repository));
    await tester.pumpAndSettle();

    await _openSourceSheet(tester);
    await _tapSourceOption(tester, 'Tệp');

    repository.gates['a.png']!.complete();
    await tester.pump();
    await tester.pump();

    expect(find.text('Không thể tải lên tệp.'), findsOneWidget);
    expect(find.textContaining('Null check operator'), findsNothing);

    repository.gates['b.png']!.complete();
    await tester.pumpAndSettle();
  });

  testWidgets('A refresh failure after a successful upload is not reported as an upload failure', (tester) async {
    final repository = _FakeAttachmentRepository();
    filePicker.files = [FakeFilePicker.file('a.png')];

    await tester.pumpWidget(_buildSection(repository));
    await tester.pumpAndSettle();

    // Lần getAttachments đầu là build(); lần thứ 2 là refresh cuối hàng đợi -> ép lỗi.
    repository.failGetAttachmentsFromCall = 2;

    await _openSourceSheet(tester);
    await _tapSourceOption(tester, 'Tệp');
    await tester.pumpAndSettle();

    expect(repository.uploadedFileNames, ['a.png']);
    expect(find.byType(UploadQueueList), findsNothing);
    expect(find.textContaining('Tải lên thất bại'), findsNothing);
    // Lỗi refresh hiển thị qua nhánh error: của attachmentsProvider, không phải qua hàng đợi.
    expect(find.text('Không tải được danh sách.'), findsOneWidget);
  });

  testWidgets('Uploading many files refreshes the attachment list exactly once', (tester) async {
    final repository = _FakeAttachmentRepository();
    filePicker.files = [
      FakeFilePicker.file('a.png'),
      FakeFilePicker.file('b.png'),
      FakeFilePicker.file('c.png'),
    ];

    await tester.pumpWidget(_buildSection(repository));
    await tester.pumpAndSettle();
    expect(repository.getAttachmentsCallCount, 1);

    await _openSourceSheet(tester);
    await _tapSourceOption(tester, 'Tệp');
    await tester.pumpAndSettle();

    expect(repository.uploadedFileNames, ['a.png', 'b.png', 'c.png']);
    expect(repository.getAttachmentsCallCount, 2);
  });

  testWidgets('Camera source picks through ImagePicker and uploads the photo', (tester) async {
    final repository = _FakeAttachmentRepository();
    imagePicker.fileName = 'anh.png';

    await tester.pumpWidget(_buildSection(repository));
    await tester.pumpAndSettle();

    await _openSourceSheet(tester);
    await _tapSourceOption(tester, 'Máy ảnh');
    await tester.pumpAndSettle();

    expect(imagePicker.lastSource, ImageSource.camera);
    expect(repository.uploadedFileNames, ['anh.png']);
    expect(find.byType(UploadQueueList), findsNothing);
  });

  testWidgets('A camera PlatformException closes the sheet and shows an explanatory SnackBar', (tester) async {
    final repository = _FakeAttachmentRepository();
    imagePicker.error = PlatformException(code: 'camera_access_denied');

    await tester.pumpWidget(_buildSection(repository));
    await tester.pumpAndSettle();

    await _openSourceSheet(tester);
    await _tapSourceOption(tester, 'Máy ảnh');
    await tester.pumpAndSettle();

    expect(find.text('Máy ảnh'), findsNothing, reason: 'bottom sheet phải đóng chứ không kẹt lại');
    expect(find.text('Không thể truy cập máy ảnh.'), findsOneWidget);
    expect(repository.uploadedFileNames, isEmpty);
  });

  testWidgets('A gallery PlatformException closes the sheet and shows an explanatory SnackBar', (tester) async {
    final repository = _FakeAttachmentRepository();
    imagePicker.error = PlatformException(code: 'photo_access_denied');

    await tester.pumpWidget(_buildSection(repository));
    await tester.pumpAndSettle();

    await _openSourceSheet(tester);
    await _tapSourceOption(tester, 'Thư viện ảnh');
    await tester.pumpAndSettle();

    expect(find.text('Thư viện ảnh'), findsNothing);
    expect(find.text('Không thể truy cập thư viện ảnh.'), findsOneWidget);
  });

  testWidgets('A file picker exception closes the sheet and shows an explanatory SnackBar', (tester) async {
    final repository = _FakeAttachmentRepository();
    filePicker.error = PlatformException(code: 'read_external_storage_denied');

    await tester.pumpWidget(_buildSection(repository));
    await tester.pumpAndSettle();

    await _openSourceSheet(tester);
    await _tapSourceOption(tester, 'Tệp');
    await tester.pumpAndSettle();

    expect(find.text('Tệp'), findsNothing);
    expect(find.text('Không thể chọn tệp.'), findsOneWidget);
    expect(repository.uploadedFileNames, isEmpty);
  });

  testWidgets('Cancelling the file picker leaves the queue empty and shows no SnackBar', (tester) async {
    final repository = _FakeAttachmentRepository();
    filePicker.files = null;

    await tester.pumpWidget(_buildSection(repository));
    await tester.pumpAndSettle();

    await _openSourceSheet(tester);
    await _tapSourceOption(tester, 'Tệp');
    await tester.pumpAndSettle();

    expect(find.byType(UploadQueueList), findsNothing);
    expect(find.byType(SnackBar), findsNothing);
    expect(repository.getAttachmentsCallCount, 1, reason: 'huỷ chọn thì không refresh');
  });
}
