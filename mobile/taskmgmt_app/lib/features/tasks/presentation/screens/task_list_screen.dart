import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/models/paged_result.dart';
import '../../../../core/network/api_exception.dart';
import '../../../../shared/widgets/empty_state_view.dart';
import '../../../../shared/widgets/error_state_view.dart';
import '../../domain/entities/work_task.dart';
import '../providers/work_task_provider.dart';
import '../widgets/work_task_filter_bar.dart';
import '../widgets/work_task_form_sheet.dart';
import '../widgets/work_task_list_item.dart';

class TaskListScreen extends ConsumerWidget {
  const TaskListScreen({super.key});

  static const path = '/tasks';
  static const name = 'tasks';

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
    } catch (e) {
      if (!context.mounted) return;
      final message = e is ApiException ? e.message : 'Không thể xoá công việc.';
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final tasksAsync = ref.watch(workTasksProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Công việc'),
      ),
      body: Column(
        children: [
          const WorkTaskFilterBar(),
          const SizedBox(height: 4),
          Expanded(
            child: tasksAsync.when(
              data: (result) => _buildList(context, ref, result),
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (error, _) => ErrorStateView(
                message: error is ApiException ? error.message : 'Không thể tải danh sách công việc.',
                onRetry: () => ref.read(workTasksProvider.notifier).refresh(),
              ),
            ),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () => showWorkTaskFormSheet(context),
        tooltip: 'Tạo công việc',
        child: const Icon(Icons.add),
      ),
    );
  }

  Widget _buildList(BuildContext context, WidgetRef ref, PagedResult<WorkTask> result) {
    if (result.items.isEmpty) {
      return const EmptyStateView(
        icon: Icons.task_alt_outlined,
        message: 'Chưa có công việc nào.',
        hint: 'Bấm nút + để tạo công việc mới.',
      );
    }

    return RefreshIndicator(
      onRefresh: () => ref.read(workTasksProvider.notifier).refresh(),
      child: ListView.separated(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.only(bottom: 80),
        itemCount: result.items.length,
        separatorBuilder: (context, index) => const Divider(height: 1),
        itemBuilder: (context, index) {
          final task = result.items[index];
          return WorkTaskListItem(
            task: task,
            onTap: () => context.push('/tasks/${task.id}'),
            onDelete: () => _confirmDelete(context, ref, task),
          );
        },
      ),
    );
  }
}
