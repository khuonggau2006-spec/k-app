enum WorkTaskStatus { toDo, inProgress, inReview, done, cancelled }

WorkTaskStatus workTaskStatusFromString(String value) => switch (value) {
      'ToDo' => WorkTaskStatus.toDo,
      'InProgress' => WorkTaskStatus.inProgress,
      'InReview' => WorkTaskStatus.inReview,
      'Done' => WorkTaskStatus.done,
      'Cancelled' => WorkTaskStatus.cancelled,
      _ => WorkTaskStatus.toDo,
    };

String workTaskStatusToApiString(WorkTaskStatus status) => switch (status) {
      WorkTaskStatus.toDo => 'ToDo',
      WorkTaskStatus.inProgress => 'InProgress',
      WorkTaskStatus.inReview => 'InReview',
      WorkTaskStatus.done => 'Done',
      WorkTaskStatus.cancelled => 'Cancelled',
    };

String workTaskStatusLabel(WorkTaskStatus status) => switch (status) {
      WorkTaskStatus.toDo => 'Cần làm',
      WorkTaskStatus.inProgress => 'Đang làm',
      WorkTaskStatus.inReview => 'Đang xem xét',
      WorkTaskStatus.done => 'Hoàn thành',
      WorkTaskStatus.cancelled => 'Đã huỷ',
    };

class WorkTask {
  const WorkTask({
    required this.id,
    required this.title,
    required this.description,
    required this.status,
    required this.dueDateUtc,
    required this.parentTaskId,
    required this.locationId,
  });

  final String id;
  final String title;
  final String? description;
  final WorkTaskStatus status;
  final DateTime? dueDateUtc;
  final String? parentTaskId;
  final String? locationId;

  bool get isOverdue =>
      dueDateUtc != null &&
      dueDateUtc!.isBefore(DateTime.now().toUtc()) &&
      status != WorkTaskStatus.done &&
      status != WorkTaskStatus.cancelled;
}
