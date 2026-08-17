class NotificationPreference {
  const NotificationPreference({required this.type, required this.isEnabled});

  final String type;
  final bool isEnabled;

  static const Map<String, String> _labels = {
    'FieldChanged': 'Thay đổi thông tin công việc',
    'StatusChanged': 'Đổi trạng thái công việc',
    'Deleted': 'Công việc bị xoá',
    'AssigneeAdded': 'Được thêm vào công việc',
    'AssigneeRemoved': 'Bị gỡ khỏi công việc',
    'AssigneeRoleChanged': 'Đổi vai trò trong công việc',
    'CommentAdded': 'Có bình luận mới',
    'AttachmentAdded': 'Có tệp đính kèm mới',
    'DueSoon': 'Sắp đến hạn',
    'Overdue': 'Quá hạn',
  };

  String get label => _labels[type] ?? type;

  NotificationPreference copyWith({bool? isEnabled}) =>
      NotificationPreference(type: type, isEnabled: isEnabled ?? this.isEnabled);
}
