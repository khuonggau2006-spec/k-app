import 'package:flutter/material.dart';

import '../utils/upload_queue.dart';

class UploadQueueList extends StatelessWidget {
  const UploadQueueList({super.key, required this.queue});

  final List<UploadQueueItem> queue;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: queue.map((item) => _UploadQueueRow(item: item)).toList(),
    );
  }
}

class _UploadQueueRow extends StatelessWidget {
  const _UploadQueueRow({required this.item});

  final UploadQueueItem item;

  @override
  Widget build(BuildContext context) {
    final isError = item.status == UploadItemStatus.error;

    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(item.fileName, overflow: TextOverflow.ellipsis),
                const SizedBox(height: 4),
                isError
                    ? Row(
                        children: [
                          Icon(Icons.error_outline, color: Theme.of(context).colorScheme.error, size: 16),
                          const SizedBox(width: 4),
                          Expanded(
                            child: Text(
                              item.errorMessage ?? 'Lỗi tải lên',
                              style: TextStyle(color: Theme.of(context).colorScheme.error),
                              overflow: TextOverflow.ellipsis,
                            ),
                          ),
                        ],
                      )
                    : LinearProgressIndicator(value: item.progress),
              ],
            ),
          ),
          if (!isError) ...[
            const SizedBox(width: 8),
            Text('${(item.progress * 100).round()}%'),
          ],
        ],
      ),
    );
  }
}
