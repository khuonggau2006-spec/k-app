class AppNotification {
  const AppNotification({
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

  AppNotification copyWith({bool? isRead, DateTime? readAtUtc}) => AppNotification(
        id: id,
        title: title,
        body: body,
        type: type,
        workTaskId: workTaskId,
        isRead: isRead ?? this.isRead,
        readAtUtc: readAtUtc ?? this.readAtUtc,
        createdAtUtc: createdAtUtc,
      );
}
