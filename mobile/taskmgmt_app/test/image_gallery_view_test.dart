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
