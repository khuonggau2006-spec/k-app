import '../../../../core/storage/stored_session.dart';
import '../../../../core/storage/token_storage.dart';
import '../../domain/entities/user.dart';
import '../../domain/repositories/auth_repository.dart';
import '../datasources/auth_remote_data_source.dart';
import '../models/auth_result_model.dart';

class AuthRepositoryImpl implements AuthRepository {
  AuthRepositoryImpl(this._remoteDataSource, this._tokenStorage);

  final AuthRemoteDataSource _remoteDataSource;
  final TokenStorage _tokenStorage;

  @override
  Future<User?> restoreSession() async {
    final session = await _tokenStorage.readSession();
    if (session == null) return null;

    if (session.accessTokenExpiresAtUtc.isAfter(DateTime.now().toUtc())) {
      return User(
        id: session.userId,
        email: session.userEmail,
        fullName: session.userFullName,
        systemRole: systemRoleFromString(session.userSystemRole),
      );
    }

    try {
      final result = await _remoteDataSource.refreshToken(session.refreshToken);
      await _persist(result);
      return result.user.toDomain();
    } catch (_) {
      await _tokenStorage.clear();
      return null;
    }
  }

  @override
  Future<User> login({required String email, required String password}) async {
    final result = await _remoteDataSource.login(email: email, password: password);
    await _persist(result);
    return result.user.toDomain();
  }

  @override
  Future<User> register({required String email, required String fullName, required String password}) async {
    final result = await _remoteDataSource.register(email: email, fullName: fullName, password: password);
    await _persist(result);
    return result.user.toDomain();
  }

  @override
  Future<void> logout() async {
    final session = await _tokenStorage.readSession();
    if (session != null) {
      // Thu hồi refresh token trên server để chặn dùng lại sau khi đăng xuất; nếu mạng lỗi
      // thì vẫn cứ xoá token cục bộ - không để người dùng bị kẹt không đăng xuất được.
      try {
        await _remoteDataSource.logout(session.refreshToken);
      } catch (_) {}
    }
    await _tokenStorage.clear();
  }

  Future<void> _persist(AuthResultModel result) => _tokenStorage.saveSession(
        StoredSession(
          accessToken: result.accessToken,
          accessTokenExpiresAtUtc: result.accessTokenExpiresAtUtc,
          refreshToken: result.refreshToken,
          userId: result.user.id,
          userEmail: result.user.email,
          userFullName: result.user.fullName,
          userSystemRole: result.user.systemRole,
        ),
      );
}
