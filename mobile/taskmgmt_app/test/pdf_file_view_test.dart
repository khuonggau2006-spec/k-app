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
