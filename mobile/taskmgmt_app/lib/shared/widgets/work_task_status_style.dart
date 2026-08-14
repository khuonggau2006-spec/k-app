import 'package:flutter/material.dart';

import '../../features/tasks/domain/entities/work_task.dart';

/// Màu đại diện cho từng trạng thái công việc - dùng chung giữa danh sách công việc
/// (chấm tròn trạng thái) và dashboard (thẻ thống kê, cột Kanban) để nhất quán.
Color workTaskStatusColor(WorkTaskStatus status) => switch (status) {
      WorkTaskStatus.toDo => Colors.grey,
      WorkTaskStatus.inProgress => Colors.blue,
      WorkTaskStatus.inReview => Colors.orange,
      WorkTaskStatus.done => Colors.green,
      WorkTaskStatus.cancelled => Colors.red,
    };
