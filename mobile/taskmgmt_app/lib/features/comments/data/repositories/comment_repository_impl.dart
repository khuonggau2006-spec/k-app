import '../../domain/entities/comment.dart';
import '../../domain/repositories/comment_repository.dart';
import '../datasources/comment_remote_data_source.dart';

class CommentRepositoryImpl implements CommentRepository {
  CommentRepositoryImpl(this._remoteDataSource);

  final CommentRemoteDataSource _remoteDataSource;

  @override
  Future<List<Comment>> getComments(String workTaskId) async {
    final models = await _remoteDataSource.getComments(workTaskId);
    return models.map((model) => model.toDomain()).toList();
  }

  @override
  Future<Comment> addComment({
    required String workTaskId,
    required String content,
    required List<String> mentionedUserIds,
  }) async {
    final model = await _remoteDataSource.addComment(
      workTaskId: workTaskId,
      content: content,
      mentionedUserIds: mentionedUserIds,
    );
    return model.toDomain();
  }
}
