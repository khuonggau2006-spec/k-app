import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../domain/entities/notification.dart';

class NotificationListItem extends StatelessWidget {
  const NotificationListItem({super.key, required this.notification, required this.onTap});

  final AppNotification notification;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return ListTile(
      onTap: onTap,
      tileColor: notification.isRead ? null : colorScheme.primaryContainer.withValues(alpha: 0.25),
      leading: CircleAvatar(
        backgroundColor: colorScheme.secondaryContainer,
        child: Icon(_iconFor(notification.type), color: colorScheme.onSecondaryContainer, size: 20),
      ),
      title: Text(
        notification.title,
        style: TextStyle(fontWeight: notification.isRead ? FontWeight.normal : FontWeight.bold),
      ),
      subtitle: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(notification.body),
          const SizedBox(height: 4),
          Text(
            DateFormat('dd/MM/yyyy HH:mm').format(notification.createdAtUtc.toLocal()),
            style: Theme.of(context).textTheme.bodySmall?.copyWith(color: colorScheme.onSurfaceVariant),
          ),
        ],
      ),
      isThreeLine: true,
      trailing: notification.isRead
          ? null
          : Icon(Icons.circle, size: 10, color: colorScheme.primary),
    );
  }

  IconData _iconFor(String type) => switch (type) {
        'FieldChanged' => Icons.edit_outlined,
        'StatusChanged' => Icons.sync_outlined,
        'Deleted' => Icons.delete_outline,
        'AssigneeAdded' => Icons.person_add_outlined,
        'AssigneeRemoved' => Icons.person_remove_outlined,
        'AssigneeRoleChanged' => Icons.admin_panel_settings_outlined,
        'CommentAdded' => Icons.chat_bubble_outline,
        'AttachmentAdded' => Icons.attach_file,
        _ => Icons.notifications_outlined,
      };
}
