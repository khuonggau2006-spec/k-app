import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../domain/entities/notification_preference.dart';
import 'notification_provider.dart';

final notificationPreferencesProvider =
    AsyncNotifierProvider<NotificationPreferencesController, List<NotificationPreference>>(
        NotificationPreferencesController.new);

class NotificationPreferencesController extends AsyncNotifier<List<NotificationPreference>> {
  @override
  Future<List<NotificationPreference>> build() =>
      ref.read(notificationRepositoryProvider).getPreferences();

  // Optimistic: đổi UI ngay, gọi API, rollback nếu lỗi - tránh switch "đứng hình" chờ mạng.
  Future<void> toggle(String type, bool isEnabled) async {
    final previous = state;
    final current = state.valueOrNull;
    if (current == null) return;

    state = AsyncData([
      for (final pref in current)
        if (pref.type == type) pref.copyWith(isEnabled: isEnabled) else pref,
    ]);

    try {
      await ref.read(notificationRepositoryProvider).updatePreference(type, isEnabled);
    } catch (e) {
      state = previous;
      rethrow;
    }
  }
}
