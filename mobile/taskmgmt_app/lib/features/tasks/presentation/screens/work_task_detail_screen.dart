import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../../../core/network/api_exception.dart';
import '../../../attachments/presentation/providers/attachment_provider.dart';
import '../../../attachments/presentation/widgets/attachment_list_section.dart';
import '../../../comments/presentation/providers/comment_provider.dart';
import '../../../comments/presentation/widgets/comment_list_section.dart';
import '../../../locations/presentation/providers/location_provider.dart';
import '../../../task_assignees/presentation/providers/task_assignee_provider.dart';
import '../../../task_assignees/presentation/widgets/assignee_list_section.dart';
import '../../../task_histories/presentation/providers/task_history_provider.dart';
import '../../../task_histories/presentation/widgets/task_history_timeline_section.dart';
import '../../domain/entities/work_task.dart';
import '../providers/work_task_provider.dart';
import '../providers/work_task_realtime_provider.dart';
import '../widgets/subtask_list_section.dart';
import '../widgets/work_task_form_sheet.dart';

class WorkTaskDetailScreen extends ConsumerWidget {
  const WorkTaskDetailScreen({super.key, required this.taskId});

  final String taskId;

  static String pathFor(String id) => '/tasks/$id';
  static const name = 'task-detail';

  Future<void> _confirmDelete(BuildContext context, WidgetRef ref, WorkTask task) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Xoá công việc?'),
        content: Text('Bạn có chắc muốn xoá "${task.title}"?'),
        actions: [
          TextButton(onPressed: () => Navigator.of(context).pop(false), child: const Text('Huỷ')),
          FilledButton(onPressed: () => Navigator.of(context).pop(true), child: const Text('Xoá')),
        ],
      ),
    );

    if (confirmed != true || !context.mounted) return;

    try {
      await ref.read(workTasksProvider.notifier).deleteTask(task.id);
      if (context.mounted) context.pop();
    } catch (e) {
      if (!context.mounted) return;
      final message = e is ApiException ? e.message : 'Không thể xoá công việc.';
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final taskAsync = ref.watch(taskDetailProvider(taskId));
    // Join nhóm SignalR của task này khi màn hình mở, tự leave khi rời màn hình (autoDispose) -
    // xem workTaskRealtimeProvider.
    ref.watch(workTaskRealtimeProvider(taskId));

    return Scaffold(
      appBar: AppBar(
        title: const Text('Chi tiết công việc'),
        actions: [
          if (taskAsync.hasValue) ...[
            IconButton(
              icon: const Icon(Icons.edit_outlined),
              tooltip: 'Sửa',
              onPressed: () => showWorkTaskFormSheet(context, task: taskAsync.value),
            ),
            IconButton(
              icon: const Icon(Icons.delete_outline),
              tooltip: 'Xoá',
              onPressed: () => _confirmDelete(context, ref, taskAsync.value!),
            ),
          ],
        ],
      ),
      body: taskAsync.when(
        data: (task) => RefreshIndicator(
          onRefresh: () async {
            ref.invalidate(taskDetailProvider(taskId));
            ref.invalidate(taskAssigneesProvider(taskId));
            ref.invalidate(taskChildrenProvider(taskId));
            ref.invalidate(commentsProvider(taskId));
            ref.invalidate(attachmentsProvider(taskId));
            ref.invalidate(taskHistoryProvider(taskId));
          },
          child: ListView(
            padding: const EdgeInsets.all(16),
            children: [
              _GeneralInfoCard(task: task),
              const SizedBox(height: 16),
              AssigneeListSection(taskId: taskId),
              const SizedBox(height: 16),
              SubtaskListSection(taskId: taskId),
              const SizedBox(height: 16),
              CommentListSection(taskId: taskId),
              const SizedBox(height: 16),
              AttachmentListSection(taskId: taskId),
              const SizedBox(height: 16),
              TaskHistoryTimelineSection(taskId: taskId),
            ],
          ),
        ),
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => Center(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(Icons.error_outline, size: 48, color: Theme.of(context).colorScheme.error),
                const SizedBox(height: 16),
                Text(
                  error is ApiException ? error.message : 'Không thể tải công việc.',
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 16),
                FilledButton(
                  onPressed: () => ref.invalidate(taskDetailProvider(taskId)),
                  child: const Text('Thử lại'),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _GeneralInfoCard extends ConsumerWidget {
  const _GeneralInfoCard({required this.task});

  final WorkTask task;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final locations = ref.watch(locationsProvider).valueOrNull ?? [];
    String? locationName;
    if (task.locationId != null) {
      for (final location in locations) {
        if (location.id == task.locationId) {
          locationName = location.name;
          break;
        }
      }
    }

    return Card(
      margin: EdgeInsets.zero,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (task.parentTaskId != null)
              Padding(
                padding: const EdgeInsets.only(bottom: 8),
                child: TextButton.icon(
                  onPressed: () => context.push('/tasks/${task.parentTaskId}'),
                  icon: const Icon(Icons.subdirectory_arrow_left, size: 18),
                  label: const Text('Xem công việc cha'),
                  style: TextButton.styleFrom(padding: EdgeInsets.zero, alignment: Alignment.centerLeft),
                ),
              ),
            Text(task.title, style: Theme.of(context).textTheme.headlineSmall),
            const SizedBox(height: 8),
            Chip(label: Text(workTaskStatusLabel(task.status)), visualDensity: VisualDensity.compact),
            if (task.description != null && task.description!.isNotEmpty) ...[
              const SizedBox(height: 12),
              Text(task.description!),
            ],
            const SizedBox(height: 12),
            if (task.dueDateUtc != null)
              _InfoRow(
                icon: Icons.event_outlined,
                text: 'Hạn hoàn thành: ${DateFormat('dd/MM/yyyy').format(task.dueDateUtc!.toLocal())}',
                color: task.isOverdue ? Theme.of(context).colorScheme.error : null,
              ),
            if (locationName != null) _InfoRow(icon: Icons.location_on_outlined, text: locationName),
          ],
        ),
      ),
    );
  }
}

class _InfoRow extends StatelessWidget {
  const _InfoRow({required this.icon, required this.text, this.color});

  final IconData icon;
  final String text;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    final effectiveColor = color ?? Theme.of(context).colorScheme.onSurfaceVariant;
    return Padding(
      padding: const EdgeInsets.only(top: 4),
      child: Row(
        children: [
          Icon(icon, size: 18, color: effectiveColor),
          const SizedBox(width: 8),
          Text(text, style: TextStyle(color: effectiveColor)),
        ],
      ),
    );
  }
}
