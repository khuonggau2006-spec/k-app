import '../../domain/entities/user.dart';

class UserModel {
  const UserModel({
    required this.id,
    required this.email,
    required this.fullName,
    required this.systemRole,
  });

  final String id;
  final String email;
  final String fullName;
  final String systemRole;

  factory UserModel.fromJson(Map<String, dynamic> json) => UserModel(
        id: json['id'] as String,
        email: json['email'] as String,
        fullName: json['fullName'] as String,
        systemRole: json['systemRole'] as String,
      );

  User toDomain() => User(
        id: id,
        email: email,
        fullName: fullName,
        systemRole: systemRoleFromString(systemRole),
      );
}
