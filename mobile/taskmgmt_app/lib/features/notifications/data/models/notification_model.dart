import '../../domain/entities/notification.dart';

class NotificationModel {
  const NotificationModel({
    required this.id,
    required this.title,
    required this.body,
    required this.type,
    required this.workTaskId,
    required this.isRead,
    required this.readAtUtc,
    required this.createdAtUtc,
  });

  final String id;
  final String title;
  final String body;
  final String type;
  final String? workTaskId;
  final bool isRead;
  final DateTime? readAtUtc;
  final DateTime createdAtUtc;

  factory NotificationModel.fromJson(Map<String, dynamic> json) => NotificationModel(
        id: json['id'] as String,
        title: json['title'] as String,
        body: json['body'] as String,
        type: json['type'] as String,
        workTaskId: json['workTaskId'] as String?,
        isRead: json['isRead'] as bool,
        readAtUtc: json['readAtUtc'] == null ? null : DateTime.parse(json['readAtUtc'] as String),
        createdAtUtc: DateTime.parse(json['createdAtUtc'] as String),
      );

  AppNotification toDomain() => AppNotification(
        id: id,
        title: title,
        body: body,
        type: type,
        workTaskId: workTaskId,
        isRead: isRead,
        readAtUtc: readAtUtc,
        createdAtUtc: createdAtUtc,
      );
}
