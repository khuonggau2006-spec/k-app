import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:get_it/get_it.dart';

import '../../features/attachments/data/datasources/attachment_remote_data_source.dart';
import '../../features/attachments/data/repositories/attachment_repository_impl.dart';
import '../../features/attachments/domain/repositories/attachment_repository.dart';
import '../../features/attendance/data/datasources/attendance_remote_data_source.dart';
import '../../features/attendance/data/repositories/attendance_repository_impl.dart';
import '../../features/attendance/domain/repositories/attendance_repository.dart';
import '../../features/auth/data/datasources/auth_remote_data_source.dart';
import '../../features/auth/data/repositories/auth_repository_impl.dart';
import '../../features/auth/domain/repositories/auth_repository.dart';
import '../../features/comments/data/datasources/comment_remote_data_source.dart';
import '../../features/comments/data/repositories/comment_repository_impl.dart';
import '../../features/comments/domain/repositories/comment_repository.dart';
import '../../features/dashboard/data/datasources/dashboard_remote_data_source.dart';
import '../../features/dashboard/data/repositories/dashboard_repository_impl.dart';
import '../../features/dashboard/domain/repositories/dashboard_repository.dart';
import '../../features/locations/data/datasources/location_remote_data_source.dart';
import '../../features/locations/data/repositories/location_repository_impl.dart';
import '../../features/locations/domain/repositories/location_repository.dart';
import '../../features/notifications/data/datasources/device_token_remote_data_source.dart';
import '../../features/notifications/data/datasources/notification_remote_data_source.dart';
import '../../features/notifications/data/repositories/notification_repository_impl.dart';
import '../../features/notifications/domain/repositories/notification_repository.dart';
import '../../features/task_assignees/data/datasources/task_assignee_remote_data_source.dart';
import '../../features/task_assignees/data/repositories/task_assignee_repository_impl.dart';
import '../../features/task_assignees/domain/repositories/task_assignee_repository.dart';
import '../../features/task_histories/data/datasources/task_history_remote_data_source.dart';
import '../../features/task_histories/data/repositories/task_history_repository_impl.dart';
import '../../features/task_histories/domain/repositories/task_history_repository.dart';
import '../../features/tasks/data/datasources/work_task_remote_data_source.dart';
import '../../features/tasks/data/repositories/work_task_repository_impl.dart';
import '../../features/tasks/domain/repositories/work_task_repository.dart';
import '../../features/users/data/datasources/user_remote_data_source.dart';
import '../../features/users/data/repositories/user_repository_impl.dart';
import '../../features/users/domain/repositories/user_repository.dart';
import '../network/auth_event_bus.dart';
import '../network/dio_client.dart';
import '../realtime/realtime_service.dart';
import '../storage/token_storage.dart';

final getIt = GetIt.instance;

void setupLocator() {
  getIt.registerLazySingleton<FlutterSecureStorage>(() => const FlutterSecureStorage());
  getIt.registerLazySingleton<TokenStorage>(() => TokenStorage(getIt()));
  getIt.registerLazySingleton<AuthEventBus>(() => AuthEventBus());
  getIt.registerLazySingleton<Dio>(() => createDioClient(getIt(), getIt()));

  getIt.registerLazySingleton<AuthRemoteDataSource>(() => AuthRemoteDataSource(getIt()));
  getIt.registerLazySingleton<AuthRepository>(() => AuthRepositoryImpl(getIt(), getIt()));

  getIt.registerLazySingleton<LocationRemoteDataSource>(() => LocationRemoteDataSource(getIt()));
  getIt.registerLazySingleton<LocationRepository>(() => LocationRepositoryImpl(getIt()));

  getIt.registerLazySingleton<WorkTaskRemoteDataSource>(() => WorkTaskRemoteDataSource(getIt()));
  getIt.registerLazySingleton<WorkTaskRepository>(() => WorkTaskRepositoryImpl(getIt()));

  getIt.registerLazySingleton<UserRemoteDataSource>(() => UserRemoteDataSource(getIt()));
  getIt.registerLazySingleton<UserRepository>(() => UserRepositoryImpl(getIt()));

  getIt.registerLazySingleton<TaskAssigneeRemoteDataSource>(() => TaskAssigneeRemoteDataSource(getIt()));
  getIt.registerLazySingleton<TaskAssigneeRepository>(() => TaskAssigneeRepositoryImpl(getIt()));

  getIt.registerLazySingleton<CommentRemoteDataSource>(() => CommentRemoteDataSource(getIt()));
  getIt.registerLazySingleton<CommentRepository>(() => CommentRepositoryImpl(getIt()));

  getIt.registerLazySingleton<AttachmentRemoteDataSource>(() => AttachmentRemoteDataSource(getIt()));
  getIt.registerLazySingleton<AttachmentRepository>(() => AttachmentRepositoryImpl(getIt()));

  getIt.registerLazySingleton<AttendanceRemoteDataSource>(() => AttendanceRemoteDataSource(getIt()));
  getIt.registerLazySingleton<AttendanceRepository>(() => AttendanceRepositoryImpl(getIt()));

  getIt.registerLazySingleton<TaskHistoryRemoteDataSource>(() => TaskHistoryRemoteDataSource(getIt()));
  getIt.registerLazySingleton<TaskHistoryRepository>(() => TaskHistoryRepositoryImpl(getIt()));

  getIt.registerLazySingleton<DeviceTokenRemoteDataSource>(() => DeviceTokenRemoteDataSource(getIt()));

  getIt.registerLazySingleton<NotificationRemoteDataSource>(() => NotificationRemoteDataSource(getIt()));
  getIt.registerLazySingleton<NotificationRepository>(() => NotificationRepositoryImpl(getIt()));

  getIt.registerLazySingleton<DashboardRemoteDataSource>(() => DashboardRemoteDataSource(getIt()));
  getIt.registerLazySingleton<DashboardRepository>(() => DashboardRepositoryImpl(getIt()));

  getIt.registerLazySingleton<RealtimeService>(() => RealtimeService(getIt()));
}
