import '../../domain/entities/task_assignee.dart';

class TaskAssigneeModel {
  const TaskAssigneeModel({
    required this.id,
    required this.workTaskId,
    required this.userId,
    required this.userFullName,
    required this.userEmail,
    required this.userHasAvatar,
    required this.role,
  });

  final String id;
  final String workTaskId;
  final String userId;
  final String userFullName;
  final String userEmail;
  final bool userHasAvatar;
  final String role;

  factory TaskAssigneeModel.fromJson(Map<String, dynamic> json) => TaskAssigneeModel(
        id: json['id'] as String,
        workTaskId: json['workTaskId'] as String,
        userId: json['userId'] as String,
        userFullName: json['userFullName'] as String,
        userEmail: json['userEmail'] as String,
        userHasAvatar: json['userHasAvatar'] as bool,
        role: json['role'] as String,
      );

  TaskAssignee toDomain() => TaskAssignee(
        id: id,
        workTaskId: workTaskId,
        userId: userId,
        userFullName: userFullName,
        userEmail: userEmail,
        userHasAvatar: userHasAvatar,
        role: taskAssigneeRoleFromString(role),
      );
}
