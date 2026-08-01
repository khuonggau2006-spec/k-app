import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:taskmgmt_app/core/models/paged_result.dart';
import 'package:taskmgmt_app/features/attachments/domain/entities/attachment.dart';
import 'package:taskmgmt_app/features/attachments/domain/repositories/attachment_repository.dart';
import 'package:taskmgmt_app/features/attachments/presentation/providers/attachment_provider.dart';
import 'package:taskmgmt_app/features/auth/domain/entities/user.dart';
import 'package:taskmgmt_app/features/comments/domain/entities/comment.dart';
import 'package:taskmgmt_app/features/comments/domain/repositories/comment_repository.dart';
import 'package:taskmgmt_app/features/comments/presentation/providers/comment_provider.dart';
import 'package:taskmgmt_app/features/locations/domain/entities/location.dart';
import 'package:taskmgmt_app/features/locations/domain/repositories/location_repository.dart';
import 'package:taskmgmt_app/features/locations/presentation/providers/location_provider.dart';
import 'package:taskmgmt_app/features/task_assignees/domain/entities/task_assignee.dart';
import 'package:taskmgmt_app/features/task_assignees/domain/repositories/task_assignee_repository.dart';
import 'package:taskmgmt_app/features/task_assignees/presentation/providers/task_assignee_provider.dart';
import 'package:taskmgmt_app/features/task_histories/domain/entities/task_history.dart';
import 'package:taskmgmt_app/features/task_histories/domain/repositories/task_history_repository.dart';
import 'package:taskmgmt_app/features/task_histories/presentation/providers/task_history_provider.dart';
import 'package:taskmgmt_app/features/tasks/domain/entities/work_task.dart';
import 'package:taskmgmt_app/features/tasks/domain/repositories/work_task_repository.dart';
import 'package:taskmgmt_app/features/tasks/presentation/providers/work_task_provider.dart';
import 'package:taskmgmt_app/features/tasks/presentation/screens/work_task_detail_screen.dart';
import 'package:taskmgmt_app/features/users/domain/repositories/user_repository.dart';
import 'package:taskmgmt_app/features/users/presentation/providers/user_provider.dart';

const _taskId = 'task-1';

class _FakeWorkTaskRepository implements WorkTaskRepository {
  @override
  Future<WorkTask> getTaskById(String id) async => const WorkTask(
        id: _taskId,
        title: 'Công việc mẫu',
        description: 'Mô tả mẫu',
        status: WorkTaskStatus.toDo,
        dueDateUtc: null,
        parentTaskId: null,
        locationId: null,
      );

  @override
  Future<WorkTask> createTask({
    required String title,
    String? description,
    DateTime? dueDateUtc,
    String? locationId,
    String? parentTaskId,
  }) async =>
      throw UnimplementedError();

  @override
  Future<void> deleteTask(String id) async {}

  @override
  Future<PagedResult<WorkTask>> getTasks({WorkTaskStatus? status, String? locationId, String? parentTaskId}) async =>
      throw UnimplementedError();

  @override
  Future<WorkTask> updateTask({
    required String id,
    required String title,
    String? description,
    required WorkTaskStatus status,
    DateTime? dueDateUtc,
    String? locationId,
  }) async =>
      throw UnimplementedError();
}

class _FakeLocationRepository implements LocationRepository {
  @override
  Future<List<Location>> getLocations() async => [];
}

class _FakeTaskAssigneeRepository implements TaskAssigneeRepository {
  @override
  Future<List<TaskAssignee>> getAssignees(String workTaskId) async => [];

  @override
  Future<TaskAssignee> addAssignee({required String workTaskId, required String userId, required TaskAssigneeRole role}) =>
      throw UnimplementedError();

  @override
  Future<TaskAssignee> changeRole({required String workTaskId, required String userId, required TaskAssigneeRole role}) =>
      throw UnimplementedError();

  @override
  Future<void> removeAssignee({required String workTaskId, required String userId}) async {}
}

class _FakeUserRepository implements UserRepository {
  @override
  Future<List<User>> getUsers() async =>
      [const User(id: 'u1', email: 'nguoidung@example.com', fullName: 'Người Dùng A', systemRole: SystemRole.member)];
}

class _FakeCommentRepository implements CommentRepository {
  @override
  Future<List<Comment>> getComments(String workTaskId) async => [
        Comment(
          id: 'c1',
          workTaskId: workTaskId,
          content: 'Đây là một bình luận test.',
          authorUserId: 'u1',
          authorFullName: 'Người Dùng A',
          authorEmail: 'nguoidung@example.com',
          createdAtUtc: DateTime.utc(2026, 7, 31, 10),
          mentions: const [],
        ),
      ];

  @override
  Future<Comment> addComment({required String workTaskId, required String content, required List<String> mentionedUserIds}) =>
      throw UnimplementedError();
}

class _FakeAttachmentRepository implements AttachmentRepository {
  @override
  Future<List<Attachment>> getAttachments(String workTaskId) async => [
        Attachment(
          id: 'a1',
          workTaskId: workTaskId,
          fileName: 'tai-lieu.pdf',
          contentType: 'application/pdf',
          sizeBytes: 204800,
          uploadedByUserId: 'u1',
          uploadedByFullName: 'Người Dùng A',
          uploadedByEmail: 'nguoidung@example.com',
          createdAtUtc: DateTime.utc(2026, 7, 31, 9),
        ),
      ];

  @override
  Future<Uint8List> downloadAttachment({required String workTaskId, required String attachmentId}) =>
      throw UnimplementedError();

  @override
  Future<void> deleteAttachment({required String workTaskId, required String attachmentId}) async {}

  @override
  Future<Attachment> uploadAttachment({required String workTaskId, required String fileName, required Uint8List bytes}) =>
      throw UnimplementedError();
}

class _FakeTaskHistoryRepository implements TaskHistoryRepository {
  @override
  Future<List<TaskHistory>> getHistory(String workTaskId) async => [
        TaskHistory(
          id: 'h1',
          workTaskId: workTaskId,
          actionType: TaskHistoryActionType.created,
          description: 'Công việc được tạo.',
          fieldName: null,
          oldValue: null,
          newValue: null,
          actorUserId: 'u1',
          actorFullName: 'Người Dùng A',
          actorEmail: 'nguoidung@example.com',
          targetUserId: null,
          targetUserFullName: null,
          targetUserEmail: null,
          createdAtUtc: DateTime.utc(2026, 7, 31, 8),
        ),
      ];
}

Widget _buildScreen() => ProviderScope(
      overrides: [
        workTaskRepositoryProvider.overrideWithValue(_FakeWorkTaskRepository()),
        locationRepositoryProvider.overrideWithValue(_FakeLocationRepository()),
        taskAssigneeRepositoryProvider.overrideWithValue(_FakeTaskAssigneeRepository()),
        userRepositoryProvider.overrideWithValue(_FakeUserRepository()),
        commentRepositoryProvider.overrideWithValue(_FakeCommentRepository()),
        attachmentRepositoryProvider.overrideWithValue(_FakeAttachmentRepository()),
        taskHistoryRepositoryProvider.overrideWithValue(_FakeTaskHistoryRepository()),
      ],
      child: const MaterialApp(home: WorkTaskDetailScreen(taskId: _taskId)),
    );

void main() {
  testWidgets('Work task detail shows comments, attachments and history sections', (tester) async {
    // Màn hình dùng ListView lazy-build - phóng khổ viewport đủ cao để mọi section
    // (bình luận, tệp đính kèm, lịch sử) đều được build sẵn mà không cần cuộn thủ công.
    await tester.binding.setSurfaceSize(const Size(800, 3000));
    addTearDown(() => tester.binding.setSurfaceSize(null));

    await tester.pumpWidget(_buildScreen());
    await tester.pumpAndSettle();

    expect(find.text('Công việc mẫu'), findsOneWidget);

    expect(find.text('Bình luận'), findsOneWidget);
    expect(find.text('Đây là một bình luận test.'), findsOneWidget);

    expect(find.text('Tệp đính kèm'), findsOneWidget);
    expect(find.text('tai-lieu.pdf'), findsOneWidget);

    expect(find.text('Lịch sử thay đổi'), findsOneWidget);
    expect(find.text('Công việc được tạo.'), findsOneWidget);
  });
}
