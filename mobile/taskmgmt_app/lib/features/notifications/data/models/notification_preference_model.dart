import '../../domain/entities/notification_preference.dart';

class NotificationPreferenceModel {
  const NotificationPreferenceModel({required this.type, required this.isEnabled});

  final String type;
  final bool isEnabled;

  factory NotificationPreferenceModel.fromJson(Map<String, dynamic> json) => NotificationPreferenceModel(
        type: json['type'] as String,
        isEnabled: json['isEnabled'] as bool,
      );

  NotificationPreference toDomain() => NotificationPreference(type: type, isEnabled: isEnabled);
}
