enum TaskHistoryActionType {
  created,
  fieldChanged,
  statusChanged,
  deleted,
  assigneeAdded,
  assigneeRemoved,
  assigneeRoleChanged,
  commentAdded,
  attachmentAdded,
  attachmentRemoved,
}

TaskHistoryActionType taskHistoryActionTypeFromString(String value) => switch (value) {
      'Created' => TaskHistoryActionType.created,
      'FieldChanged' => TaskHistoryActionType.fieldChanged,
      'StatusChanged' => TaskHistoryActionType.statusChanged,
      'Deleted' => TaskHistoryActionType.deleted,
      'AssigneeAdded' => TaskHistoryActionType.assigneeAdded,
      'AssigneeRemoved' => TaskHistoryActionType.assigneeRemoved,
      'AssigneeRoleChanged' => TaskHistoryActionType.assigneeRoleChanged,
      'CommentAdded' => TaskHistoryActionType.commentAdded,
      'AttachmentAdded' => TaskHistoryActionType.attachmentAdded,
      'AttachmentRemoved' => TaskHistoryActionType.attachmentRemoved,
      _ => TaskHistoryActionType.fieldChanged,
    };

class TaskHistory {
  const TaskHistory({
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
  final TaskHistoryActionType actionType;
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
}
