import '../../domain/entities/weekly_completion.dart';

class WeeklyCompletionModel {
  const WeeklyCompletionModel({required this.weekStartDate, required this.completedCount});

  final DateTime weekStartDate;
  final int completedCount;

  factory WeeklyCompletionModel.fromJson(Map<String, dynamic> json) => WeeklyCompletionModel(
        weekStartDate: DateTime.parse(json['weekStartDate'] as String),
        completedCount: json['completedCount'] as int,
      );

  WeeklyCompletion toDomain() => WeeklyCompletion(weekStartDate: weekStartDate, completedCount: completedCount);
}
