import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../shared/widgets/error_state_view.dart';
import '../providers/preferences_provider.dart';

class NotificationPreferencesScreen extends ConsumerWidget {
  const NotificationPreferencesScreen({super.key});

  static const path = '/notifications/preferences';
  static const name = 'notification-preferences';

  Future<void> _handleToggle(BuildContext context, WidgetRef ref, String type, bool value) async {
    try {
      await ref.read(notificationPreferencesProvider.notifier).toggle(type, value);
    } catch (e) {
      if (!context.mounted) return;
      final message = e is ApiException ? e.message : 'Không thể cập nhật cài đặt.';
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final preferencesAsync = ref.watch(notificationPreferencesProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Cài đặt thông báo')),
      body: preferencesAsync.when(
        data: (preferences) => ListView(
          children: [
            for (final pref in preferences)
              SwitchListTile(
                title: Text(pref.label),
                value: pref.isEnabled,
                onChanged: (value) => _handleToggle(context, ref, pref.type, value),
              ),
          ],
        ),
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => ErrorStateView(
          message: error is ApiException ? error.message : 'Không thể tải cài đặt thông báo.',
          onRetry: () => ref.invalidate(notificationPreferencesProvider),
        ),
      ),
    );
  }
}
