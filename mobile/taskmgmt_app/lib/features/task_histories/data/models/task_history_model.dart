import '../../domain/entities/task_history.dart';

class TaskHistoryModel {
  const TaskHistoryModel({
    required this.id,
    required this.workTaskId,
    required this.actionType,
    required this.description,
    required this.fieldName,
    required this.oldValue,
    required this.newValue,
    required this.actorUserId,
    required this.actorFullName,
    required this.actorEmail,
    required this.targetUserId,
    required this.targetUserFullName,
    required this.targetUserEmail,
    required this.createdAtUtc,
  });

  final String id;
  final String workTaskId;
  final String actionType;
  final String description;
  final String? fieldName;
  final String? oldValue;
  final String? newValue;
  final String? actorUserId;
  final String actorFullName;
  final String actorEmail;
  final String? targetUserId;
  final String? targetUserFullName;
  final String? targetUserEmail;
  final DateTime createdAtUtc;

  factory TaskHistoryModel.fromJson(Map<String, dynamic> json) => TaskHistoryModel(
        id: json['id'] as String,
        workTaskId: json['workTaskId'] as String,
        actionType: json['actionType'] as String,
        description: json['description'] as String,
        fieldName: json['fieldName'] as String?,
        oldValue: json['oldValue'] as String?,
        newValue: json['newValue'] as String?,
        actorUserId: json['actorUserId'] as String?,
        actorFullName: json['actorFullName'] as String,
        actorEmail: json['actorEmail'] as String,
        targetUserId: json['targetUserId'] as String?,
        targetUserFullName: json['targetUserFullName'] as String?,
        targetUserEmail: json['targetUserEmail'] as String?,
        createdAtUtc: DateTime.parse(json['createdAtUtc'] as String),
      );

  TaskHistory toDomain() => TaskHistory(
        id: id,
        workTaskId: workTaskId,
        actionType: taskHistoryActionTypeFromString(actionType),
        description: description,
        fieldName: fieldName,
        oldValue: oldValue,
        newValue: newValue,
        actorUserId: actorUserId,
        actorFullName: actorFullName,
        actorEmail: actorEmail,
        targetUserId: targetUserId,
        targetUserFullName: targetUserFullName,
        targetUserEmail: targetUserEmail,
        createdAtUtc: createdAtUtc,
      );
}
