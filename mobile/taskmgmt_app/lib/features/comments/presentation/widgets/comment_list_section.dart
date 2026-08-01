import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../../../core/network/api_exception.dart';
import '../../../users/presentation/providers/user_provider.dart';
import '../../domain/entities/comment.dart';
import '../providers/comment_provider.dart';
import 'mention_picker_dialog.dart';

class CommentListSection extends ConsumerStatefulWidget {
  const CommentListSection({super.key, required this.taskId});

  final String taskId;

  @override
  ConsumerState<CommentListSection> createState() => _CommentListSectionState();
}

class _CommentListSectionState extends ConsumerState<CommentListSection> {
  final _contentController = TextEditingController();
  final Set<String> _mentionedUserIds = {};
  bool _isSubmitting = false;

  @override
  void dispose() {
    _contentController.dispose();
    super.dispose();
  }

  Future<void> _pickMentions() async {
    final result = await showMentionPickerDialog(context, selected: _mentionedUserIds);
    if (result != null) {
      setState(() {
        _mentionedUserIds
          ..clear()
          ..addAll(result);
      });
    }
  }

  Future<void> _submit() async {
    final content = _contentController.text.trim();
    if (content.isEmpty) return;

    setState(() => _isSubmitting = true);
    try {
      await ref.read(commentsProvider(widget.taskId).notifier).addComment(
            content: content,
            mentionedUserIds: _mentionedUserIds.toList(),
          );
      _contentController.clear();
      setState(() => _mentionedUserIds.clear());
    } catch (e) {
      if (!mounted) return;
      final message = e is ApiException ? e.message : 'Không thể gửi bình luận.';
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final commentsAsync = ref.watch(commentsProvider(widget.taskId));
    final users = ref.watch(usersProvider).valueOrNull ?? [];
    final mentionedUsers = [
      for (final id in _mentionedUserIds)
        (id: id, name: users.where((u) => u.id == id).map((u) => u.fullName).firstOrNull ?? id),
    ];

    return Card(
      margin: EdgeInsets.zero,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Bình luận', style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            commentsAsync.when(
              data: (comments) {
                if (comments.isEmpty) {
                  return const Padding(
                    padding: EdgeInsets.symmetric(vertical: 8),
                    child: Text('Chưa có bình luận nào.'),
                  );
                }
                return Column(
                  children: [for (final comment in comments) _CommentTile(comment: comment)],
                );
              },
              loading: () => const Padding(
                padding: EdgeInsets.symmetric(vertical: 16),
                child: Center(child: CircularProgressIndicator()),
              ),
              error: (error, _) => Padding(
                padding: const EdgeInsets.symmetric(vertical: 8),
                child: Text(
                  error is ApiException ? error.message : 'Không tải được bình luận.',
                  style: TextStyle(color: Theme.of(context).colorScheme.error),
                ),
              ),
            ),
            const Divider(height: 24),
            if (mentionedUsers.isNotEmpty) ...[
              Wrap(
                spacing: 6,
                runSpacing: -8,
                children: [
                  for (final mentioned in mentionedUsers)
                    Chip(
                      label: Text('@${mentioned.name}'),
                      visualDensity: VisualDensity.compact,
                      onDeleted: () => setState(() => _mentionedUserIds.remove(mentioned.id)),
                    ),
                ],
              ),
              const SizedBox(height: 8),
            ],
            Row(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                IconButton(
                  icon: const Icon(Icons.alternate_email),
                  tooltip: 'Nhắc đến người dùng',
                  onPressed: _isSubmitting ? null : _pickMentions,
                ),
                Expanded(
                  child: TextField(
                    controller: _contentController,
                    minLines: 1,
                    maxLines: 4,
                    decoration: const InputDecoration(hintText: 'Viết bình luận...', border: OutlineInputBorder()),
                    enabled: !_isSubmitting,
                  ),
                ),
                const SizedBox(width: 8),
                IconButton.filled(
                  icon: _isSubmitting
                      ? const SizedBox(height: 16, width: 16, child: CircularProgressIndicator(strokeWidth: 2))
                      : const Icon(Icons.send),
                  onPressed: _isSubmitting ? null : _submit,
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _CommentTile extends StatelessWidget {
  const _CommentTile({required this.comment});

  final Comment comment;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          CircleAvatar(
            child: Text(comment.authorFullName.isNotEmpty ? comment.authorFullName[0].toUpperCase() : '?'),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Text(comment.authorFullName, style: const TextStyle(fontWeight: FontWeight.w600)),
                    const SizedBox(width: 8),
                    Text(
                      DateFormat('dd/MM/yyyy HH:mm').format(comment.createdAtUtc.toLocal()),
                      style: Theme.of(context).textTheme.bodySmall,
                    ),
                  ],
                ),
                const SizedBox(height: 2),
                Text(comment.content),
                if (comment.mentions.isNotEmpty) ...[
                  const SizedBox(height: 4),
                  Wrap(
                    spacing: 4,
                    children: [
                      for (final mention in comment.mentions)
                        Text(
                          '@${mention.fullName}',
                          style: TextStyle(color: Theme.of(context).colorScheme.primary, fontSize: 12),
                        ),
                    ],
                  ),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }
}
