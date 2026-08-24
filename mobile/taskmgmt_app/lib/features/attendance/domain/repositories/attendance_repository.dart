import '../entities/attendance_record.dart';
import '../entities/attendance_stats.dart';

abstract class AttendanceRepository {
  Future<AttendanceRecord> checkIn({required double latitude, required double longitude});

  Future<AttendanceRecord> checkOut({required double latitude, required double longitude});

  Future<AttendanceRecord?> getToday();

  Future<List<AttendanceRecord>> getHistory({required int year, required int month});

  Future<AttendanceStats> getStats({required int year, required int month});
}
