enum SystemRole { admin, manager, member }

SystemRole systemRoleFromString(String value) => switch (value) {
      'Admin' => SystemRole.admin,
      'Manager' => SystemRole.manager,
      _ => SystemRole.member,
    };

class User {
  const User({
    required this.id,
    required this.email,
    required this.fullName,
    required this.systemRole,
    required this.hasAvatar,
  });

  final String id;
  final String email;
  final String fullName;
  final SystemRole systemRole;
  final bool hasAvatar;

  @override
  bool operator ==(Object other) =>
      other is User &&
      other.id == id &&
      other.email == email &&
      other.fullName == fullName &&
      other.systemRole == systemRole &&
      other.hasAvatar == hasAvatar;

  @override
  int get hashCode => Object.hash(id, email, fullName, systemRole, hasAvatar);
}
