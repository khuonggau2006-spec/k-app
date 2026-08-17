# Trình xem tệp đính kèm trong app Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Xem trước ảnh (lướt ngang giữa các ảnh cùng task, pinch-zoom), PDF, và video ngay trong app thay vì luôn giao cho ứng dụng ngoài mở (`OpenFilex`).

**Architecture:** Một `AttachmentViewerScreen` (route `/tasks/:taskId/attachments/:attachmentId`) đọc `contentType` rồi build 1 trong 3 widget con (`ImageGalleryView`/`PdfFileView`/`VideoFileView`). Logic "tải bytes rồi ghi ra file tạm" (đang có sẵn, lặp lại cho từng loại) được tách thành 1 hàm dùng chung, tham số hoá qua closure `download` thay vì phụ thuộc trực tiếp `WidgetRef`/Riverpod — vừa DRY vừa dễ unit test độc lập không cần dựng widget tree. Backend không đổi gì (endpoint download đã trả đủ bytes + content-type).

**Tech Stack:** Flutter/Riverpod/go_router (có sẵn), `photo_view` + `flutter_pdfview` + `video_player` + `chewie` (mới), `path_provider_platform_interface` (dev, để test phần ghi file tạm không cần platform channel thật).

## Global Constraints

- 2-space indent Dart (theo style hiện có trong repo), file mirror namespace theo thư mục.
- Chỉ xem trước trong app cho 3 loại: ảnh (`contentType` bắt đầu `image/`), PDF (`application/pdf`), video (`contentType` bắt đầu `video/`). Loại khác giữ nguyên hành vi `OpenFilex` mở app ngoài — KHÔNG route vào `AttachmentViewerScreen`.
- Không sửa `AttachmentRepository`/`AttachmentsController`/backend — endpoint download đã đủ dùng.
- Lỗi tải (mạng, 404...) hiện `ErrorStateView` có sẵn (`shared/widgets/error_state_view.dart`, `message` + `onRetry`) — không tự chế UI lỗi mới.
- Cache ảnh trong `ImageGalleryView` chỉ sống trong vòng đời widget, không cần bền vững qua các lần mở lại màn hình.

Spec đầy đủ: `docs/superpowers/specs/2026-08-17-attachment-viewer-design.md`.

---

### Task 1: Thêm dependencies

**Files:**
- Modify: `mobile/taskmgmt_app/pubspec.yaml`

**Interfaces:**
- Produces: 4 package mới sẵn sàng import (`photo_view`, `flutter_pdfview`, `video_player`, `chewie`) + 1 dev dependency (`path_provider_platform_interface`) cho Task 2 dùng test.

- [ ] **Step 1: Thêm dependency**

Trong `mobile/taskmgmt_app/pubspec.yaml`, thêm vào cuối khối `dependencies:` (sau `open_filex: ^4.7.0`):

```yaml
  photo_view: ^0.15.0
  flutter_pdfview: ^1.4.5
  video_player: ^2.11.1
  chewie: ^1.15.0
```

Và thêm vào cuối khối `dev_dependencies:` (sau `freezed: ^4.0.0-dev.3`):

```yaml
  path_provider_platform_interface: ^2.1.2
```

- [ ] **Step 2: Cài đặt**

Run: `cd mobile/taskmgmt_app && flutter pub get`
Expected: chạy thành công, không lỗi resolve version.

- [ ] **Step 3: Phân tích tĩnh**

Run: `flutter analyze`
Expected: không lỗi/warning mới (4 package chưa được import ở đâu nên chỉ là thêm dependency, chưa có code dùng).

- [ ] **Step 4: Commit**

```bash
cd d:/projects/K-app
git add mobile/taskmgmt_app/pubspec.yaml mobile/taskmgmt_app/pubspec.lock
git commit -m "feat(mobile): add photo_view, flutter_pdfview, video_player, chewie dependencies"
```

---

### Task 2: Hàm dùng chung `downloadAttachmentToTempFile` + refactor `_openAttachment`

**Files:**
- Create: `mobile/taskmgmt_app/lib/features/attachments/presentation/utils/attachment_temp_file.dart`
- Create: `mobile/taskmgmt_app/test/fake_path_provider_platform.dart`
- Test: `mobile/taskmgmt_app/test/attachment_temp_file_test.dart`
- Modify: `mobile/taskmgmt_app/lib/features/attachments/presentation/widgets/attachment_list_section.dart`

**Interfaces:**
- Produces: `Future<File> downloadAttachmentToTempFile({required Future<Uint8List> Function() download, required String attachmentId, required String fileName})`. Task 4, 5 tiêu thụ. `FakePathProviderPlatform` (test helper) — Task 4, 5 test không cần nó (xem lý do ở mục đó), nhưng để sẵn cho ai cần sau này.

- [ ] **Step 1: Tạo fake `PathProviderPlatform` dùng chung cho test**

`getTemporaryDirectory()` (package `path_provider`) gọi platform channel thật — không hoạt động trong `flutter_test` nếu không thay `PathProviderPlatform.instance`. Tạo file:

```dart
import 'package:path_provider_platform_interface/path_provider_platform_interface.dart';

/// Trả về thư mục temp thật của máy chạy test (Directory.systemTemp) thay vì gọi platform
/// channel - path_provider không có sẵn platform channel trong flutter_test.
class FakePathProviderPlatform extends PathProviderPlatform {
  @override
  Future<String?> getTemporaryPath() async => Directory.systemTemp.path;
}
```

Thêm `import 'dart:io';` ở đầu file (cho `Directory.systemTemp`).

- [ ] **Step 2: Viết test trước cho `downloadAttachmentToTempFile`**

```dart
import 'dart:io';
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
```

- [ ] **Step 3: Chạy test, xác nhận FAIL**

Run: `cd mobile/taskmgmt_app && flutter test test/attachment_temp_file_test.dart`
Expected: FAIL — `downloadAttachmentToTempFile` chưa tồn tại.

- [ ] **Step 4: Tạo `downloadAttachmentToTempFile`**

```dart
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
```

- [ ] **Step 5: Chạy lại test, xác nhận PASS**

Run: `cd mobile/taskmgmt_app && flutter test test/attachment_temp_file_test.dart`
Expected: PASS (2/2).

- [ ] **Step 6: Refactor `AttachmentListSection._openAttachment` dùng hàm chung**

Trong `attachment_list_section.dart`, xoá 2 import đầu file không còn dùng ở đây nữa:

```dart
import 'dart:io';
```
```dart
import 'package:path_provider/path_provider.dart';
```

Thêm import mới:

```dart
import '../utils/attachment_temp_file.dart';
```

Thay toàn bộ method `_openAttachment` bằng:

```dart
  Future<void> _openAttachment(Attachment attachment) async {
    setState(() => _openingAttachmentId = attachment.id);
    try {
      final file = await downloadAttachmentToTempFile(
        download: () => ref.read(attachmentsProvider(widget.taskId).notifier).downloadAttachment(attachment.id),
        attachmentId: attachment.id,
        fileName: attachment.fileName,
      );

      final result = await OpenFilex.open(file.path);
      if (result.type != ResultType.done && mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Không thể mở tệp: ${result.message}')));
      }
    } catch (e) {
      if (!mounted) return;
      final message = e is ApiException ? e.message : 'Không thể mở tệp.';
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
    } finally {
      if (mounted) setState(() => _openingAttachmentId = null);
    }
  }
```

Hành vi giữ nguyên 100% so với trước (chỉ đổi chỗ đặt code) — chưa route sang viewer nào, việc đó ở Task 7.

- [ ] **Step 7: Phân tích tĩnh + chạy test cũ xác nhận không phá vỡ**

Run: `cd mobile/taskmgmt_app && flutter analyze`
Expected: không lỗi/warning mới.

Run: `flutter test test/work_task_detail_test.dart`
Expected: PASS (không đổi số lượng test so với trước — test này không exercise `_openAttachment` vì fake repository ném `UnimplementedError` cho `downloadAttachment`, nên refactor không ảnh hưởng).

- [ ] **Step 8: Commit**

```bash
cd d:/projects/K-app
git add mobile/taskmgmt_app/lib/features/attachments/presentation/utils/attachment_temp_file.dart mobile/taskmgmt_app/test/fake_path_provider_platform.dart mobile/taskmgmt_app/test/attachment_temp_file_test.dart mobile/taskmgmt_app/lib/features/attachments/presentation/widgets/attachment_list_section.dart
git commit -m "refactor(mobile): extract downloadAttachmentToTempFile helper with tests"
```

---

### Task 3: `ImageGalleryView`

**Files:**
- Create: `mobile/taskmgmt_app/lib/features/attachments/presentation/widgets/image_gallery_view.dart`
- Test: `mobile/taskmgmt_app/test/image_gallery_view_test.dart`

**Interfaces:**
- Consumes: `attachmentsProvider` / `attachmentRepositoryProvider` (đã có), `Attachment` entity (đã có).
- Produces: `ImageGalleryView({required String taskId, required List<Attachment> images, required int initialIndex})`. Task 6 tiêu thụ.

- [ ] **Step 1: Viết test trước**

```dart
import 'dart:convert';
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:photo_view/photo_view.dart';

import 'package:taskmgmt_app/features/attachments/domain/entities/attachment.dart';
import 'package:taskmgmt_app/features/attachments/domain/repositories/attachment_repository.dart';
import 'package:taskmgmt_app/features/attachments/presentation/providers/attachment_provider.dart';
import 'package:taskmgmt_app/features/attachments/presentation/widgets/image_gallery_view.dart';
import 'package:taskmgmt_app/shared/widgets/error_state_view.dart';

// PNG 1x1 hợp lệ tối thiểu - PhotoView/MemoryImage cần decode được, không thể dùng bytes rỗng.
final _onePixelPng = base64Decode(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=',
);

class _FakeAttachmentRepository implements AttachmentRepository {
  final Map<String, int> downloadCallCount = {};

  @override
  Future<List<Attachment>> getAttachments(String workTaskId) async => [];

  @override
  Future<Uint8List> downloadAttachment({required String workTaskId, required String attachmentId}) async {
    downloadCallCount[attachmentId] = (downloadCallCount[attachmentId] ?? 0) + 1;
    return _onePixelPng;
  }

  @override
  Future<void> deleteAttachment({required String workTaskId, required String attachmentId}) async {}

  @override
  Future<Attachment> uploadAttachment({required String workTaskId, required String fileName, required Uint8List bytes}) =>
      throw UnimplementedError();
}

class _FlakyAttachmentRepository implements AttachmentRepository {
  bool shouldFail = true;

  @override
  Future<List<Attachment>> getAttachments(String workTaskId) async => [];

  @override
  Future<Uint8List> downloadAttachment({required String workTaskId, required String attachmentId}) async {
    if (shouldFail) {
      shouldFail = false;
      throw Exception('network error');
    }
    return _onePixelPng;
  }

  @override
  Future<void> deleteAttachment({required String workTaskId, required String attachmentId}) async {}

  @override
  Future<Attachment> uploadAttachment({required String workTaskId, required String fileName, required Uint8List bytes}) =>
      throw UnimplementedError();
}

Attachment _image(String id) => Attachment(
      id: id,
      workTaskId: 'task-1',
      fileName: '$id.png',
      contentType: 'image/png',
      sizeBytes: _onePixelPng.length,
      uploadedByUserId: 'u1',
      uploadedByFullName: 'Người Dùng A',
      uploadedByEmail: 'a@example.com',
      createdAtUtc: DateTime.utc(2026, 8, 17),
    );

Widget _buildGallery(AttachmentRepository repo, List<Attachment> images, {int initialIndex = 0}) => ProviderScope(
      overrides: [attachmentRepositoryProvider.overrideWithValue(repo)],
      child: MaterialApp(
        home: ImageGalleryView(taskId: 'task-1', images: images, initialIndex: initialIndex),
      ),
    );

void main() {
  testWidgets('Loads only the initially visible image', (tester) async {
    final repo = _FakeAttachmentRepository();
    final images = [_image('img1'), _image('img2')];

    await tester.pumpWidget(_buildGallery(repo, images));
    await tester.pumpAndSettle();

    expect(repo.downloadCallCount['img1'], 1);
    expect(repo.downloadCallCount['img2'], null);
  });

  testWidgets('Swiping to next page loads it once, swiping back does not reload', (tester) async {
    final repo = _FakeAttachmentRepository();
    final images = [_image('img1'), _image('img2')];

    await tester.pumpWidget(_buildGallery(repo, images));
    await tester.pumpAndSettle();

    await tester.drag(find.byType(PageView), const Offset(-400, 0));
    await tester.pumpAndSettle();
    expect(repo.downloadCallCount['img2'], 1);

    await tester.drag(find.byType(PageView), const Offset(400, 0));
    await tester.pumpAndSettle();
    expect(repo.downloadCallCount['img1'], 1);
  });

  testWidgets('Download error shows retry, tapping retry loads successfully', (tester) async {
    final repo = _FlakyAttachmentRepository();
    final images = [_image('img1')];

    await tester.pumpWidget(_buildGallery(repo, images));
    await tester.pumpAndSettle();

    expect(find.byType(ErrorStateView), findsOneWidget);
    expect(find.text('Thử lại'), findsOneWidget);

    await tester.tap(find.text('Thử lại'));
    await tester.pumpAndSettle();

    expect(find.byType(ErrorStateView), findsNothing);
    expect(find.byType(PhotoView), findsOneWidget);
  });
}
```

- [ ] **Step 2: Chạy test, xác nhận FAIL**

Run: `cd mobile/taskmgmt_app && flutter test test/image_gallery_view_test.dart`
Expected: FAIL — `ImageGalleryView` chưa tồn tại.

- [ ] **Step 3: Tạo `ImageGalleryView`**

```dart
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
  final Map<String, Uint8List> _cache = {};
  final Map<String, Future<Uint8List>> _inFlight = {};

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
      controller: PageController(initialPage: widget.initialIndex),
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
            return PhotoView(imageProvider: MemoryImage(snapshot.data!));
          },
        );
      },
    );
  }
}
```

- [ ] **Step 4: Chạy lại test, xác nhận PASS**

Run: `cd mobile/taskmgmt_app && flutter test test/image_gallery_view_test.dart`
Expected: PASS (3/3).

- [ ] **Step 5: Commit**

```bash
cd d:/projects/K-app
git add mobile/taskmgmt_app/lib/features/attachments/presentation/widgets/image_gallery_view.dart mobile/taskmgmt_app/test/image_gallery_view_test.dart
git commit -m "feat(mobile): add ImageGalleryView with swipe + zoom + in-memory cache"
```

---

### Task 4: `PdfFileView`

**Files:**
- Create: `mobile/taskmgmt_app/lib/features/attachments/presentation/widgets/pdf_file_view.dart`
- Test: `mobile/taskmgmt_app/test/pdf_file_view_test.dart`

**Interfaces:**
- Consumes: `downloadAttachmentToTempFile` (Task 2), `attachmentsProvider` (đã có).
- Produces: `PdfFileView({required String taskId, required Attachment attachment})`. Task 6 tiêu thụ.

Test chỉ kiểm tra trạng thái loading/lỗi/thử-lại qua fake repository luôn ném lỗi — KHÔNG kiểm tra `PDFView` render thành công, vì `flutter_pdfview` dùng platform view thật, không đáng tin cậy trong `flutter_test` (đã ghi trong spec §7 - "không cần test riêng thư viện ngoài").

- [ ] **Step 1: Viết test trước**

```dart
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:taskmgmt_app/features/attachments/domain/entities/attachment.dart';
import 'package:taskmgmt_app/features/attachments/domain/repositories/attachment_repository.dart';
import 'package:taskmgmt_app/features/attachments/presentation/providers/attachment_provider.dart';
import 'package:taskmgmt_app/features/attachments/presentation/widgets/pdf_file_view.dart';
import 'package:taskmgmt_app/shared/widgets/error_state_view.dart';

class _AlwaysFailingAttachmentRepository implements AttachmentRepository {
  int downloadCallCount = 0;

  @override
  Future<List<Attachment>> getAttachments(String workTaskId) async => [];

  @override
  Future<Uint8List> downloadAttachment({required String workTaskId, required String attachmentId}) async {
    downloadCallCount++;
    throw Exception('network error');
  }

  @override
  Future<void> deleteAttachment({required String workTaskId, required String attachmentId}) async {}

  @override
  Future<Attachment> uploadAttachment({required String workTaskId, required String fileName, required Uint8List bytes}) =>
      throw UnimplementedError();
}

final _pdfAttachment = Attachment(
  id: 'pdf1',
  workTaskId: 'task-1',
  fileName: 'tai-lieu.pdf',
  contentType: 'application/pdf',
  sizeBytes: 1024,
  uploadedByUserId: 'u1',
  uploadedByFullName: 'Người Dùng A',
  uploadedByEmail: 'a@example.com',
  createdAtUtc: DateTime.utc(2026, 8, 17),
);

void main() {
  testWidgets('Shows loading indicator first', (tester) async {
    final repo = _AlwaysFailingAttachmentRepository();

    await tester.pumpWidget(
      ProviderScope(
        overrides: [attachmentRepositoryProvider.overrideWithValue(repo)],
        child: MaterialApp(home: PdfFileView(taskId: 'task-1', attachment: _pdfAttachment)),
      ),
    );

    expect(find.byType(CircularProgressIndicator), findsOneWidget);
  });

  testWidgets('Shows error with retry when download fails, retry calls download again', (tester) async {
    final repo = _AlwaysFailingAttachmentRepository();

    await tester.pumpWidget(
      ProviderScope(
        overrides: [attachmentRepositoryProvider.overrideWithValue(repo)],
        child: MaterialApp(home: PdfFileView(taskId: 'task-1', attachment: _pdfAttachment)),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byType(ErrorStateView), findsOneWidget);
    expect(repo.downloadCallCount, 1);

    await tester.tap(find.text('Thử lại'));
    await tester.pumpAndSettle();

    expect(repo.downloadCallCount, 2);
  });
}
```

- [ ] **Step 2: Chạy test, xác nhận FAIL**

Run: `cd mobile/taskmgmt_app && flutter test test/pdf_file_view_test.dart`
Expected: FAIL — `PdfFileView` chưa tồn tại.

- [ ] **Step 3: Tạo `PdfFileView`**

```dart
import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter_pdfview/flutter_pdfview.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../shared/widgets/error_state_view.dart';
import '../../domain/entities/attachment.dart';
import '../providers/attachment_provider.dart';
import '../utils/attachment_temp_file.dart';

class PdfFileView extends ConsumerStatefulWidget {
  const PdfFileView({super.key, required this.taskId, required this.attachment});

  final String taskId;
  final Attachment attachment;

  @override
  ConsumerState<PdfFileView> createState() => _PdfFileViewState();
}

class _PdfFileViewState extends ConsumerState<PdfFileView> {
  late Future<File> _fileFuture;

  @override
  void initState() {
    super.initState();
    _fileFuture = _download();
  }

  Future<File> _download() => downloadAttachmentToTempFile(
        download: () => ref.read(attachmentsProvider(widget.taskId).notifier).downloadAttachment(widget.attachment.id),
        attachmentId: widget.attachment.id,
        fileName: widget.attachment.fileName,
      );

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<File>(
      future: _fileFuture,
      builder: (context, snapshot) {
        if (snapshot.connectionState != ConnectionState.done) {
          return const Center(child: CircularProgressIndicator());
        }
        if (snapshot.hasError) {
          final error = snapshot.error;
          return ErrorStateView(
            message: error is ApiException ? error.message : 'Không tải được tệp PDF.',
            onRetry: () => setState(() => _fileFuture = _download()),
          );
        }
        return PDFView(
          filePath: snapshot.data!.path,
          onError: (error) => debugPrint('PDFView error: $error'),
          onPageError: (page, error) => debugPrint('PDFView page $page error: $error'),
        );
      },
    );
  }
}
```

- [ ] **Step 4: Chạy lại test, xác nhận PASS**

Run: `cd mobile/taskmgmt_app && flutter test test/pdf_file_view_test.dart`
Expected: PASS (2/2).

- [ ] **Step 5: Commit**

```bash
cd d:/projects/K-app
git add mobile/taskmgmt_app/lib/features/attachments/presentation/widgets/pdf_file_view.dart mobile/taskmgmt_app/test/pdf_file_view_test.dart
git commit -m "feat(mobile): add PdfFileView"
```

---

### Task 5: `VideoFileView`

**Files:**
- Create: `mobile/taskmgmt_app/lib/features/attachments/presentation/widgets/video_file_view.dart`
- Test: `mobile/taskmgmt_app/test/video_file_view_test.dart`

**Interfaces:**
- Consumes: `downloadAttachmentToTempFile` (Task 2), `attachmentsProvider` (đã có).
- Produces: `VideoFileView({required String taskId, required Attachment attachment})`. Task 6 tiêu thụ.

Cùng lý do như Task 4: test chỉ kiểm tra loading/lỗi/thử-lại, không kiểm tra `Chewie`/`VideoPlayerController` khởi tạo thành công (cần platform channel thật của `video_player`).

- [ ] **Step 1: Viết test trước**

```dart
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:taskmgmt_app/features/attachments/domain/entities/attachment.dart';
import 'package:taskmgmt_app/features/attachments/domain/repositories/attachment_repository.dart';
import 'package:taskmgmt_app/features/attachments/presentation/providers/attachment_provider.dart';
import 'package:taskmgmt_app/features/attachments/presentation/widgets/video_file_view.dart';
import 'package:taskmgmt_app/shared/widgets/error_state_view.dart';

class _AlwaysFailingAttachmentRepository implements AttachmentRepository {
  int downloadCallCount = 0;

  @override
  Future<List<Attachment>> getAttachments(String workTaskId) async => [];

  @override
  Future<Uint8List> downloadAttachment({required String workTaskId, required String attachmentId}) async {
    downloadCallCount++;
    throw Exception('network error');
  }

  @override
  Future<void> deleteAttachment({required String workTaskId, required String attachmentId}) async {}

  @override
  Future<Attachment> uploadAttachment({required String workTaskId, required String fileName, required Uint8List bytes}) =>
      throw UnimplementedError();
}

final _videoAttachment = Attachment(
  id: 'vid1',
  workTaskId: 'task-1',
  fileName: 'clip.mp4',
  contentType: 'video/mp4',
  sizeBytes: 2048,
  uploadedByUserId: 'u1',
  uploadedByFullName: 'Người Dùng A',
  uploadedByEmail: 'a@example.com',
  createdAtUtc: DateTime.utc(2026, 8, 17),
);

void main() {
  testWidgets('Shows loading indicator first', (tester) async {
    final repo = _AlwaysFailingAttachmentRepository();

    await tester.pumpWidget(
      ProviderScope(
        overrides: [attachmentRepositoryProvider.overrideWithValue(repo)],
        child: MaterialApp(home: VideoFileView(taskId: 'task-1', attachment: _videoAttachment)),
      ),
    );

    expect(find.byType(CircularProgressIndicator), findsOneWidget);
  });

  testWidgets('Shows error with retry when download fails, retry calls download again', (tester) async {
    final repo = _AlwaysFailingAttachmentRepository();

    await tester.pumpWidget(
      ProviderScope(
        overrides: [attachmentRepositoryProvider.overrideWithValue(repo)],
        child: MaterialApp(home: VideoFileView(taskId: 'task-1', attachment: _videoAttachment)),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.byType(ErrorStateView), findsOneWidget);
    expect(repo.downloadCallCount, 1);

    await tester.tap(find.text('Thử lại'));
    await tester.pumpAndSettle();

    expect(repo.downloadCallCount, 2);
  });
}
```

- [ ] **Step 2: Chạy test, xác nhận FAIL**

Run: `cd mobile/taskmgmt_app && flutter test test/video_file_view_test.dart`
Expected: FAIL — `VideoFileView` chưa tồn tại.

- [ ] **Step 3: Tạo `VideoFileView`**

```dart
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
    });
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
            onRetry: () => setState(() => _controllerFuture = _load()),
          );
        }
        return Chewie(controller: snapshot.data!);
      },
    );
  }
}
```

- [ ] **Step 4: Chạy lại test, xác nhận PASS**

Run: `cd mobile/taskmgmt_app && flutter test test/video_file_view_test.dart`
Expected: PASS (2/2).

- [ ] **Step 5: Commit**

```bash
cd d:/projects/K-app
git add mobile/taskmgmt_app/lib/features/attachments/presentation/widgets/video_file_view.dart mobile/taskmgmt_app/test/video_file_view_test.dart
git commit -m "feat(mobile): add VideoFileView"
```

---

### Task 6: `AttachmentViewerScreen`

**Files:**
- Create: `mobile/taskmgmt_app/lib/features/attachments/presentation/screens/attachment_viewer_screen.dart`
- Test: `mobile/taskmgmt_app/test/attachment_viewer_screen_test.dart`

**Interfaces:**
- Consumes: `attachmentsProvider` (đã có), `ImageGalleryView` (Task 3), `PdfFileView` (Task 4), `VideoFileView` (Task 5).
- Produces: `AttachmentViewerScreen({required String taskId, required String attachmentId})`, `static const path = '/tasks/:taskId/attachments/:attachmentId'`, `static const name = 'attachment-viewer'`. Task 7 tiêu thụ.

Test chỉ kiểm tra ĐÚNG WIDGET CON được chọn theo `contentType` (không gọi `pumpAndSettle`, chỉ `pump()` 1 lần đủ để `attachmentsProvider` trả list và widget build nhánh) — không cần quan tâm widget con tải file thành công hay không, vì đó không phải việc của màn hình này (đã test riêng ở Task 3/4/5).

- [ ] **Step 1: Viết test trước**

```dart
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:taskmgmt_app/features/attachments/domain/entities/attachment.dart';
import 'package:taskmgmt_app/features/attachments/domain/repositories/attachment_repository.dart';
import 'package:taskmgmt_app/features/attachments/presentation/providers/attachment_provider.dart';
import 'package:taskmgmt_app/features/attachments/presentation/screens/attachment_viewer_screen.dart';
import 'package:taskmgmt_app/features/attachments/presentation/widgets/image_gallery_view.dart';
import 'package:taskmgmt_app/features/attachments/presentation/widgets/pdf_file_view.dart';
import 'package:taskmgmt_app/features/attachments/presentation/widgets/video_file_view.dart';
import 'package:taskmgmt_app/shared/widgets/error_state_view.dart';

List<Attachment> _attachments() => [
      Attachment(
        id: 'img1',
        workTaskId: 'task-1',
        fileName: 'anh.png',
        contentType: 'image/png',
        sizeBytes: 100,
        uploadedByUserId: 'u1',
        uploadedByFullName: 'Người Dùng A',
        uploadedByEmail: 'a@example.com',
        createdAtUtc: DateTime.utc(2026, 8, 17),
      ),
      Attachment(
        id: 'pdf1',
        workTaskId: 'task-1',
        fileName: 'tai-lieu.pdf',
        contentType: 'application/pdf',
        sizeBytes: 200,
        uploadedByUserId: 'u1',
        uploadedByFullName: 'Người Dùng A',
        uploadedByEmail: 'a@example.com',
        createdAtUtc: DateTime.utc(2026, 8, 17),
      ),
      Attachment(
        id: 'vid1',
        workTaskId: 'task-1',
        fileName: 'clip.mp4',
        contentType: 'video/mp4',
        sizeBytes: 300,
        uploadedByUserId: 'u1',
        uploadedByFullName: 'Người Dùng A',
        uploadedByEmail: 'a@example.com',
        createdAtUtc: DateTime.utc(2026, 8, 17),
      ),
      Attachment(
        id: 'doc1',
        workTaskId: 'task-1',
        fileName: 'bao-cao.docx',
        contentType: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
        sizeBytes: 400,
        uploadedByUserId: 'u1',
        uploadedByFullName: 'Người Dùng A',
        uploadedByEmail: 'a@example.com',
        createdAtUtc: DateTime.utc(2026, 8, 17),
      ),
    ];

class _FakeAttachmentRepository implements AttachmentRepository {
  @override
  Future<List<Attachment>> getAttachments(String workTaskId) async => _attachments();

  @override
  Future<Uint8List> downloadAttachment({required String workTaskId, required String attachmentId}) =>
      throw UnimplementedError();

  @override
  Future<void> deleteAttachment({required String workTaskId, required String attachmentId}) async {}

  @override
  Future<Attachment> uploadAttachment({required String workTaskId, required String fileName, required Uint8List bytes}) =>
      throw UnimplementedError();
}

Widget _buildViewer(String attachmentId) => ProviderScope(
      overrides: [attachmentRepositoryProvider.overrideWithValue(_FakeAttachmentRepository())],
      child: MaterialApp(
        home: AttachmentViewerScreen(taskId: 'task-1', attachmentId: attachmentId),
      ),
    );

void main() {
  testWidgets('Routes image attachment to ImageGalleryView', (tester) async {
    await tester.pumpWidget(_buildViewer('img1'));
    await tester.pump();

    expect(find.byType(ImageGalleryView), findsOneWidget);
  });

  testWidgets('Routes PDF attachment to PdfFileView', (tester) async {
    await tester.pumpWidget(_buildViewer('pdf1'));
    await tester.pump();

    expect(find.byType(PdfFileView), findsOneWidget);
  });

  testWidgets('Routes video attachment to VideoFileView', (tester) async {
    await tester.pumpWidget(_buildViewer('vid1'));
    await tester.pump();

    expect(find.byType(VideoFileView), findsOneWidget);
  });

  testWidgets('Unsupported content type shows an explanatory error', (tester) async {
    await tester.pumpWidget(_buildViewer('doc1'));
    await tester.pump();

    expect(find.byType(ErrorStateView), findsOneWidget);
    expect(find.text('Loại tệp này không hỗ trợ xem trước.'), findsOneWidget);
  });
}
```

- [ ] **Step 2: Chạy test, xác nhận FAIL**

Run: `cd mobile/taskmgmt_app && flutter test test/attachment_viewer_screen_test.dart`
Expected: FAIL — `AttachmentViewerScreen` chưa tồn tại.

- [ ] **Step 3: Tạo `AttachmentViewerScreen`**

```dart
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../shared/widgets/error_state_view.dart';
import '../../domain/entities/attachment.dart';
import '../providers/attachment_provider.dart';
import '../widgets/image_gallery_view.dart';
import '../widgets/pdf_file_view.dart';
import '../widgets/video_file_view.dart';

class AttachmentViewerScreen extends ConsumerWidget {
  const AttachmentViewerScreen({super.key, required this.taskId, required this.attachmentId});

  final String taskId;
  final String attachmentId;

  static const path = '/tasks/:taskId/attachments/:attachmentId';
  static const name = 'attachment-viewer';

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final attachmentsAsync = ref.watch(attachmentsProvider(taskId));

    return Scaffold(
      appBar: AppBar(),
      body: attachmentsAsync.when(
        data: (attachments) {
          final attachment = attachments.firstWhere((a) => a.id == attachmentId);
          return _buildContent(attachment);
        },
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => ErrorStateView(
          message: error is ApiException ? error.message : 'Không tải được tệp đính kèm.',
          onRetry: () => ref.invalidate(attachmentsProvider(taskId)),
        ),
      ),
    );
  }

  Widget _buildContent(Attachment attachment) {
    if (attachment.isImage) {
      return _ImageGalleryFor(taskId: taskId, attachmentId: attachment.id);
    }
    if (attachment.contentType == 'application/pdf') {
      return PdfFileView(taskId: taskId, attachment: attachment);
    }
    if (attachment.contentType.startsWith('video/')) {
      return VideoFileView(taskId: taskId, attachment: attachment);
    }
    return ErrorStateView(message: 'Loại tệp này không hỗ trợ xem trước.', onRetry: () {});
  }
}

/// Lọc riêng đúng danh sách ảnh trong task (không lẫn PDF/video) và tìm vị trí ảnh đang bấm
/// trong danh sách đó - tách hàm riêng để không lặp lại logic firstWhere/where 2 lần khi build.
class _ImageGalleryFor extends ConsumerWidget {
  const _ImageGalleryFor({required this.taskId, required this.attachmentId});

  final String taskId;
  final String attachmentId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final attachments = ref.watch(attachmentsProvider(taskId)).value!;
    final images = attachments.where((a) => a.isImage).toList();
    final initialIndex = images.indexWhere((a) => a.id == attachmentId);

    return ImageGalleryView(taskId: taskId, images: images, initialIndex: initialIndex);
  }
}
```

- [ ] **Step 4: Chạy lại test, xác nhận PASS**

Run: `cd mobile/taskmgmt_app && flutter test test/attachment_viewer_screen_test.dart`
Expected: PASS (4/4).

- [ ] **Step 5: Commit**

```bash
cd d:/projects/K-app
git add mobile/taskmgmt_app/lib/features/attachments/presentation/screens/attachment_viewer_screen.dart mobile/taskmgmt_app/test/attachment_viewer_screen_test.dart
git commit -m "feat(mobile): add AttachmentViewerScreen routing by content type"
```

---

### Task 7: Wire route + cập nhật `AttachmentListSection`

**Files:**
- Modify: `mobile/taskmgmt_app/lib/core/routing/app_router.dart`
- Modify: `mobile/taskmgmt_app/lib/features/attachments/presentation/widgets/attachment_list_section.dart`

**Interfaces:**
- Consumes: `AttachmentViewerScreen` (Task 6).

- [ ] **Step 1: Đăng ký route**

Trong `app_router.dart`, thêm import:

```dart
import '../../features/attachments/presentation/screens/attachment_viewer_screen.dart';
```

Thêm route vào mảng `routes`, ngay sau route `WorkTaskDetailScreen` (`/tasks/:id`):

```dart
      GoRoute(
        path: '/tasks/:taskId/attachments/:attachmentId',
        name: AttachmentViewerScreen.name,
        builder: (context, state) => AttachmentViewerScreen(
          taskId: state.pathParameters['taskId']!,
          attachmentId: state.pathParameters['attachmentId']!,
        ),
      ),
```

- [ ] **Step 2: Sửa `AttachmentListSection._openAttachment` — route sang viewer cho ảnh/PDF/video**

Thêm import ở đầu `attachment_list_section.dart`:

```dart
import 'package:go_router/go_router.dart';
```

Sửa method `_openAttachment` (đã refactor ở Task 2) — thêm điều kiện rẽ nhánh NGAY ĐẦU method, trước đoạn tải-file-tạm+OpenFilex hiện có:

```dart
  Future<void> _openAttachment(Attachment attachment) async {
    if (attachment.isImage ||
        attachment.contentType == 'application/pdf' ||
        attachment.contentType.startsWith('video/')) {
      context.push('/tasks/${widget.taskId}/attachments/${attachment.id}');
      return;
    }

    setState(() => _openingAttachmentId = attachment.id);
    try {
      final file = await downloadAttachmentToTempFile(
        download: () => ref.read(attachmentsProvider(widget.taskId).notifier).downloadAttachment(attachment.id),
        attachmentId: attachment.id,
        fileName: attachment.fileName,
      );

      final result = await OpenFilex.open(file.path);
      if (result.type != ResultType.done && mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Không thể mở tệp: ${result.message}')));
      }
    } catch (e) {
      if (!mounted) return;
      final message = e is ApiException ? e.message : 'Không thể mở tệp.';
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
    } finally {
      if (mounted) setState(() => _openingAttachmentId = null);
    }
  }
```

- [ ] **Step 3: Phân tích tĩnh + chạy toàn bộ test mobile**

Run: `cd mobile/taskmgmt_app && flutter analyze`
Expected: không lỗi/warning mới.

Run: `flutter test`
Expected: PASS toàn bộ (21 test cũ + 2 Task 2 + 3 Task 3 + 2 Task 4 + 2 Task 5 + 4 Task 6 = 34, không phá vỡ test cũ).

- [ ] **Step 4: Commit**

```bash
cd d:/projects/K-app
git add mobile/taskmgmt_app/lib/core/routing/app_router.dart mobile/taskmgmt_app/lib/features/attachments/presentation/widgets/attachment_list_section.dart
git commit -m "feat(mobile): open image/PDF/video attachments in the in-app viewer"
```

---

## Self-Review Notes

- **Spec coverage:** Task 1 → dependencies (spec §6); Task 2 → hàm dùng chung tải-file-tạm (không nêu rõ trong spec ban đầu nhưng cần thiết để DRY giữa `_openAttachment` cũ và `PdfFileView`/`VideoFileView` mới — phát hiện khi thiết kế chi tiết, đúng tinh thần "tái dùng đúng đoạn tải-về-file-tạm hiện có" ở spec §4); Task 3 → `ImageGalleryView` (spec §4, §7); Task 4/5 → `PdfFileView`/`VideoFileView` (spec §4, §7); Task 6 → `AttachmentViewerScreen` rẽ nhánh + case "loại không hỗ trợ" (spec §4); Task 7 → routing + sửa `AttachmentListSection` (spec §4, §5). Toàn bộ mục "ngoài phạm vi" ở spec §2 không có task nào đụng tới.
- **Placeholder scan:** không còn "TBD"/"implement later" — mọi step đều có code đầy đủ, kể cả 5 file test.
- **Type consistency:** đã đối chiếu `Attachment` entity thật (10 field, kể cả `isImage` getter), `AttachmentRepository` interface thật (4 method), `attachmentsProvider`/`attachmentRepositoryProvider` thật, `ErrorStateView(message, onRetry)` thật trước khi viết plan. `downloadAttachmentToTempFile(download, attachmentId, fileName)` định nghĩa ở Task 2 được Task 4/5/7 gọi đúng 3 tham số này xuyên suốt. `AttachmentViewerScreen.path`/`.name` (Task 6) khớp đúng route Task 7 đăng ký.
- **Quyết định kỹ thuật phát sinh khi viết plan (không có trong spec):** `downloadAttachmentToTempFile` thiết kế nhận `download` dạng closure thay vì `WidgetRef` trực tiếp — giúp hàm test được bằng `test()` thuần (không cần widget tree), đồng thời cần fake `PathProviderPlatform` (Task 2) vì `getTemporaryDirectory()` gọi platform channel thật không có sẵn trong `flutter_test`. Test Task 4/5 cố tình chỉ dùng fake repository luôn lỗi để không bao giờ chạm tới `flutter_pdfview`/`video_player`'s platform view thật trong lúc test — khớp đúng giới hạn đã nêu ở spec §7.
