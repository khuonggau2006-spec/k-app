# Upload UX cho tệp đính kèm Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the single-shot "pick one file, opaque spinner" upload flow in `AttachmentListSection` with a bottom sheet to choose a source (Camera / Thư viện ảnh / Tệp), multi-file selection for "Tệp", and a real per-file progress queue that tolerates partial failure.

**Architecture:** A pure, closure-based `runUploadQueue()` function (no widget/plugin dependencies) drives sequential upload with progress and error-tolerance; `AttachmentListSection` only owns `List<UploadQueueItem> _queue` as display state and renders it via a new `UploadQueueList` widget. `onSendProgress` is threaded from Dio up through `AttachmentRemoteDataSource` → `AttachmentRepository` → `AttachmentsController` as an optional named parameter. Picker/source selection (`image_picker`, `file_picker`) stays thin and untested, matching the existing precedent for `flutter_pdfview`/`video_player` (platform-channel plugins can't be exercised in `flutter_test`).

**Tech Stack:** Flutter/Riverpod, `image_picker: ^1.2.3` (new), existing `file_picker: ^8.1.2`, `dio: ^5.11.0` (`onSendProgress`).

## Global Constraints

- Multi-select applies only to "Tệp" (file picker); Camera/Thư viện ảnh each return at most 1 file.
- 1 file's upload failure must not stop the queue; remaining files continue uploading in order.
- No cancel/pause, no per-file retry UI, no image compression — explicitly out of scope.
- All new user-facing strings are in Vietnamese, matching existing strings in `attachment_list_section.dart`.
- `--concurrency=1` when running `flutter test` in this environment for reliable output.

---

### Task 1: `image_picker` dependency + platform permissions

**Files:**
- Modify: `mobile/taskmgmt_app/pubspec.yaml:56` (after `chewie: ^1.13.1`)
- Modify: `mobile/taskmgmt_app/android/app/src/main/AndroidManifest.xml`
- Modify: `mobile/taskmgmt_app/ios/Runner/Info.plist`

**Interfaces:**
- Produces: `image_picker` package available for import in later tasks (`ImagePicker`, `ImageSource`, `XFile`).

This is a config-only task (no test cycle) — `image_picker` is a platform-channel plugin like the ones already in this project (`file_picker`, `video_player`), so its behavior isn't unit/widget-testable here; only `flutter analyze` + a full test run (regression check) apply.

- [ ] **Step 1: Add the dependency**

In `mobile/taskmgmt_app/pubspec.yaml`, add this line right after `chewie: ^1.13.1` (line 56), keeping the same indentation:

```yaml
  image_picker: ^1.2.3
```

- [ ] **Step 2: Fetch packages**

Run: `cd mobile/taskmgmt_app && flutter pub get`
Expected: resolves successfully, no version conflicts (verify by checking the command exits 0).

- [ ] **Step 3: Declare Android camera permission**

`image_picker`'s camera source needs `android.permission.CAMERA` declared. In `mobile/taskmgmt_app/android/app/src/main/AndroidManifest.xml`, add this line as the first child of `<manifest ...>`, immediately before the `<application ...>` tag (line 2):

```xml
    <uses-permission android:name="android.permission.CAMERA" />
```

- [ ] **Step 4: Declare iOS camera/photo-library usage descriptions**

In `mobile/taskmgmt_app/ios/Runner/Info.plist`, add these two key/value pairs inside the top-level `<dict>`, right after the opening `<dict>` tag (line 4):

```xml
	<key>NSCameraUsageDescription</key>
	<string>Ứng dụng cần quyền truy cập máy ảnh để chụp ảnh đính kèm vào công việc.</string>
	<key>NSPhotoLibraryUsageDescription</key>
	<string>Ứng dụng cần quyền truy cập thư viện ảnh để đính kèm ảnh vào công việc.</string>
```

- [ ] **Step 5: Verify nothing broke**

Run: `cd mobile/taskmgmt_app && flutter analyze`
Expected: No errors (pre-existing warnings, if any, are unrelated and unchanged).

- [ ] **Step 6: Commit**

```bash
git add mobile/taskmgmt_app/pubspec.yaml mobile/taskmgmt_app/pubspec.lock mobile/taskmgmt_app/android/app/src/main/AndroidManifest.xml mobile/taskmgmt_app/ios/Runner/Info.plist
git commit -m "feat(mobile): add image_picker dependency and camera/photo permissions"
```

---

### Task 2: `runUploadQueue` — pure upload-queue engine

**Files:**
- Create: `mobile/taskmgmt_app/lib/features/attachments/presentation/utils/upload_queue.dart`
- Test: `mobile/taskmgmt_app/test/upload_queue_test.dart`

**Interfaces:**
- Produces:
  - `class PickedUpload { const PickedUpload({required String fileName, required Uint8List bytes}); }`
  - `enum UploadItemStatus { uploading, done, error }`
  - `class UploadQueueItem { UploadQueueItem({required String fileName, UploadItemStatus status = UploadItemStatus.uploading, double progress = 0, String? errorMessage}); final String fileName; UploadItemStatus status; double progress; String? errorMessage; }`
  - `Future<List<UploadQueueItem>> runUploadQueue({required List<PickedUpload> files, required Future<void> Function(PickedUpload file, void Function(int sent, int total) onSendProgress) upload, required void Function(List<UploadQueueItem> queue) onUpdate})`
  - `String? uploadFailureSnackBarMessage(List<UploadQueueItem> queue)` — null if no item has `status == UploadItemStatus.error`, otherwise `'Tải lên thất bại: <fileName1>, <fileName2>, ...'` for every failed item in queue order.
- Consumes: nothing from other tasks (leaf module).

`onUpdate` receives a fresh, independent snapshot list on every call (cloned `UploadQueueItem`s, not references into the internal mutable queue) — callers that store a call's snapshot must see it stay frozen even as later files in the same run change status.

- [ ] **Step 1: Write the failing tests**

Create `mobile/taskmgmt_app/test/upload_queue_test.dart`:

```dart
import 'dart:typed_data';

import 'package:flutter_test/flutter_test.dart';

import 'package:taskmgmt_app/features/attachments/presentation/utils/upload_queue.dart';

void main() {
  test('Uploads files sequentially in order, marking each done', () async {
    final uploadedOrder = <String>[];
    final files = [
      const PickedUpload(fileName: 'a.png', bytes: Uint8List(0)),
      const PickedUpload(fileName: 'b.png', bytes: Uint8List(0)),
      const PickedUpload(fileName: 'c.png', bytes: Uint8List(0)),
    ];

    final result = await runUploadQueue(
      files: files,
      upload: (file, onSendProgress) async {
        uploadedOrder.add(file.fileName);
        onSendProgress(1, 1);
      },
      onUpdate: (_) {},
    );

    expect(uploadedOrder, ['a.png', 'b.png', 'c.png']);
    expect(result.map((item) => item.status), everyElement(UploadItemStatus.done));
    expect(result.map((item) => item.progress), everyElement(1.0));
  });

  test('Reports progress percentage as the upload closure reports bytes sent', () async {
    final progressSnapshots = <double>[];

    await runUploadQueue(
      files: [const PickedUpload(fileName: 'big.zip', bytes: Uint8List(0))],
      upload: (file, onSendProgress) async {
        onSendProgress(0, 100);
        onSendProgress(50, 100);
        onSendProgress(100, 100);
      },
      onUpdate: (queue) => progressSnapshots.add(queue.single.progress),
    );

    expect(progressSnapshots, [0.0, 0.5, 1.0, 1.0]);
  });

  test('A failing file does not stop the queue and is marked with its error', () async {
    final files = [
      const PickedUpload(fileName: 'a.png', bytes: Uint8List(0)),
      const PickedUpload(fileName: 'bad.png', bytes: Uint8List(0)),
      const PickedUpload(fileName: 'c.png', bytes: Uint8List(0)),
    ];

    final result = await runUploadQueue(
      files: files,
      upload: (file, onSendProgress) async {
        if (file.fileName == 'bad.png') {
          throw Exception('network error');
        }
        onSendProgress(1, 1);
      },
      onUpdate: (_) {},
    );

    expect(result[0].status, UploadItemStatus.done);
    expect(result[1].status, UploadItemStatus.error);
    expect(result[1].errorMessage, contains('network error'));
    expect(result[2].status, UploadItemStatus.done);
  });

  test('onUpdate snapshots stay frozen after later updates mutate the queue', () async {
    final snapshots = <List<UploadQueueItem>>[];

    await runUploadQueue(
      files: [const PickedUpload(fileName: 'a.png', bytes: Uint8List(0))],
      upload: (file, onSendProgress) async => onSendProgress(50, 100),
      onUpdate: snapshots.add,
    );

    // First call: mid-upload progress (50%, still "uploading"). Second call: after the file
    // finishes (status flips to "done", progress to 1). The first snapshot must not have been
    // mutated in place when the second call happened.
    expect(snapshots, hasLength(2));
    expect(snapshots[0].single.progress, 0.5);
    expect(snapshots[0].single.status, UploadItemStatus.uploading);
    expect(snapshots[1].single.status, UploadItemStatus.done);
    expect(snapshots[0].single.progress, 0.5);
    expect(snapshots[0].single.status, UploadItemStatus.uploading);
  });

  test('uploadFailureSnackBarMessage returns null when nothing failed', () {
    final queue = [UploadQueueItem(fileName: 'a.png', status: UploadItemStatus.done, progress: 1)];
    expect(uploadFailureSnackBarMessage(queue), isNull);
  });

  test('uploadFailureSnackBarMessage lists the names of every failed file in order', () {
    final queue = [
      UploadQueueItem(fileName: 'a.png', status: UploadItemStatus.done, progress: 1),
      UploadQueueItem(fileName: 'b.png', status: UploadItemStatus.error, errorMessage: 'x'),
      UploadQueueItem(fileName: 'c.png', status: UploadItemStatus.error, errorMessage: 'y'),
    ];
    expect(uploadFailureSnackBarMessage(queue), 'Tải lên thất bại: b.png, c.png');
  });
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd mobile/taskmgmt_app && flutter test test/upload_queue_test.dart --concurrency=1`
Expected: FAIL — `upload_queue.dart` does not exist yet (import error).

- [ ] **Step 3: Implement `upload_queue.dart`**

Create `mobile/taskmgmt_app/lib/features/attachments/presentation/utils/upload_queue.dart`:

```dart
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
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd mobile/taskmgmt_app && flutter test test/upload_queue_test.dart --concurrency=1`
Expected: PASS (6 tests).

- [ ] **Step 5: Commit**

```bash
git add mobile/taskmgmt_app/lib/features/attachments/presentation/utils/upload_queue.dart mobile/taskmgmt_app/test/upload_queue_test.dart
git commit -m "feat(mobile): add runUploadQueue pure upload-queue engine"
```

---

### Task 3: Thread `onSendProgress` through the attachment data layer

**Files:**
- Modify: `mobile/taskmgmt_app/lib/features/attachments/domain/repositories/attachment_repository.dart`
- Modify: `mobile/taskmgmt_app/lib/features/attachments/data/repositories/attachment_repository_impl.dart`
- Modify: `mobile/taskmgmt_app/lib/features/attachments/data/datasources/attachment_remote_data_source.dart`
- Modify: `mobile/taskmgmt_app/lib/features/attachments/presentation/providers/attachment_provider.dart:25-28`
- Modify: `mobile/taskmgmt_app/test/work_task_detail_test.dart:147-149`
- Modify: `mobile/taskmgmt_app/test/attachment_viewer_screen_test.dart:74-76`

**Interfaces:**
- Consumes: nothing new from Task 2.
- Produces: `AttachmentsController.uploadAttachment({required String fileName, required Uint8List bytes, void Function(int sent, int total)? onSendProgress})` — Task 5's `_upload()` calls this.

Dart requires an overriding method to redeclare every named parameter the interface declares (including optional ones) — the two existing `_FakeAttachmentRepository` classes in the test suite must add the new parameter or the project won't compile. This is plumbing with no new business logic, so there's no new test — verify via `flutter analyze` plus a full regression test run, matching how this codebase already treats the Dio datasource layer (no dedicated datasource tests exist).

- [ ] **Step 1: Add `onSendProgress` to the repository interface**

In `mobile/taskmgmt_app/lib/features/attachments/domain/repositories/attachment_repository.dart`, replace:

```dart
  Future<Attachment> uploadAttachment({
    required String workTaskId,
    required String fileName,
    required Uint8List bytes,
  });
```

with:

```dart
  Future<Attachment> uploadAttachment({
    required String workTaskId,
    required String fileName,
    required Uint8List bytes,
    void Function(int sent, int total)? onSendProgress,
  });
```

- [ ] **Step 2: Thread it through `AttachmentRepositoryImpl`**

In `mobile/taskmgmt_app/lib/features/attachments/data/repositories/attachment_repository_impl.dart`, replace:

```dart
  @override
  Future<Attachment> uploadAttachment({
    required String workTaskId,
    required String fileName,
    required Uint8List bytes,
  }) async {
    final model = await _remoteDataSource.uploadAttachment(workTaskId: workTaskId, fileName: fileName, bytes: bytes);
    return model.toDomain();
  }
```

with:

```dart
  @override
  Future<Attachment> uploadAttachment({
    required String workTaskId,
    required String fileName,
    required Uint8List bytes,
    void Function(int sent, int total)? onSendProgress,
  }) async {
    final model = await _remoteDataSource.uploadAttachment(
      workTaskId: workTaskId,
      fileName: fileName,
      bytes: bytes,
      onSendProgress: onSendProgress,
    );
    return model.toDomain();
  }
```

- [ ] **Step 3: Thread it through `AttachmentRemoteDataSource` into Dio**

In `mobile/taskmgmt_app/lib/features/attachments/data/datasources/attachment_remote_data_source.dart`, replace:

```dart
  Future<AttachmentModel> uploadAttachment({
    required String workTaskId,
    required String fileName,
    required Uint8List bytes,
  }) async {
    try {
      final formData = FormData.fromMap({
        'file': MultipartFile.fromBytes(bytes, filename: fileName),
      });
      final response = await _dio.post<Map<String, dynamic>>(
        '/worktasks/$workTaskId/attachments',
        data: formData,
      );
      return AttachmentModel.fromJson(response.data!);
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }
```

with:

```dart
  Future<AttachmentModel> uploadAttachment({
    required String workTaskId,
    required String fileName,
    required Uint8List bytes,
    void Function(int sent, int total)? onSendProgress,
  }) async {
    try {
      final formData = FormData.fromMap({
        'file': MultipartFile.fromBytes(bytes, filename: fileName),
      });
      final response = await _dio.post<Map<String, dynamic>>(
        '/worktasks/$workTaskId/attachments',
        data: formData,
        onSendProgress: onSendProgress,
      );
      return AttachmentModel.fromJson(response.data!);
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }
```

- [ ] **Step 4: Thread it through `AttachmentsController`**

In `mobile/taskmgmt_app/lib/features/attachments/presentation/providers/attachment_provider.dart`, replace (lines 25-28):

```dart
  Future<void> uploadAttachment({required String fileName, required Uint8List bytes}) async {
    await ref.read(attachmentRepositoryProvider).uploadAttachment(workTaskId: arg, fileName: fileName, bytes: bytes);
    await refresh();
  }
```

with:

```dart
  Future<void> uploadAttachment({
    required String fileName,
    required Uint8List bytes,
    void Function(int sent, int total)? onSendProgress,
  }) async {
    await ref.read(attachmentRepositoryProvider).uploadAttachment(
          workTaskId: arg,
          fileName: fileName,
          bytes: bytes,
          onSendProgress: onSendProgress,
        );
    await refresh();
  }
```

- [ ] **Step 5: Update the two existing test fakes to keep the project compiling**

In `mobile/taskmgmt_app/test/work_task_detail_test.dart`, replace (lines 147-149):

```dart
  @override
  Future<Attachment> uploadAttachment({required String workTaskId, required String fileName, required Uint8List bytes}) =>
      throw UnimplementedError();
```

with:

```dart
  @override
  Future<Attachment> uploadAttachment({
    required String workTaskId,
    required String fileName,
    required Uint8List bytes,
    void Function(int sent, int total)? onSendProgress,
  }) =>
      throw UnimplementedError();
```

In `mobile/taskmgmt_app/test/attachment_viewer_screen_test.dart`, replace (lines 74-76):

```dart
  @override
  Future<Attachment> uploadAttachment({required String workTaskId, required String fileName, required Uint8List bytes}) =>
      throw UnimplementedError();
```

with:

```dart
  @override
  Future<Attachment> uploadAttachment({
    required String workTaskId,
    required String fileName,
    required Uint8List bytes,
    void Function(int sent, int total)? onSendProgress,
  }) =>
      throw UnimplementedError();
```

- [ ] **Step 6: Verify the project still compiles and all existing tests pass**

Run: `cd mobile/taskmgmt_app && flutter analyze`
Expected: No errors.

Run: `cd mobile/taskmgmt_app && flutter test --concurrency=1`
Expected: PASS — all existing tests green (this task adds no new tests; it's a signature change verified by the full suite compiling and passing).

- [ ] **Step 7: Commit**

```bash
git add mobile/taskmgmt_app/lib/features/attachments/domain/repositories/attachment_repository.dart mobile/taskmgmt_app/lib/features/attachments/data/repositories/attachment_repository_impl.dart mobile/taskmgmt_app/lib/features/attachments/data/datasources/attachment_remote_data_source.dart mobile/taskmgmt_app/lib/features/attachments/presentation/providers/attachment_provider.dart mobile/taskmgmt_app/test/work_task_detail_test.dart mobile/taskmgmt_app/test/attachment_viewer_screen_test.dart
git commit -m "feat(mobile): thread onSendProgress through the attachment upload data layer"
```

---

### Task 4: `UploadQueueList` widget

**Files:**
- Create: `mobile/taskmgmt_app/lib/features/attachments/presentation/widgets/upload_queue_list.dart`
- Test: `mobile/taskmgmt_app/test/upload_queue_list_test.dart`

**Interfaces:**
- Consumes: `UploadQueueItem`, `UploadItemStatus` from Task 2 (`lib/features/attachments/presentation/utils/upload_queue.dart`).
- Produces: `class UploadQueueList extends StatelessWidget { const UploadQueueList({super.key, required this.queue}); final List<UploadQueueItem> queue; }` — Task 5 renders this inside `AttachmentListSection.build()`.

Per-row layout: file name, then either a `LinearProgressIndicator(value: item.progress)` + `'<percent>%'` text (normal/uploading/done), or a red error icon + `item.errorMessage` (error) — exactly as specified in the approved design doc's UI section.

- [ ] **Step 1: Write the failing tests**

Create `mobile/taskmgmt_app/test/upload_queue_list_test.dart`:

```dart
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:taskmgmt_app/features/attachments/presentation/utils/upload_queue.dart';
import 'package:taskmgmt_app/features/attachments/presentation/widgets/upload_queue_list.dart';

void main() {
  testWidgets('Shows file name, progress bar and percentage for an uploading item', (tester) async {
    final queue = [UploadQueueItem(fileName: 'anh.png', progress: 0.42)];

    await tester.pumpWidget(MaterialApp(home: Scaffold(body: UploadQueueList(queue: queue))));

    expect(find.text('anh.png'), findsOneWidget);
    expect(find.byType(LinearProgressIndicator), findsOneWidget);
    expect(find.text('42%'), findsOneWidget);
  });

  testWidgets('Shows an error icon and message instead of a progress bar for a failed item', (tester) async {
    final queue = [
      UploadQueueItem(fileName: 'loi.png', status: UploadItemStatus.error, errorMessage: 'Không thể tải lên tệp.'),
    ];

    await tester.pumpWidget(MaterialApp(home: Scaffold(body: UploadQueueList(queue: queue))));

    expect(find.byIcon(Icons.error_outline), findsOneWidget);
    expect(find.text('Không thể tải lên tệp.'), findsOneWidget);
    expect(find.byType(LinearProgressIndicator), findsNothing);
  });

  testWidgets('Renders one row per queued file', (tester) async {
    final queue = [
      UploadQueueItem(fileName: 'a.png', progress: 1, status: UploadItemStatus.done),
      UploadQueueItem(fileName: 'b.png', progress: 0.1),
    ];

    await tester.pumpWidget(MaterialApp(home: Scaffold(body: UploadQueueList(queue: queue))));

    expect(find.text('a.png'), findsOneWidget);
    expect(find.text('b.png'), findsOneWidget);
    expect(find.byType(LinearProgressIndicator), findsNWidgets(2));
  });
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd mobile/taskmgmt_app && flutter test test/upload_queue_list_test.dart --concurrency=1`
Expected: FAIL — `upload_queue_list.dart` does not exist yet.

- [ ] **Step 3: Implement `UploadQueueList`**

Create `mobile/taskmgmt_app/lib/features/attachments/presentation/widgets/upload_queue_list.dart`:

```dart
import 'package:flutter/material.dart';

import '../utils/upload_queue.dart';

class UploadQueueList extends StatelessWidget {
  const UploadQueueList({super.key, required this.queue});

  final List<UploadQueueItem> queue;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: queue.map((item) => _UploadQueueRow(item: item)).toList(),
    );
  }
}

class _UploadQueueRow extends StatelessWidget {
  const _UploadQueueRow({required this.item});

  final UploadQueueItem item;

  @override
  Widget build(BuildContext context) {
    final isError = item.status == UploadItemStatus.error;

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(item.fileName, overflow: TextOverflow.ellipsis),
                const SizedBox(height: 4),
                isError
                    ? Row(
                        children: [
                          Icon(Icons.error_outline, color: Theme.of(context).colorScheme.error, size: 16),
                          const SizedBox(width: 4),
                          Expanded(
                            child: Text(
                              item.errorMessage ?? 'Lỗi tải lên',
                              style: TextStyle(color: Theme.of(context).colorScheme.error),
                              overflow: TextOverflow.ellipsis,
                            ),
                          ),
                        ],
                      )
                    : LinearProgressIndicator(value: item.progress),
              ],
            ),
          ),
          if (!isError) ...[
            const SizedBox(width: 8),
            Text('${(item.progress * 100).round()}%'),
          ],
        ],
      ),
    );
  }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd mobile/taskmgmt_app && flutter test test/upload_queue_list_test.dart --concurrency=1`
Expected: PASS (3 tests).

- [ ] **Step 5: Commit**

```bash
git add mobile/taskmgmt_app/lib/features/attachments/presentation/widgets/upload_queue_list.dart mobile/taskmgmt_app/test/upload_queue_list_test.dart
git commit -m "feat(mobile): add UploadQueueList progress widget"
```

---

### Task 5: Pick-source bottom sheet + wire `AttachmentListSection`

**Files:**
- Modify: `mobile/taskmgmt_app/lib/features/attachments/presentation/widgets/attachment_list_section.dart`

**Interfaces:**
- Consumes: `PickedUpload`, `runUploadQueue`, `uploadFailureSnackBarMessage`, `UploadQueueItem` (Task 2); `UploadQueueList` (Task 4); `AttachmentsController.uploadAttachment(..., onSendProgress: ...)` (Task 3).
- Produces: nothing new for later tasks (leaf UI task).

The picker calls (`ImagePicker().pickImage`, `FilePicker.platform.pickFiles`) use real platform channels and cannot run under `flutter_test` (`MissingPluginException`), matching the existing precedent for `flutter_pdfview`/`video_player` in this codebase — this task's new interactive logic is therefore verified manually in Task 6, not by an automated test. The pure queue-driving logic and progress UI are already covered by Tasks 2 and 4.

- [ ] **Step 1: Add imports**

In `mobile/taskmgmt_app/lib/features/attachments/presentation/widgets/attachment_list_section.dart`, replace the import block (lines 1-12):

```dart
import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:open_filex/open_filex.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../shared/widgets/inline_empty_state.dart';
import '../../domain/entities/attachment.dart';
import '../providers/attachment_provider.dart';
import '../utils/attachment_temp_file.dart';
```

with:

```dart
import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:image_picker/image_picker.dart';
import 'package:intl/intl.dart';
import 'package:open_filex/open_filex.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../shared/widgets/inline_empty_state.dart';
import '../../domain/entities/attachment.dart';
import '../providers/attachment_provider.dart';
import '../utils/attachment_temp_file.dart';
import '../utils/upload_queue.dart';
import 'upload_queue_list.dart';
```

- [ ] **Step 2: Replace `_isUploading` with the upload queue and rewrite `_upload()`**

Replace (lines 38-59):

```dart
class _AttachmentListSectionState extends ConsumerState<AttachmentListSection> {
  bool _isUploading = false;
  String? _openingAttachmentId;

  Future<void> _upload() async {
    final result = await FilePicker.platform.pickFiles(withData: true);
    final file = result?.files.singleOrNull;
    if (file?.bytes == null) return;

    setState(() => _isUploading = true);
    try {
      await ref
          .read(attachmentsProvider(widget.taskId).notifier)
          .uploadAttachment(fileName: file!.name, bytes: file.bytes!);
    } catch (e) {
      if (!mounted) return;
      final message = e is ApiException ? e.message : 'Không thể tải lên tệp.';
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
    } finally {
      if (mounted) setState(() => _isUploading = false);
    }
  }
```

with:

```dart
class _AttachmentListSectionState extends ConsumerState<AttachmentListSection> {
  List<UploadQueueItem> _queue = [];
  String? _openingAttachmentId;

  Future<List<PickedUpload>> _pickFromCamera() async {
    final xfile = await ImagePicker().pickImage(source: ImageSource.camera);
    if (xfile == null) return [];
    return [PickedUpload(fileName: xfile.name, bytes: await xfile.readAsBytes())];
  }

  Future<List<PickedUpload>> _pickFromGallery() async {
    final xfile = await ImagePicker().pickImage(source: ImageSource.gallery);
    if (xfile == null) return [];
    return [PickedUpload(fileName: xfile.name, bytes: await xfile.readAsBytes())];
  }

  Future<List<PickedUpload>> _pickFiles() async {
    final result = await FilePicker.platform.pickFiles(withData: true, allowMultiple: true);
    if (result == null) return [];
    return result.files
        .where((platformFile) => platformFile.bytes != null)
        .map((platformFile) => PickedUpload(fileName: platformFile.name, bytes: platformFile.bytes!))
        .toList();
  }

  Future<List<PickedUpload>?> _showSourceSheet() {
    return showModalBottomSheet<List<PickedUpload>>(
      context: context,
      builder: (sheetContext) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              leading: const Icon(Icons.photo_camera_outlined),
              title: const Text('Máy ảnh'),
              onTap: () async {
                final picked = await _pickFromCamera();
                if (sheetContext.mounted) Navigator.of(sheetContext).pop(picked);
              },
            ),
            ListTile(
              leading: const Icon(Icons.photo_library_outlined),
              title: const Text('Thư viện ảnh'),
              onTap: () async {
                final picked = await _pickFromGallery();
                if (sheetContext.mounted) Navigator.of(sheetContext).pop(picked);
              },
            ),
            ListTile(
              leading: const Icon(Icons.insert_drive_file_outlined),
              title: const Text('Tệp'),
              onTap: () async {
                final picked = await _pickFiles();
                if (sheetContext.mounted) Navigator.of(sheetContext).pop(picked);
              },
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _upload() async {
    final picked = await _showSourceSheet();
    if (picked == null || picked.isEmpty) return;

    final finalQueue = await runUploadQueue(
      files: picked,
      upload: (file, onSendProgress) => ref
          .read(attachmentsProvider(widget.taskId).notifier)
          .uploadAttachment(fileName: file.fileName, bytes: file.bytes, onSendProgress: onSendProgress),
      onUpdate: (queue) {
        if (mounted) setState(() => _queue = queue);
      },
    );

    if (!mounted) return;
    setState(() => _queue = []);
    final message = uploadFailureSnackBarMessage(finalQueue);
    if (message != null) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
    }
  }
```

Note: per-file upload errors are now captured inside `runUploadQueue` (as `UploadItemStatus.error` + `errorMessage`) rather than thrown, so the old `try`/`catch`/`ApiException` handling in `_upload()` is gone — `AttachmentsController.uploadAttachment`'s exceptions are caught by `runUploadQueue`'s internal `try`/`catch` (Task 2, Step 3), not here.

- [ ] **Step 3: Update the icon button and add the queue list to `build()`**

Replace (lines 126-133):

```dart
                IconButton(
                  icon: _isUploading
                      ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                      : const Icon(Icons.attach_file),
                  tooltip: 'Tải tệp lên',
                  onPressed: _isUploading ? null : _upload,
                ),
              ],
            ),
```

with:

```dart
                IconButton(
                  icon: _queue.isNotEmpty
                      ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                      : const Icon(Icons.attach_file),
                  tooltip: 'Tải tệp lên',
                  onPressed: _queue.isNotEmpty ? null : _upload,
                ),
              ],
            ),
            if (_queue.isNotEmpty) ...[
              const SizedBox(height: 8),
              UploadQueueList(queue: _queue),
            ],
```

- [ ] **Step 4: Run the full test suite**

Run: `cd mobile/taskmgmt_app && flutter analyze`
Expected: No errors.

Run: `cd mobile/taskmgmt_app && flutter test --concurrency=1`
Expected: PASS — all tests green, including Tasks 2-4's new tests and the pre-existing suite (no test exercises the new bottom sheet directly, per this task's note above).

- [ ] **Step 5: Commit**

```bash
git add mobile/taskmgmt_app/lib/features/attachments/presentation/widgets/attachment_list_section.dart
git commit -m "feat(mobile): add pick-source bottom sheet and upload queue UI to AttachmentListSection"
```

---

### Task 6: Manual verification

**Files:** none (verification only).

- [ ] **Step 1: Run the full analyzer + test suite one more time**

Run: `cd mobile/taskmgmt_app && flutter analyze && flutter test --concurrency=1`
Expected: No errors, all tests PASS.

- [ ] **Step 2: Manually verify on an emulator/device**

Launch the app, open a task's detail screen, tap the attach icon, and confirm:
- The bottom sheet shows 3 options: Máy ảnh, Thư viện ảnh, Tệp.
- Picking "Tệp" with multiple files selected uploads them sequentially, each showing its own progress bar and `%` in `UploadQueueList`.
- Forcing one file to fail (e.g. temporarily disconnect network mid-queue, or picking a file the backend rejects) still lets the remaining files upload, and a single SnackBar lists the failed file name(s) at the end.
- The attach icon shows a spinner and is disabled while the queue is running, and returns to the normal icon once done.
- Camera and Thư viện ảnh each upload exactly 1 file with the same progress UI.

This step has no automated equivalent because `image_picker`/`file_picker` are platform-channel plugins (see Task 5's note) — record the outcome in the SDD ledger's deviations/notes section rather than skipping it.

---

## Self-Review Notes

**Spec coverage:**
- Bottom sheet with 3 sources → Task 5, Step 2 (`_showSourceSheet`).
- Multi-file only for "Tệp", 0-1 for Camera/Thư viện ảnh → Task 5, Step 2 (`_pickFromCamera`/`_pickFromGallery` return ≤1, `_pickFiles` returns N).
- Real progress via `Dio.onSendProgress` → Task 3 (threading) + Task 2 (`runUploadQueue` forwards `onSendProgress` to the `upload` closure).
- Partial-failure-tolerant queue → Task 2, Step 3 (`try`/`catch` per file, loop continues) + test 3.
- `runUploadQueue` unit tests (sequential order, progress 0→100, error mid-queue) → Task 2.
- `AttachmentListSection` widget test verifying progress bar/% rendering and queue-clear-with-SnackBar behavior → refined into two more precise, still-fully-automated pieces given Dart's constraint that a test file cannot reach a private `State` class's internals: `UploadQueueList` widget test (Task 4, rendering) + `uploadFailureSnackBarMessage` unit test (Task 2, SnackBar message content). The `setState(() => _queue = [])` clearing itself is a one-line side effect with no branching, consistent with how similarly trivial resets elsewhere in this codebase (e.g. `_openingAttachmentId`) aren't separately unit tested.
- `image_picker: ^1.2.3` dependency + Android/iOS permissions → Task 1.
- Out-of-scope items (multi-select for Camera/Gallery, cancel/pause, per-file retry, compression) → none implemented, matches spec.

**Placeholder scan:** No TBD/TODO/"add appropriate handling" phrases; every step has complete code.

**Type consistency:** `PickedUpload`, `UploadItemStatus`, `UploadQueueItem`, `runUploadQueue`, `uploadFailureSnackBarMessage` (Task 2) are used with identical signatures in Tasks 4 and 5. `onSendProgress`'s type (`void Function(int sent, int total)?`) is identical across `AttachmentRepository`, `AttachmentRepositoryImpl`, `AttachmentRemoteDataSource`, `AttachmentsController` (Task 3) and matches the `upload` closure's parameter in `runUploadQueue` (Task 2) and its call site in `_upload()` (Task 5).
