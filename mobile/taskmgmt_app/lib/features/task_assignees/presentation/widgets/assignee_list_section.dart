import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/network/api_exception.dart';
import '../../../auth/domain/entities/user.dart' as domain;
import '../../../auth/presentation/providers/auth_provider.dart';
import '../../domain/entities/task_assignee.dart';
import '../providers/task_assignee_provider.dart';
import 'add_assignee_dialog.dart';

class AssigneeListSection extends ConsumerWidget {
  const AssigneeListSection({super.key, required this.taskId});

  final String taskId;

  bool _canManage(List<TaskAssignee> assignees, domain.User? currentUser) {
    if (currentUser == null) return false;
    if (currentUser.systemRole == domain.SystemRole.admin || currentUser.systemRole == domain.SystemRole.manager) {
      return true;
    }
    return assignees.any((a) => a.userId == currentUser.id && a.role == TaskAssigneeRole.owner);
  }

  Future<void> _handleAction(BuildContext context, WidgetRef ref, TaskAssignee assignee, String action) async {
    final controller = ref.read(taskAssigneesProvider(taskId).notifier);
    try {
      if (action == 'remove') {
        final confirmed = await showDialog<bool>(
          context: context,
          builder: (context) => AlertDialog(
            title: const Text('Gỡ người tham gia?'),
            content: Text('Gỡ ${assignee.userFullName} khỏi công việc này?'),
            actions: [
              TextButton(onPressed: () => Navigator.of(context).pop(false), child: const Text('Huỷ')),
              FilledButton(onPressed: () => Navigator.of(context).pop(true), child: const Text('Gỡ')),
            ],
          ),
        );
        if (confirmed == true) {
          await controller.removeAssignee(assignee.userId);
        }
      } else {
        final role = TaskAssigneeRole.values.firstWhere((r) => r.name == action);
        await controller.changeRole(userId: assignee.userId, role: role);
      }
    } catch (e) {
      if (!context.mounted) return;
      final message = e is ApiException ? e.message : 'Thao tác thất bại.';
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final assigneesAsync = ref.watch(taskAssigneesProvider(taskId));
    final currentUser = ref.watch(authControllerProvider).valueOrNull;

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
                Text('Người tham gia', style: Theme.of(context).textTheme.titleMedium),
                assigneesAsync.when(
                  data: (assignees) => _canManage(assignees, currentUser)
                      ? IconButton(
                          icon: const Icon(Icons.person_add_alt_outlined),
                          tooltip: 'Thêm người tham gia',
                          onPressed: () => showAddAssigneeDialog(context, taskId: taskId, existing: assignees),
                        )
                      : const SizedBox.shrink(),
                  loading: () => const SizedBox.shrink(),
                  error: (_, _) => const SizedBox.shrink(),
                ),
              ],
            ),
            assigneesAsync.when(
              data: (assignees) {
                if (assignees.isEmpty) {
                  return const Padding(
                    padding: EdgeInsets.symmetric(vertical: 8),
                    child: Text('Chưa có ai tham gia công việc này.'),
                  );
                }
                final canManage = _canManage(assignees, currentUser);
                return Column(
                  children: assignees
                      .map(
                        (assignee) => ListTile(
                          contentPadding: EdgeInsets.zero,
                          leading: CircleAvatar(
                            child: Text(assignee.userFullName.isNotEmpty ? assignee.userFullName[0].toUpperCase() : '?'),
                          ),
                          title: Text(assignee.userFullName),
                          subtitle: Text('${assignee.userEmail} • ${taskAssigneeRoleLabel(assignee.role)}'),
                          trailing: canManage
                              ? PopupMenuButton<String>(
                                  onSelected: (value) => _handleAction(context, ref, assignee, value),
                                  itemBuilder: (context) => [
                                    for (final role in TaskAssigneeRole.values)
                                      if (role != assignee.role)
                                        PopupMenuItem(
                                          value: role.name,
                                          child: Text('Đổi thành ${taskAssigneeRoleLabel(role)}'),
                                        ),
                                    const PopupMenuDivider(),
                                    const PopupMenuItem(value: 'remove', child: Text('Gỡ khỏi công việc')),
                                  ],
                                )
                              : null,
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
                  error is ApiException ? error.message : 'Không tải được danh sách người tham gia.',
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
