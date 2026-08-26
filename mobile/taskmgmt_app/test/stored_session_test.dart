import 'package:flutter_test/flutter_test.dart';

import 'package:taskmgmt_app/core/storage/stored_session.dart';

// Task 9's restoreSession() fast-path (access token vẫn còn hạn, không gọi mạng) hardcode
// hasAvatar: false vì StoredSession chưa lưu trường này. Fix: StoredSession giờ lưu hasAvatar
// nên fast-path đọc lại đúng giá trị đã cache thay vì luôn trả về false.
void main() {
  group('StoredSession hasAvatar persistence', () {
    test('toJson includes hasAvatar', () {
      final session = StoredSession(
        accessToken: 'access',
        accessTokenExpiresAtUtc: DateTime.utc(2026, 1, 1),
        refreshToken: 'refresh',
        userId: 'u1',
        userEmail: 'a@b.com',
        userFullName: 'A B',
        userSystemRole: 'Member',
        hasAvatar: true,
      );

      expect(session.toJson()['hasAvatar'], isTrue);
    });

    test('fromJson round-trips hasAvatar: true', () {
      final session = StoredSession(
        accessToken: 'access',
        accessTokenExpiresAtUtc: DateTime.utc(2026, 1, 1),
        refreshToken: 'refresh',
        userId: 'u1',
        userEmail: 'a@b.com',
        userFullName: 'A B',
        userSystemRole: 'Member',
        hasAvatar: true,
      );

      final restored = StoredSession.fromJson(session.toJson());

      expect(restored.hasAvatar, isTrue);
    });

    test('fromJson round-trips hasAvatar: false', () {
      final session = StoredSession(
        accessToken: 'access',
        accessTokenExpiresAtUtc: DateTime.utc(2026, 1, 1),
        refreshToken: 'refresh',
        userId: 'u1',
        userEmail: 'a@b.com',
        userFullName: 'A B',
        userSystemRole: 'Member',
        hasAvatar: false,
      );

      final restored = StoredSession.fromJson(session.toJson());

      expect(restored.hasAvatar, isFalse);
    });
  });
}
