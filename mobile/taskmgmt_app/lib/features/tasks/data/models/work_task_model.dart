import '../../domain/entities/work_task.dart';

class WorkTaskModel {
  const WorkTaskModel({
    required this.id,
    required this.title,
    required this.description,
    required this.status,
    required this.dueDateUtc,
    required this.isActive,
    required this.parentTaskId,
    required this.locationId,
  });

  final String id;
  final String title;
  final String? description;
  final String status;
  final DateTime? dueDateUtc;
  final bool isActive;
  final String? parentTaskId;
  final String? locationId;

  factory WorkTaskModel.fromJson(Map<String, dynamic> json) => WorkTaskModel(
        id: json['id'] as String,
        title: json['title'] as String,
        description: json['description'] as String?,
        status: json['status'] as String,
        dueDateUtc: json['dueDateUtc'] == null ? null : DateTime.parse(json['dueDateUtc'] as String),
        isActive: json['isActive'] as bool,
        parentTaskId: json['parentTaskId'] as String?,
        locationId: json['locationId'] as String?,
      );

  WorkTask toDomain() => WorkTask(
        id: id,
        title: title,
        description: description,
        status: workTaskStatusFromString(status),
        dueDateUtc: dueDateUtc,
        parentTaskId: parentTaskId,
        locationId: locationId,
      );
}
