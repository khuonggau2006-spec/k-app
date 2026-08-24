import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:taskmgmt_app/features/attendance/domain/entities/attendance_record.dart';
import 'package:taskmgmt_app/features/attendance/domain/entities/attendance_stats.dart';
import 'package:taskmgmt_app/features/attendance/domain/repositories/attendance_repository.dart';
import 'package:taskmgmt_app/features/attendance/presentation/providers/attendance_provider.dart';

class _FakeAttendanceRepository implements AttendanceRepository {
  AttendanceRecord? today;
  bool throwOnCheckIn = false;

  @override
  Future<AttendanceRecord> checkIn({required double latitude, required double longitude}) async {
    if (throwOnCheckIn) throw Exception('Ngoài phạm vi cho phép của mọi vị trí đã đăng ký.');
    today = AttendanceRecord(
      id: 'a1',
      workDate: DateTime(2026, 8, 24),
      checkInAtUtc: DateTime.utc(2026, 8, 24, 1),
      checkInLocationName: 'Văn phòng chính',
      checkOutAtUtc: null,
      checkOutLocationName: null,
    );
    return today!;
  }

  @override
  Future<AttendanceRecord> checkOut({required double latitude, required double longitude}) async {
    today = AttendanceRecord(
      id: today!.id,
      workDate: today!.workDate,
      checkInAtUtc: today!.checkInAtUtc,
      checkInLocationName: today!.checkInLocationName,
      checkOutAtUtc: DateTime.utc(2026, 8, 24, 9),
      checkOutLocationName: 'Văn phòng chính',
    );
    return today!;
  }

  @override
  Future<AttendanceRecord?> getToday() async => today;

  @override
  Future<List<AttendanceRecord>> getHistory({required int year, required int month}) async =>
      today == null ? [] : [today!];

  @override
  Future<AttendanceStats> getStats({required int year, required int month}) async =>
      const AttendanceStats(daysCheckedIn: 1, totalHoursWorked: 8);
}

ProviderContainer _buildContainer(_FakeAttendanceRepository repo) => ProviderContainer(
      overrides: [attendanceRepositoryProvider.overrideWithValue(repo)],
    );

void main() {
  test('todayAttendanceProvider starts null when nothing checked in', () async {
    final container = _buildContainer(_FakeAttendanceRepository());
    addTearDown(container.dispose);

    final result = await container.read(todayAttendanceProvider.future);

    expect(result, isNull);
  });

  test('checkIn updates todayAttendanceProvider state', () async {
    final repo = _FakeAttendanceRepository();
    final container = _buildContainer(repo);
    addTearDown(container.dispose);
    await container.read(todayAttendanceProvider.future);

    await container.read(todayAttendanceProvider.notifier).checkIn(latitude: 10, longitude: 106);

    final state = container.read(todayAttendanceProvider).value;
    expect(state?.isCheckedIn, isTrue);
    expect(state?.checkInLocationName, 'Văn phòng chính');
  });

  test('checkIn failure leaves state as error without crashing', () async {
    final repo = _FakeAttendanceRepository()..throwOnCheckIn = true;
    final container = _buildContainer(repo);
    addTearDown(container.dispose);
    await container.read(todayAttendanceProvider.future);

    await expectLater(
      container.read(todayAttendanceProvider.notifier).checkIn(latitude: 10, longitude: 106),
      throwsException,
    );
  });

  test('checkOut updates todayAttendanceProvider state', () async {
    final repo = _FakeAttendanceRepository();
    final container = _buildContainer(repo);
    addTearDown(container.dispose);
    await container.read(todayAttendanceProvider.notifier).checkIn(latitude: 10, longitude: 106);

    await container.read(todayAttendanceProvider.notifier).checkOut(latitude: 10, longitude: 106);

    final state = container.read(todayAttendanceProvider).value;
    expect(state?.isCheckedOut, isTrue);
  });

  test('attendanceStatsProvider reads stats for the requested month', () async {
    final container = _buildContainer(_FakeAttendanceRepository());
    addTearDown(container.dispose);

    final stats = await container.read(attendanceStatsProvider((year: 2026, month: 8)).future);

    expect(stats.daysCheckedIn, 1);
    expect(stats.totalHoursWorked, 8);
  });
}
