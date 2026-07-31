import '../entities/user.dart';

abstract class AuthRepository {
  /// Trả về user nếu có phiên đăng nhập hợp lệ đã lưu (tự refresh nếu access
  /// token hết hạn), null nếu chưa đăng nhập hoặc phiên không còn hợp lệ.
  Future<User?> restoreSession();

  Future<User> login({required String email, required String password});

  Future<User> register({required String email, required String fullName, required String password});

  Future<void> logout();
}
