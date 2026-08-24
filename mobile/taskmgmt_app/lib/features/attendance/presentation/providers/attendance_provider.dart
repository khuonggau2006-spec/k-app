import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/di/injection.dart';
import '../../domain/entities/attendance_record.dart';
import '../../domain/entities/attendance_stats.dart';
import '../../domain/repositories/attendance_repository.dart';

final attendanceRepositoryProvider = Provider<AttendanceRepository>((ref) => getIt<AttendanceRepository>());

final todayAttendanceProvider =
    AsyncNotifierProvider<TodayAttendanceController, AttendanceRecord?>(TodayAttendanceController.new);

class TodayAttendanceController extends AsyncNotifier<AttendanceRecord?> {
  @override
  Future<AttendanceRecord?> build() {
    return ref.read(attendanceRepositoryProvider).getToday();
  }

  Future<void> checkIn({required double latitude, required double longitude}) async {
    state = await AsyncValue.guard(
      () => ref.read(attendanceRepositoryProvider).checkIn(latitude: latitude, longitude: longitude),
    );
    if (state.hasError) {
      throw state.error!;
    }
  }

  Future<void> checkOut({required double latitude, required double longitude}) async {
    state = await AsyncValue.guard(
      () => ref.read(attendanceRepositoryProvider).checkOut(latitude: latitude, longitude: longitude),
    );
    if (state.hasError) {
      throw state.error!;
    }
  }
}

typedef YearMonth = ({int year, int month});

final attendanceHistoryProvider =
    AsyncNotifierProviderFamily<AttendanceHistoryController, List<AttendanceRecord>, YearMonth>(
        AttendanceHistoryController.new);

class AttendanceHistoryController extends FamilyAsyncNotifier<List<AttendanceRecord>, YearMonth> {
  @override
  Future<List<AttendanceRecord>> build(YearMonth arg) {
    return ref.read(attendanceRepositoryProvider).getHistory(year: arg.year, month: arg.month);
  }
}

final attendanceStatsProvider =
    AsyncNotifierProviderFamily<AttendanceStatsController, AttendanceStats, YearMonth>(
        AttendanceStatsController.new);

class AttendanceStatsController extends FamilyAsyncNotifier<AttendanceStats, YearMonth> {
  @override
  Future<AttendanceStats> build(YearMonth arg) {
    return ref.read(attendanceRepositoryProvider).getStats(year: arg.year, month: arg.month);
  }
}
