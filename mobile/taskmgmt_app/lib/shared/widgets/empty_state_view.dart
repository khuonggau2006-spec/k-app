import 'package:flutter/material.dart';

/// Trạng thái rỗng cho toàn màn hình (danh sách chính) - icon lớn + thông báo,
/// có thể kèm 1 dòng gợi ý hành động tiếp theo.
class EmptyStateView extends StatelessWidget {
  const EmptyStateView({super.key, required this.icon, required this.message, this.hint});

  final IconData icon;
  final String message;
  final String? hint;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 48, color: Theme.of(context).colorScheme.outline),
          const SizedBox(height: 16),
          Text(message),
          if (hint != null) Text(hint!),
        ],
      ),
    );
  }
}
