import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/di/injection.dart';
import '../../domain/entities/comment.dart';
import '../../domain/repositories/comment_repository.dart';

final commentRepositoryProvider = Provider<CommentRepository>((ref) => getIt<CommentRepository>());

final commentsProvider = AsyncNotifierProvider.family<CommentsController, List<Comment>, String>(CommentsController.new);

class CommentsController extends FamilyAsyncNotifier<List<Comment>, String> {
  @override
  Future<List<Comment>> build(String workTaskId) {
    return ref.read(commentRepositoryProvider).getComments(workTaskId);
  }

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() => ref.read(commentRepositoryProvider).getComments(arg));
  }

  Future<void> addComment({required String content, required List<String> mentionedUserIds}) async {
    await ref.read(commentRepositoryProvider).addComment(
          workTaskId: arg,
          content: content,
          mentionedUserIds: mentionedUserIds,
        );
    await refresh();
  }
}
