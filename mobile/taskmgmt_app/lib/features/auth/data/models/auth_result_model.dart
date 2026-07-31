import 'user_model.dart';

class AuthResultModel {
  const AuthResultModel({
    required this.accessToken,
    required this.accessTokenExpiresAtUtc,
    required this.refreshToken,
    required this.refreshTokenExpiresAtUtc,
    required this.user,
  });

  final String accessToken;
  final DateTime accessTokenExpiresAtUtc;
  final String refreshToken;
  final DateTime refreshTokenExpiresAtUtc;
  final UserModel user;

  factory AuthResultModel.fromJson(Map<String, dynamic> json) => AuthResultModel(
        accessToken: json['accessToken'] as String,
        accessTokenExpiresAtUtc: DateTime.parse(json['accessTokenExpiresAtUtc'] as String),
        refreshToken: json['refreshToken'] as String,
        refreshTokenExpiresAtUtc: DateTime.parse(json['refreshTokenExpiresAtUtc'] as String),
        user: UserModel.fromJson(json['user'] as Map<String, dynamic>),
      );
}
