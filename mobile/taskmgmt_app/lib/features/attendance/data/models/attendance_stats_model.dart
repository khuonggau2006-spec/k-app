import '../../domain/entities/attendance_stats.dart';

class AttendanceStatsModel {
  const AttendanceStatsModel({required this.daysCheckedIn, required this.totalHoursWorked});

  final int daysCheckedIn;
  final double totalHoursWorked;

  factory AttendanceStatsModel.fromJson(Map<String, dynamic> json) => AttendanceStatsModel(
        daysCheckedIn: json['daysCheckedIn'] as int,
        totalHoursWorked: (json['totalHoursWorked'] as num).toDouble(),
      );

  AttendanceStats toDomain() => AttendanceStats(daysCheckedIn: daysCheckedIn, totalHoursWorked: totalHoursWorked);
}
