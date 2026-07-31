class StoredSession {
  const StoredSession({
    required this.accessToken,
    required this.accessTokenExpiresAtUtc,
    required this.refreshToken,
    required this.userId,
    required this.userEmail,
    required this.userFullName,
    required this.userSystemRole,
  });

  final String accessToken;
  final DateTime accessTokenExpiresAtUtc;
  final String refreshToken;
  final String userId;
  final String userEmail;
  final String userFullName;
  final String userSystemRole;

  Map<String, dynamic> toJson() => {
        'accessToken': accessToken,
        'accessTokenExpiresAtUtc': accessTokenExpiresAtUtc.toIso8601String(),
        'refreshToken': refreshToken,
        'userId': userId,
        'userEmail': userEmail,
        'userFullName': userFullName,
        'userSystemRole': userSystemRole,
      };

  factory StoredSession.fromJson(Map<String, dynamic> json) => StoredSession(
        accessToken: json['accessToken'] as String,
        accessTokenExpiresAtUtc: DateTime.parse(json['accessTokenExpiresAtUtc'] as String),
        refreshToken: json['refreshToken'] as String,
        userId: json['userId'] as String,
        userEmail: json['userEmail'] as String,
        userFullName: json['userFullName'] as String,
        userSystemRole: json['userSystemRole'] as String,
      );
}
