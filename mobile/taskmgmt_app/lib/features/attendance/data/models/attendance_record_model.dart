import '../../domain/entities/attendance_record.dart';

class AttendanceRecordModel {
  const AttendanceRecordModel({
    required this.id,
    required this.workDate,
    required this.checkInAtUtc,
    required this.checkInLocationName,
    required this.checkOutAtUtc,
    required this.checkOutLocationName,
  });

  final String id;
  final DateTime workDate;
  final DateTime? checkInAtUtc;
  final String? checkInLocationName;
  final DateTime? checkOutAtUtc;
  final String? checkOutLocationName;

  factory AttendanceRecordModel.fromJson(Map<String, dynamic> json) => AttendanceRecordModel(
        id: json['id'] as String,
        workDate: DateTime.parse(json['workDate'] as String),
        checkInAtUtc: json['checkInAtUtc'] == null ? null : DateTime.parse(json['checkInAtUtc'] as String),
        checkInLocationName: json['checkInLocationName'] as String?,
        checkOutAtUtc: json['checkOutAtUtc'] == null ? null : DateTime.parse(json['checkOutAtUtc'] as String),
        checkOutLocationName: json['checkOutLocationName'] as String?,
      );

  AttendanceRecord toDomain() => AttendanceRecord(
        id: id,
        workDate: workDate,
        checkInAtUtc: checkInAtUtc,
        checkInLocationName: checkInLocationName,
        checkOutAtUtc: checkOutAtUtc,
        checkOutLocationName: checkOutLocationName,
      );
}
