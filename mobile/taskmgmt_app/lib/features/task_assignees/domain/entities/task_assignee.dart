enum TaskAssigneeRole { owner, assignee, reviewer, watcher }

TaskAssigneeRole taskAssigneeRoleFromString(String value) => switch (value) {
      'Owner' => TaskAssigneeRole.owner,
      'Assignee' => TaskAssigneeRole.assignee,
      'Reviewer' => TaskAssigneeRole.reviewer,
      'Watcher' => TaskAssigneeRole.watcher,
      _ => TaskAssigneeRole.assignee,
    };

String taskAssigneeRoleToApiString(TaskAssigneeRole role) => switch (role) {
      TaskAssigneeRole.owner => 'Owner',
      TaskAssigneeRole.assignee => 'Assignee',
      TaskAssigneeRole.reviewer => 'Reviewer',
      TaskAssigneeRole.watcher => 'Watcher',
    };

String taskAssigneeRoleLabel(TaskAssigneeRole role) => switch (role) {
      TaskAssigneeRole.owner => 'Owner',
      TaskAssigneeRole.assignee => 'Người thực hiện',
      TaskAssigneeRole.reviewer => 'Người xem xét',
      TaskAssigneeRole.watcher => 'Người theo dõi',
    };

class TaskAssignee {
  const TaskAssignee({
    required this.id,
    required this.workTaskId,
    required this.userId,
    required this.userFullName,
    required this.userEmail,
    required this.role,
  });

  final String id;
  final String workTaskId;
  final String userId;
  final String userFullName;
  final String userEmail;
  final TaskAssigneeRole role;
}
