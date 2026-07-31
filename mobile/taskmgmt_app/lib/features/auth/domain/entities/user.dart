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
  });

  final String id;
  final String email;
  final String fullName;
  final SystemRole systemRole;
}
