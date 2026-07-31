import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:taskmgmt_app/app.dart';
import 'package:taskmgmt_app/core/models/paged_result.dart';
import 'package:taskmgmt_app/core/network/api_exception.dart';
import 'package:taskmgmt_app/features/auth/domain/entities/user.dart';
import 'package:taskmgmt_app/features/auth/domain/repositories/auth_repository.dart';
import 'package:taskmgmt_app/features/auth/presentation/providers/auth_provider.dart';
import 'package:taskmgmt_app/features/locations/domain/entities/location.dart';
import 'package:taskmgmt_app/features/locations/domain/repositories/location_repository.dart';
import 'package:taskmgmt_app/features/locations/presentation/providers/location_provider.dart';
import 'package:taskmgmt_app/features/tasks/domain/entities/work_task.dart';
import 'package:taskmgmt_app/features/tasks/domain/repositories/work_task_repository.dart';
import 'package:taskmgmt_app/features/tasks/presentation/providers/work_task_provider.dart';

class _FakeAuthRepository implements AuthRepository {
  @override
  Future<User?> restoreSession() async => null;

  @override
  Future<User> login({required String email, required String password}) async {
    if (password != 'correct-password') {
      throw const ApiException('Email hoặc mật khẩu không đúng.');
    }
    return User(id: '1', email: email, fullName: 'Test User', systemRole: SystemRole.member);
  }

  @override
  Future<User> register({required String email, required String fullName, required String password}) async =>
      User(id: '1', email: email, fullName: fullName, systemRole: SystemRole.member);

  @override
  Future<void> logout() async {}
}

class _FakeLocationRepository implements LocationRepository {
  @override
  Future<List<Location>> getLocations() async => [];
}

class _FakeWorkTaskRepository implements WorkTaskRepository {
  @override
  Future<PagedResult<WorkTask>> getTasks({WorkTaskStatus? status, String? locationId, String? parentTaskId}) async =>
      const PagedResult(items: [], pageNumber: 1, pageSize: 50, totalCount: 0, totalPages: 0);

  @override
  Future<WorkTask> getTaskById(String id) async => throw UnimplementedError();

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
  Future<WorkTask> updateTask({
    required String id,
    required String title,
    String? description,
    required WorkTaskStatus status,
    DateTime? dueDateUtc,
    String? locationId,
  }) async =>
      throw UnimplementedError();

  @override
  Future<void> deleteTask(String id) async {}
}

Widget _buildApp() => ProviderScope(
      overrides: [
        authRepositoryProvider.overrideWithValue(_FakeAuthRepository()),
        locationRepositoryProvider.overrideWithValue(_FakeLocationRepository()),
        workTaskRepositoryProvider.overrideWithValue(_FakeWorkTaskRepository()),
      ],
      child: const TaskMgmtApp(),
    );

void main() {
  testWidgets('Login screen shows validation errors on empty submit', (tester) async {
    await tester.pumpWidget(_buildApp());
    await tester.pumpAndSettle();

    expect(find.text('TaskMgmt'), findsOneWidget);

    await tester.tap(find.widgetWithText(FilledButton, 'Đăng nhập'));
    await tester.pumpAndSettle();

    expect(find.text('Vui lòng nhập email.'), findsOneWidget);
    expect(find.text('Vui lòng nhập mật khẩu.'), findsOneWidget);
  });

  testWidgets('Login with wrong password shows error message', (tester) async {
    await tester.pumpWidget(_buildApp());
    await tester.pumpAndSettle();

    await tester.enterText(find.widgetWithText(TextFormField, 'Email'), 'user@example.com');
    await tester.enterText(find.widgetWithText(TextFormField, 'Mật khẩu'), 'wrong-password');
    await tester.tap(find.widgetWithText(FilledButton, 'Đăng nhập'));
    await tester.pumpAndSettle();

    expect(find.textContaining('không đúng'), findsOneWidget);
    expect(find.text('Công việc'), findsNothing);
  });

  testWidgets('Login with correct credentials navigates to task list', (tester) async {
    await tester.pumpWidget(_buildApp());
    await tester.pumpAndSettle();

    await tester.enterText(find.widgetWithText(TextFormField, 'Email'), 'user@example.com');
    await tester.enterText(find.widgetWithText(TextFormField, 'Mật khẩu'), 'correct-password');
    await tester.tap(find.widgetWithText(FilledButton, 'Đăng nhập'));
    await tester.pumpAndSettle();

    expect(find.text('Công việc'), findsOneWidget);
  });

  testWidgets('Register link navigates to register screen', (tester) async {
    await tester.pumpWidget(_buildApp());
    await tester.pumpAndSettle();

    await tester.tap(find.text('Chưa có tài khoản? Đăng ký ngay'));
    await tester.pumpAndSettle();

    expect(find.text('Tạo tài khoản mới'), findsOneWidget);
  });
}
