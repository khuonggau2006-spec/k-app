import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:taskmgmt_app/features/attendance/domain/entities/attendance_record.dart';
import 'package:taskmgmt_app/features/attendance/domain/entities/attendance_stats.dart';
import 'package:taskmgmt_app/features/attendance/domain/repositories/attendance_repository.dart';
import 'package:taskmgmt_app/features/attendance/presentation/providers/attendance_provider.dart';
import 'package:taskmgmt_app/features/attendance/presentation/screens/attendance_screen.dart';

class _FakeAttendanceRepository implements AttendanceRepository {
  _FakeAttendanceRepository({this.today, this.history = const [], this.stats = const AttendanceStats(daysCheckedIn: 0, totalHoursWorked: 0)});

  AttendanceRecord? today;
  final List<AttendanceRecord> history;
  final AttendanceStats stats;

  @override
  Future<AttendanceRecord> checkIn({required double latitude, required double longitude}) =>
      throw UnimplementedError();

  @override
  Future<AttendanceRecord> checkOut({required double latitude, required double longitude}) =>
      throw UnimplementedError();

  @override
  Future<AttendanceRecord?> getToday() async => today;

  @override
  Future<List<AttendanceRecord>> getHistory({required int year, required int month}) async => history;

  @override
  Future<AttendanceStats> getStats({required int year, required int month}) async => stats;
}

Widget _buildScreen(_FakeAttendanceRepository repo) => ProviderScope(
      overrides: [attendanceRepositoryProvider.overrideWithValue(repo)],
      child: const MaterialApp(home: AttendanceScreen()),
    );

void main() {
  testWidgets('Shows "chưa check-in" when there is no record today', (tester) async {
    await tester.pumpWidget(_buildScreen(_FakeAttendanceRepository()));
    await tester.pumpAndSettle();

    expect(find.textContaining('Chưa check-in'), findsOneWidget);
  });

  testWidgets('Shows check-in time and location when checked in but not out', (tester) async {
    final repo = _FakeAttendanceRepository(
      today: AttendanceRecord(
        id: 'a1',
        workDate: DateTime(2026, 8, 24),
        checkInAtUtc: DateTime.utc(2026, 8, 24, 1, 30),
        checkInLocationName: 'Văn phòng chính',
        checkOutAtUtc: null,
        checkOutLocationName: null,
      ),
    );
    await tester.pumpWidget(_buildScreen(repo));
    await tester.pumpAndSettle();

    expect(find.textContaining('Văn phòng chính'), findsOneWidget);
  });

  testWidgets('Switching to Lịch sử tab shows history rows and stats', (tester) async {
    final repo = _FakeAttendanceRepository(
      history: [
        AttendanceRecord(
          id: 'a1',
          workDate: DateTime(2026, 8, 20),
          checkInAtUtc: DateTime.utc(2026, 8, 20, 1),
          checkInLocationName: 'Văn phòng chính',
          checkOutAtUtc: DateTime.utc(2026, 8, 20, 9),
          checkOutLocationName: 'Văn phòng chính',
        ),
      ],
      stats: const AttendanceStats(daysCheckedIn: 1, totalHoursWorked: 8),
    );
    await tester.pumpWidget(_buildScreen(repo));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Lịch sử'));
    await tester.pumpAndSettle();

    expect(find.textContaining('Văn phòng chính'), findsWidgets);
    expect(find.textContaining('8'), findsWidgets);
  });
}
