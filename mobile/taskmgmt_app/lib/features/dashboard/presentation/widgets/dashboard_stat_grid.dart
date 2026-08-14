import 'package:flutter/material.dart';

import '../../domain/entities/dashboard_stats.dart';

class DashboardStatGrid extends StatelessWidget {
  const DashboardStatGrid({super.key, required this.stats});

  final DashboardStats stats;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final tiles = [
      _StatTileData('Tổng cộng', stats.totalActive, Icons.list_alt_outlined, colorScheme.primary),
      _StatTileData('Đang thực hiện', stats.inProgressCount, Icons.play_circle_outline, Colors.blue),
      _StatTileData('Quá hạn', stats.overdueCount, Icons.warning_amber_outlined, colorScheme.error),
      _StatTileData('Sắp đến hạn', stats.dueSoonCount, Icons.schedule_outlined, Colors.orange),
    ];

    return GridView.count(
      crossAxisCount: 2,
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      mainAxisSpacing: 12,
      crossAxisSpacing: 12,
      childAspectRatio: 2.2,
      children: tiles.map((tile) => _StatTile(data: tile)).toList(),
    );
  }
}

class _StatTileData {
  const _StatTileData(this.label, this.count, this.icon, this.color);

  final String label;
  final int count;
  final IconData icon;
  final Color color;
}

class _StatTile extends StatelessWidget {
  const _StatTile({required this.data});

  final _StatTileData data;

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: EdgeInsets.zero,
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Row(
          children: [
            CircleAvatar(
              backgroundColor: data.color.withValues(alpha: 0.15),
              child: Icon(data.icon, color: data.color, size: 20),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    '${data.count}',
                    style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold),
                  ),
                  Text(
                    data.label,
                    style: Theme.of(context).textTheme.bodySmall,
                    overflow: TextOverflow.ellipsis,
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
