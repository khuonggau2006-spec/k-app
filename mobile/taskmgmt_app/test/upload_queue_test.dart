import 'dart:typed_data';

import 'package:flutter_test/flutter_test.dart';

import 'package:taskmgmt_app/features/attachments/presentation/utils/upload_queue.dart';

void main() {
  test('Uploads files sequentially in order, marking each done', () async {
    final uploadedOrder = <String>[];
    final files = [
      PickedUpload(fileName: 'a.png', bytes: Uint8List(0)),
      PickedUpload(fileName: 'b.png', bytes: Uint8List(0)),
      PickedUpload(fileName: 'c.png', bytes: Uint8List(0)),
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
      files: [PickedUpload(fileName: 'big.zip', bytes: Uint8List(0))],
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
      PickedUpload(fileName: 'a.png', bytes: Uint8List(0)),
      PickedUpload(fileName: 'bad.png', bytes: Uint8List(0)),
      PickedUpload(fileName: 'c.png', bytes: Uint8List(0)),
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
      files: [PickedUpload(fileName: 'a.png', bytes: Uint8List(0))],
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
