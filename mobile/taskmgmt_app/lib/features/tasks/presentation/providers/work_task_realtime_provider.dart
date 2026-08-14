import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/realtime/realtime_provider.dart';
import '../../../attachments/presentation/providers/attachment_provider.dart';
import '../../../comments/presentation/providers/comment_provider.dart';
import '../../../task_assignees/presentation/providers/task_assignee_provider.dart';
import '../../../task_histories/presentation/providers/task_history_provider.dart';
import 'work_task_provider.dart';

/// Join nhóm SignalR của [taskId] khi có widget đang watch provider này (WorkTaskDetailScreen),
/// tự leave khi không còn ai watch (autoDispose - đúng lúc rời màn hình). Nhận "TaskUpdated" cho
/// đúng task đang mở thì invalidate toàn bộ dữ liệu liên quan để tự tải lại - cùng danh sách
/// invalidate với RefreshIndicator thủ công trong WorkTaskDetailScreen.
final workTaskRealtimeProvider = Provider.autoDispose.family<void, String>((ref, taskId) {
  final realtimeService = ref.watch(realtimeServiceProvider);

  realtimeService.joinTaskGroup(taskId);
  ref.onDispose(() => realtimeService.leaveTaskGroup(taskId));

  final subscription = realtimeService.taskUpdates.listen((event) {
    if (event.workTaskId != taskId) return;

    ref.invalidate(taskDetailProvider(taskId));
    ref.invalidate(taskAssigneesProvider(taskId));
    ref.invalidate(taskChildrenProvider(taskId));
    ref.invalidate(commentsProvider(taskId));
    ref.invalidate(attachmentsProvider(taskId));
    ref.invalidate(taskHistoryProvider(taskId));
  });
  ref.onDispose(subscription.cancel);
});
