import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../../../shared/widgets/work_task_status_style.dart';
import '../../../tasks/domain/entities/work_task.dart';

class WorkTaskKanbanBoard extends StatelessWidget {
  const WorkTaskKanbanBoard({super.key, required this.tasks, required this.onTaskTap});

  final List<WorkTask> tasks;
  final ValueChanged<WorkTask> onTaskTap;

  static const _columns = [
    WorkTaskStatus.toDo,
    WorkTaskStatus.inProgress,
    WorkTaskStatus.inReview,
    WorkTaskStatus.done,
    WorkTaskStatus.cancelled,
  ];

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 420,
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: _columns
            .map(
              (status) => _KanbanColumn(
                status: status,
                tasks: tasks.where((t) => t.status == status).toList(),
                onTaskTap: onTaskTap,
              ),
            )
            .toList(),
      ),
    );
  }
}

class _KanbanColumn extends StatelessWidget {
  const _KanbanColumn({required this.status, required this.tasks, required this.onTaskTap});

  final WorkTaskStatus status;
  final List<WorkTask> tasks;
  final ValueChanged<WorkTask> onTaskTap;

  @override
  Widget build(BuildContext context) {
    final color = workTaskStatusColor(status);

    return SizedBox(
      width: 220,
      child: Card(
        margin: const EdgeInsets.only(right: 12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
              decoration: BoxDecoration(
                border: Border(bottom: BorderSide(color: Theme.of(context).dividerColor)),
              ),
              child: Row(
                children: [
                  CircleAvatar(backgroundColor: color, radius: 5),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      workTaskStatusLabel(status),
                      style: Theme.of(context).textTheme.labelLarge,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                    decoration: BoxDecoration(
                      color: Theme.of(context).colorScheme.surfaceContainerHighest,
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: Text('${tasks.length}', style: Theme.of(context).textTheme.labelSmall),
                  ),
                ],
              ),
            ),
            Expanded(
              child: tasks.isEmpty
                  ? Center(
                      child: Text(
                        'Trống',
                        style: Theme.of(context)
                            .textTheme
                            .bodySmall
                            ?.copyWith(color: Theme.of(context).colorScheme.outline),
                      ),
                    )
                  : ListView.builder(
                      padding: const EdgeInsets.all(8),
                      itemCount: tasks.length,
                      itemBuilder: (context, index) => _KanbanCard(task: tasks[index], onTap: onTaskTap),
                    ),
            ),
          ],
        ),
      ),
    );
  }
}

class _KanbanCard extends StatelessWidget {
  const _KanbanCard({required this.task, required this.onTap});

  final WorkTask task;
  final ValueChanged<WorkTask> onTap;

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      elevation: 0,
      color: Theme.of(context).colorScheme.surfaceContainerLow,
      child: InkWell(
        onTap: () => onTap(task),
        child: Padding(
          padding: const EdgeInsets.all(8),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(task.title, maxLines: 2, overflow: TextOverflow.ellipsis),
              if (task.dueDateUtc != null) ...[
                const SizedBox(height: 4),
                Text(
                  DateFormat('dd/MM/yyyy').format(task.dueDateUtc!.toLocal()),
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                        color: task.isOverdue
                            ? Theme.of(context).colorScheme.error
                            : Theme.of(context).colorScheme.onSurfaceVariant,
                      ),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}
