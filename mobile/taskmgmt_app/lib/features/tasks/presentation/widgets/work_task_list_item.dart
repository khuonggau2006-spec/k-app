import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../../shared/widgets/work_task_status_style.dart';
import '../../../locations/presentation/providers/location_provider.dart';
import '../../domain/entities/work_task.dart';

class WorkTaskListItem extends ConsumerWidget {
  const WorkTaskListItem({super.key, required this.task, required this.onTap, required this.onDelete});

  final WorkTask task;
  final VoidCallback onTap;
  final VoidCallback onDelete;

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

    return ListTile(
      onTap: onTap,
      leading: _StatusDot(status: task.status),
      title: Text(task.title),
      subtitle: Wrap(
        spacing: 12,
        runSpacing: 4,
        children: [
          Chip(
            label: Text(workTaskStatusLabel(task.status)),
            visualDensity: VisualDensity.compact,
            padding: EdgeInsets.zero,
          ),
          if (task.dueDateUtc != null)
            _InfoTag(
              icon: Icons.event_outlined,
              text: DateFormat('dd/MM/yyyy').format(task.dueDateUtc!.toLocal()),
              color: task.isOverdue ? Theme.of(context).colorScheme.error : null,
            ),
          if (locationName != null) _InfoTag(icon: Icons.location_on_outlined, text: locationName),
        ],
      ),
      trailing: IconButton(
        icon: const Icon(Icons.delete_outline),
        tooltip: 'Xoá',
        onPressed: onDelete,
      ),
    );
  }
}

class _StatusDot extends StatelessWidget {
  const _StatusDot({required this.status});

  final WorkTaskStatus status;

  @override
  Widget build(BuildContext context) {
    return CircleAvatar(backgroundColor: workTaskStatusColor(status), radius: 6);
  }
}

class _InfoTag extends StatelessWidget {
  const _InfoTag({required this.icon, required this.text, this.color});

  final IconData icon;
  final String text;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    final effectiveColor = color ?? Theme.of(context).colorScheme.onSurfaceVariant;
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 14, color: effectiveColor),
        const SizedBox(width: 4),
        Text(text, style: Theme.of(context).textTheme.bodySmall?.copyWith(color: effectiveColor)),
      ],
    );
  }
}
