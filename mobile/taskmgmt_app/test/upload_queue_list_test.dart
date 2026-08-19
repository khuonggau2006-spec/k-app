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
