import 'dart:typed_data';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/di/injection.dart';
import '../../domain/repositories/user_repository.dart';

final userRepositoryProvider = Provider<UserRepository>((ref) => getIt<UserRepository>());

final usersProvider = FutureProvider((ref) => ref.read(userRepositoryProvider).getUsers());

// Cache theo userId trong suốt phiên app - nhiều widget cùng hiện avatar của 1 người (assignee
// list, comment list) chỉ tải ảnh 1 lần nhờ Riverpod tự chia sẻ kết quả family theo tham số.
final avatarBytesProvider = FutureProvider.family<Uint8List?, String>((ref, userId) {
  return ref.read(userRepositoryProvider).downloadAvatar(userId);
});
