import '../../domain/entities/comment.dart';

class CommentMentionModel {
  const CommentMentionModel({required this.userId, required this.fullName, required this.email});

  final String userId;
  final String fullName;
  final String email;

  factory CommentMentionModel.fromJson(Map<String, dynamic> json) => CommentMentionModel(
        userId: json['userId'] as String,
        fullName: json['fullName'] as String,
        email: json['email'] as String,
      );

  CommentMention toDomain() => CommentMention(userId: userId, fullName: fullName, email: email);
}

class CommentModel {
  const CommentModel({
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
  final List<CommentMentionModel> mentions;

  factory CommentModel.fromJson(Map<String, dynamic> json) => CommentModel(
        id: json['id'] as String,
        workTaskId: json['workTaskId'] as String,
        content: json['content'] as String,
        authorUserId: json['authorUserId'] as String?,
        authorFullName: json['authorFullName'] as String,
        authorEmail: json['authorEmail'] as String,
        authorHasAvatar: json['authorHasAvatar'] as bool,
        createdAtUtc: DateTime.parse(json['createdAtUtc'] as String),
        mentions: (json['mentions'] as List<dynamic>)
            .map((m) => CommentMentionModel.fromJson(m as Map<String, dynamic>))
            .toList(),
      );

  Comment toDomain() => Comment(
        id: id,
        workTaskId: workTaskId,
        content: content,
        authorUserId: authorUserId,
        authorFullName: authorFullName,
        authorEmail: authorEmail,
        authorHasAvatar: authorHasAvatar,
        createdAtUtc: createdAtUtc,
        mentions: mentions.map((m) => m.toDomain()).toList(),
      );
}
