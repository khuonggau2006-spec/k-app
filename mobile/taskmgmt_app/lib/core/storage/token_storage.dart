import 'dart:convert';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import 'stored_session.dart';

class TokenStorage {
  TokenStorage(this._storage);

  final FlutterSecureStorage _storage;
  static const _sessionKey = 'auth_session';

  Future<void> saveSession(StoredSession session) =>
      _storage.write(key: _sessionKey, value: jsonEncode(session.toJson()));

  Future<StoredSession?> readSession() async {
    final raw = await _storage.read(key: _sessionKey);
    if (raw == null) return null;

    try {
      return StoredSession.fromJson(jsonDecode(raw) as Map<String, dynamic>);
    } catch (_) {
      return null;
    }
  }

  Future<void> clear() => _storage.delete(key: _sessionKey);
}
