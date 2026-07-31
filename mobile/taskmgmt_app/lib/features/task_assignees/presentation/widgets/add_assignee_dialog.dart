import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/network/api_exception.dart';
import '../../../auth/domain/entities/user.dart' as domain;
import '../../../users/presentation/providers/user_provider.dart';
import '../../domain/entities/task_assignee.dart';
import '../providers/task_assignee_provider.dart';

Future<void> showAddAssigneeDialog(BuildContext context, {required String taskId, required List<TaskAssignee> existing}) {
  return showDialog(
    context: context,
    builder: (context) => AddAssigneeDialog(taskId: taskId, existing: existing),
  );
}

class AddAssigneeDialog extends ConsumerStatefulWidget {
  const AddAssigneeDialog({super.key, required this.taskId, required this.existing});

  final String taskId;
  final List<TaskAssignee> existing;

  @override
  ConsumerState<AddAssigneeDialog> createState() => _AddAssigneeDialogState();
}

class _AddAssigneeDialogState extends ConsumerState<AddAssigneeDialog> {
  String? _selectedUserId;
  TaskAssigneeRole _role = TaskAssigneeRole.assignee;
  bool _isSubmitting = false;
  String? _errorMessage;

  Future<void> _submit() async {
    if (_selectedUserId == null) {
      setState(() => _errorMessage = 'Vui lòng chọn người tham gia.');
      return;
    }

    setState(() {
      _isSubmitting = true;
      _errorMessage = null;
    });

    try {
      await ref
          .read(taskAssigneesProvider(widget.taskId).notifier)
          .addAssignee(userId: _selectedUserId!, role: _role);
      if (mounted) Navigator.of(context).pop();
    } catch (e) {
      setState(() {
        _errorMessage = e is ApiException ? e.allMessages.join('\n') : 'Đã có lỗi xảy ra. Vui lòng thử lại.';
      });
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final usersAsync = ref.watch(usersProvider);
    final existingUserIds = widget.existing.map((a) => a.userId).toSet();

    return AlertDialog(
      title: const Text('Thêm người tham gia'),
      content: SizedBox(
        width: 360,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (_errorMessage != null) ...[
              Text(_errorMessage!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
              const SizedBox(height: 12),
            ],
            usersAsync.when(
              data: (users) {
                final available = users.where((u) => !existingUserIds.contains(u.id)).toList();
                if (available.isEmpty) {
                  return const Text('Không còn người dùng nào để thêm.');
                }
                return DropdownButtonFormField<String>(
                  initialValue: _selectedUserId,
                  isExpanded: true,
                  decoration: const InputDecoration(labelText: 'Người dùng'),
                  items: available
                      .map(
                        (domain.User user) => DropdownMenuItem(
                          value: user.id,
                          child: Text('${user.fullName} (${user.email})', overflow: TextOverflow.ellipsis),
                        ),
                      )
                      .toList(),
                  onChanged: (value) => setState(() => _selectedUserId = value),
                );
              },
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (_, _) => const Text('Không tải được danh sách người dùng.'),
            ),
            const SizedBox(height: 12),
            DropdownButtonFormField<TaskAssigneeRole>(
              initialValue: _role,
              isExpanded: true,
              decoration: const InputDecoration(labelText: 'Vai trò'),
              items: TaskAssigneeRole.values
                  .map((role) => DropdownMenuItem(value: role, child: Text(taskAssigneeRoleLabel(role))))
                  .toList(),
              onChanged: (value) {
                if (value != null) setState(() => _role = value);
              },
            ),
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: _isSubmitting ? null : () => Navigator.of(context).pop(),
          child: const Text('Huỷ'),
        ),
        FilledButton(
          onPressed: _isSubmitting ? null : _submit,
          child: _isSubmitting
              ? const SizedBox(height: 16, width: 16, child: CircularProgressIndicator(strokeWidth: 2))
              : const Text('Thêm'),
        ),
      ],
    );
  }
}
