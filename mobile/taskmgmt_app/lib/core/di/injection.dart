import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:get_it/get_it.dart';

import '../../features/auth/data/datasources/auth_remote_data_source.dart';
import '../../features/auth/data/repositories/auth_repository_impl.dart';
import '../../features/auth/domain/repositories/auth_repository.dart';
import '../../features/locations/data/datasources/location_remote_data_source.dart';
import '../../features/locations/data/repositories/location_repository_impl.dart';
import '../../features/locations/domain/repositories/location_repository.dart';
import '../../features/task_assignees/data/datasources/task_assignee_remote_data_source.dart';
import '../../features/task_assignees/data/repositories/task_assignee_repository_impl.dart';
import '../../features/task_assignees/domain/repositories/task_assignee_repository.dart';
import '../../features/tasks/data/datasources/work_task_remote_data_source.dart';
import '../../features/tasks/data/repositories/work_task_repository_impl.dart';
import '../../features/tasks/domain/repositories/work_task_repository.dart';
import '../../features/users/data/datasources/user_remote_data_source.dart';
import '../../features/users/data/repositories/user_repository_impl.dart';
import '../../features/users/domain/repositories/user_repository.dart';
import '../network/auth_event_bus.dart';
import '../network/dio_client.dart';
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
}
