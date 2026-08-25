class AttendanceRecord {
  const AttendanceRecord({
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

  bool get isCheckedIn => checkInAtUtc != null;
  bool get isCheckedOut => checkOutAtUtc != null;
}
