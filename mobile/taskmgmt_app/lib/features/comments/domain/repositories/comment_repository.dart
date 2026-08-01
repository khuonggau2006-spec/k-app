import '../entities/comment.dart';

abstract class CommentRepository {
  Future<List<Comment>> getComments(String workTaskId);

  Future<Comment> addComment({
    required String workTaskId,
    required String content,
    required List<String> mentionedUserIds,
  });
}
