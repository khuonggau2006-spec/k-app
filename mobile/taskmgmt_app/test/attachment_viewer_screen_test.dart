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
  Future<Attachment> uploadAttachment({
    required String workTaskId,
    required String fileName,
    required Uint8List bytes,
    void Function(int sent, int total)? onSendProgress,
  }) =>
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
