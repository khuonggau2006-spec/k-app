import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../shared/widgets/inline_empty_state.dart';
import '../../domain/entities/task_history.dart';
import '../providers/task_history_provider.dart';

IconData _iconFor(TaskHistoryActionType type) => switch (type) {
      TaskHistoryActionType.created => Icons.add_circle_outline,
      TaskHistoryActionType.fieldChanged => Icons.edit_outlined,
      TaskHistoryActionType.statusChanged => Icons.swap_horiz,
      TaskHistoryActionType.deleted => Icons.delete_outline,
      TaskHistoryActionType.assigneeAdded => Icons.person_add_alt_outlined,
      TaskHistoryActionType.assigneeRemoved => Icons.person_remove_alt_1_outlined,
      TaskHistoryActionType.assigneeRoleChanged => Icons.manage_accounts_outlined,
      TaskHistoryActionType.commentAdded => Icons.comment_outlined,
      TaskHistoryActionType.attachmentAdded => Icons.attach_file,
      TaskHistoryActionType.attachmentRemoved => Icons.attachment_outlined,
    };

class TaskHistoryTimelineSection extends ConsumerWidget {
  const TaskHistoryTimelineSection({super.key, required this.taskId});

  final String taskId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final historyAsync = ref.watch(taskHistoryProvider(taskId));

    return Card(
      margin: EdgeInsets.zero,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Lịch sử thay đổi', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 4),
            historyAsync.when(
              data: (entries) {
                if (entries.isEmpty) {
                  return const InlineEmptyState(icon: Icons.history, message: 'Chưa có lịch sử thay đổi.');
                }
                return Column(
                  children: [for (final entry in entries) _HistoryTile(entry: entry)],
                );
              },
              loading: () => const Padding(
                padding: EdgeInsets.symmetric(vertical: 16),
                child: Center(child: CircularProgressIndicator()),
              ),
              error: (error, _) => Padding(
                padding: const EdgeInsets.symmetric(vertical: 8),
                child: Text(
                  error is ApiException ? error.message : 'Không tải được lịch sử thay đổi.',
                  style: TextStyle(color: Theme.of(context).colorScheme.error),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _HistoryTile extends StatelessWidget {
  const _HistoryTile({required this.entry});

  final TaskHistory entry;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          CircleAvatar(
            radius: 16,
            backgroundColor: colorScheme.primaryContainer,
            child: Icon(_iconFor(entry.actionType), size: 16, color: colorScheme.onPrimaryContainer),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(entry.description),
                const SizedBox(height: 2),
                Text(
                  '${entry.actorFullName.isNotEmpty ? entry.actorFullName : 'Hệ thống'}'
                  '${entry.targetUserFullName != null ? ' → ${entry.targetUserFullName}' : ''}'
                  ' • ${DateFormat('dd/MM/yyyy HH:mm').format(entry.createdAtUtc.toLocal())}',
                  style: Theme.of(context).textTheme.bodySmall,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
