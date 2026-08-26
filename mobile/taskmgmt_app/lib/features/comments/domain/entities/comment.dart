class CommentMention {
  const CommentMention({required this.userId, required this.fullName, required this.email});

  final String userId;
  final String fullName;
  final String email;
}

class Comment {
  const Comment({
    required this.id,
    required this.workTaskId,
    required this.content,
    required this.authorUserId,
    required this.authorFullName,
    required this.authorEmail,
    required this.authorHasAvatar,
    required this.createdAtUtc,
    required this.mentions,
  });

  final String id;
  final String workTaskId;
  final String content;
  final String? authorUserId;
  final String authorFullName;
  final String authorEmail;
  final bool authorHasAvatar;
  final DateTime createdAtUtc;
  final List<CommentMention> mentions;
}
