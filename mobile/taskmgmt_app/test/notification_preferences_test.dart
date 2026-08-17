import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:taskmgmt_app/features/notifications/domain/entities/notification_preference.dart';
import 'package:taskmgmt_app/features/notifications/domain/entities/notification.dart';
import 'package:taskmgmt_app/features/notifications/domain/repositories/notification_repository.dart';
import 'package:taskmgmt_app/core/models/paged_result.dart';
import 'package:taskmgmt_app/features/notifications/presentation/providers/notification_provider.dart';
import 'package:taskmgmt_app/features/notifications/presentation/screens/notification_preferences_screen.dart';

class _FakePreferenceRepository implements NotificationRepository {
  final List<NotificationPreference> prefs = [
    const NotificationPreference(type: 'CommentAdded', isEnabled: true),
    const NotificationPreference(type: 'Overdue', isEnabled: false),
  ];
  bool throwOnUpdate = false;

  @override
  Future<List<NotificationPreference>> getPreferences() async => List.of(prefs);

  @override
  Future<void> updatePreference(String type, bool isEnabled) async {
    if (throwOnUpdate) {
      throw Exception('network error');
    }
    final index = prefs.indexWhere((p) => p.type == type);
    if (index != -1) prefs[index] = prefs[index].copyWith(isEnabled: isEnabled);
  }

  @override
  Future<PagedResult<AppNotification>> getNotifications({bool unreadOnly = false}) async =>
      const PagedResult(items: [], pageNumber: 1, pageSize: 50, totalCount: 0, totalPages: 0);

  @override
  Future<int> getUnreadCount() async => 0;

  @override
  Future<void> markAsRead(String id) async {}

  @override
  Future<void> markAllAsRead() async {}
}

Widget _buildScreen(_FakePreferenceRepository repo) => ProviderScope(
      overrides: [notificationRepositoryProvider.overrideWithValue(repo)],
      child: const MaterialApp(home: NotificationPreferencesScreen()),
    );

void main() {
  testWidgets('Shows a switch per preference with correct initial state', (tester) async {
    final repo = _FakePreferenceRepository();
    await tester.pumpWidget(_buildScreen(repo));
    await tester.pumpAndSettle();

    expect(find.text('Có bình luận mới'), findsOneWidget);
    expect(find.text('Quá hạn'), findsOneWidget);

    final commentSwitch = tester.widget<SwitchListTile>(
      find.widgetWithText(SwitchListTile, 'Có bình luận mới'),
    );
    expect(commentSwitch.value, isTrue);

    final overdueSwitch = tester.widget<SwitchListTile>(
      find.widgetWithText(SwitchListTile, 'Quá hạn'),
    );
    expect(overdueSwitch.value, isFalse);
  });

  testWidgets('Tapping a switch calls updatePreference and flips state', (tester) async {
    final repo = _FakePreferenceRepository();
    await tester.pumpWidget(_buildScreen(repo));
    await tester.pumpAndSettle();

    await tester.tap(find.widgetWithText(SwitchListTile, 'Quá hạn'));
    await tester.pumpAndSettle();

    expect(repo.prefs.firstWhere((p) => p.type == 'Overdue').isEnabled, isTrue);
    final overdueSwitch = tester.widget<SwitchListTile>(
      find.widgetWithText(SwitchListTile, 'Quá hạn'),
    );
    expect(overdueSwitch.value, isTrue);
  });

  testWidgets('API error rolls the switch back and shows a snackbar', (tester) async {
    final repo = _FakePreferenceRepository()..throwOnUpdate = true;
    await tester.pumpWidget(_buildScreen(repo));
    await tester.pumpAndSettle();

    await tester.tap(find.widgetWithText(SwitchListTile, 'Quá hạn'));
    await tester.pumpAndSettle();

    final overdueSwitch = tester.widget<SwitchListTile>(
      find.widgetWithText(SwitchListTile, 'Quá hạn'),
    );
    expect(overdueSwitch.value, isFalse);
    expect(find.byType(SnackBar), findsOneWidget);
  });
}
