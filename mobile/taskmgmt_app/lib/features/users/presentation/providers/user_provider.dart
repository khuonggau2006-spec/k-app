import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/di/injection.dart';
import '../../domain/repositories/user_repository.dart';

final userRepositoryProvider = Provider<UserRepository>((ref) => getIt<UserRepository>());

final usersProvider = FutureProvider((ref) => ref.read(userRepositoryProvider).getUsers());
