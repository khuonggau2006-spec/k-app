import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../users/presentation/providers/user_provider.dart';

Future<List<String>?> showMentionPickerDialog(BuildContext context, {required Set<String> selected}) {
  return showDialog<List<String>>(
    context: context,
    builder: (context) => _MentionPickerDialog(initialSelected: selected),
  );
}

class _MentionPickerDialog extends ConsumerStatefulWidget {
  const _MentionPickerDialog({required this.initialSelected});

  final Set<String> initialSelected;

  @override
  ConsumerState<_MentionPickerDialog> createState() => _MentionPickerDialogState();
}

class _MentionPickerDialogState extends ConsumerState<_MentionPickerDialog> {
  late final Set<String> _selected = {...widget.initialSelected};

  @override
  Widget build(BuildContext context) {
    final usersAsync = ref.watch(usersProvider);

    return AlertDialog(
      title: const Text('Nhắc đến người dùng'),
      content: SizedBox(
        width: 360,
        height: 400,
        child: usersAsync.when(
          data: (users) => ListView(
            shrinkWrap: true,
            children: users
                .map(
                  (user) => CheckboxListTile(
                    value: _selected.contains(user.id),
                    title: Text(user.fullName),
                    subtitle: Text(user.email),
                    onChanged: (checked) => setState(() {
                      if (checked ?? false) {
                        _selected.add(user.id);
                      } else {
                        _selected.remove(user.id);
                      }
                    }),
                  ),
                )
                .toList(),
          ),
          loading: () => const Center(child: CircularProgressIndicator()),
          error: (_, _) => const Text('Không tải được danh sách người dùng.'),
        ),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.of(context).pop(), child: const Text('Huỷ')),
        FilledButton(
          onPressed: () => Navigator.of(context).pop(_selected.toList()),
          child: const Text('Xong'),
        ),
      ],
    );
  }
}
