class TaskUpdatedEvent {
  const TaskUpdatedEvent({required this.workTaskId, required this.actionType, required this.occurredAtUtc});

  final String workTaskId;
  final String actionType;
  final DateTime occurredAtUtc;

  factory TaskUpdatedEvent.fromJson(Map<String, dynamic> json) => TaskUpdatedEvent(
        workTaskId: json['workTaskId'] as String,
        actionType: json['actionType'] as String,
        occurredAtUtc: DateTime.parse(json['occurredAtUtc'] as String),
      );
}
