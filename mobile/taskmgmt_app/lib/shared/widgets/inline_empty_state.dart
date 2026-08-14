import 'package:flutter/material.dart';

/// Trạng thái rỗng thu nhỏ, dùng trong các mục con nhúng trong 1 Card (bình luận, tệp đính
/// kèm, người tham gia, công việc con, lịch sử thay đổi) - nhỏ gọn hơn [EmptyStateView].
class InlineEmptyState extends StatelessWidget {
  const InlineEmptyState({super.key, required this.icon, required this.message});

  final IconData icon;
  final String message;

  @override
  Widget build(BuildContext context) {
    final color = Theme.of(context).colorScheme.outline;
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 16),
      child: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 32, color: color),
            const SizedBox(height: 8),
            Text(message, style: Theme.of(context).textTheme.bodySmall?.copyWith(color: color)),
          ],
        ),
      ),
    );
  }
}
