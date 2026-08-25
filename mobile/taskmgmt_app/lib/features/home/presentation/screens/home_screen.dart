import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../attendance/presentation/screens/attendance_screen.dart';
import '../../../auth/presentation/providers/auth_provider.dart';
import '../../../dashboard/presentation/screens/dashboard_screen.dart';
import '../../../locations/presentation/screens/location_list_screen.dart';
import '../../../notifications/presentation/providers/notification_provider.dart';
import '../../../notifications/presentation/screens/notification_center_screen.dart';
import '../../../tasks/presentation/screens/task_list_screen.dart';

class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  static const path = '/home';
  static const name = 'home';

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final user = ref.watch(authControllerProvider).valueOrNull;
    final unreadCount = ref.watch(unreadNotificationCountProvider).valueOrNull ?? 0;
    final colorScheme = Theme.of(context).colorScheme;

    final tiles = [
      _HomeTileData('Công việc', Icons.task_alt_outlined, colorScheme.primary, TaskListScreen.path),
      _HomeTileData('Dashboard', Icons.dashboard_outlined, Colors.blue, DashboardScreen.path),
      _HomeTileData('Chấm công', Icons.fingerprint, Colors.teal, AttendanceScreen.path),
      _HomeTileData('Vị trí', Icons.location_on_outlined, Colors.orange, LocationListScreen.path),
      _HomeTileData(
        'Thông báo',
        Icons.notifications_outlined,
        colorScheme.error,
        NotificationCenterScreen.path,
        badgeCount: unreadCount,
      ),
    ];

    return Scaffold(
      appBar: AppBar(
        title: const Text('Trang chủ'),
        actions: [
          IconButton(
            icon: const Icon(Icons.logout),
            tooltip: 'Đăng xuất',
            onPressed: () => ref.read(authControllerProvider.notifier).logout(),
          ),
        ],
      ),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (user != null)
              Padding(
                padding: const EdgeInsets.only(bottom: 16),
                child: Text('Xin chào, ${user.fullName}', style: Theme.of(context).textTheme.bodyLarge),
              ),
            Expanded(
              child: GridView.count(
                crossAxisCount: 2,
                mainAxisSpacing: 12,
                crossAxisSpacing: 12,
                childAspectRatio: 1.1,
                children: tiles.map((tile) => _HomeTile(data: tile)).toList(),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _HomeTileData {
  const _HomeTileData(this.label, this.icon, this.color, this.path, {this.badgeCount = 0});

  final String label;
  final IconData icon;
  final Color color;
  final String path;
  final int badgeCount;
}

class _HomeTile extends StatelessWidget {
  const _HomeTile({required this.data});

  final _HomeTileData data;

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: EdgeInsets.zero,
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () => context.push(data.path),
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Badge.count(
                count: data.badgeCount,
                isLabelVisible: data.badgeCount > 0,
                child: CircleAvatar(
                  radius: 24,
                  backgroundColor: data.color.withValues(alpha: 0.15),
                  child: Icon(data.icon, color: data.color, size: 26),
                ),
              ),
              const SizedBox(height: 12),
              Text(data.label, style: Theme.of(context).textTheme.bodyMedium, textAlign: TextAlign.center),
            ],
          ),
        ),
      ),
    );
  }
}
