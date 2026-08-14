import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../shared/widgets/inline_empty_state.dart';
import '../../domain/entities/work_task.dart';
import '../providers/work_task_provider.dart';
import 'work_task_form_sheet.dart';

class SubtaskListSection extends ConsumerWidget {
  const SubtaskListSection({super.key, required this.taskId});

  final String taskId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final childrenAsync = ref.watch(taskChildrenProvider(taskId));

    return Card(
      margin: EdgeInsets.zero,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text('Công việc con', style: Theme.of(context).textTheme.titleMedium),
                IconButton(
                  icon: const Icon(Icons.add_task_outlined),
                  tooltip: 'Thêm công việc con',
                  onPressed: () => showWorkTaskFormSheet(context, parentTaskId: taskId),
                ),
              ],
            ),
            childrenAsync.when(
              data: (children) {
                if (children.isEmpty) {
                  return const InlineEmptyState(icon: Icons.checklist_outlined, message: 'Chưa có công việc con.');
                }
                return Column(
                  children: children
                      .map(
                        (child) => ListTile(
                          contentPadding: EdgeInsets.zero,
                          leading: Icon(Icons.subdirectory_arrow_right, color: Theme.of(context).colorScheme.outline),
                          title: Text(child.title),
                          subtitle: Text(workTaskStatusLabel(child.status)),
                          trailing: const Icon(Icons.chevron_right),
                          onTap: () => context.push('/tasks/${child.id}'),
                        ),
                      )
                      .toList(),
                );
              },
              loading: () => const Padding(
                padding: EdgeInsets.symmetric(vertical: 16),
                child: Center(child: CircularProgressIndicator()),
              ),
              error: (error, _) => Padding(
                padding: const EdgeInsets.symmetric(vertical: 8),
                child: Text(
                  error is ApiException ? error.message : 'Không tải được công việc con.',
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
