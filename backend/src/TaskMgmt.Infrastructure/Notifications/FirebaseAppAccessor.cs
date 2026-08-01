using FirebaseAdmin;

namespace TaskMgmt.Infrastructure.Notifications;

// Bọc FirebaseApp? (có thể null nếu chưa cấu hình credentials) thành một service Singleton
// hợp lệ - DI không cho đăng ký thẳng một instance null cho kiểu tham chiếu.
public class FirebaseAppAccessor(FirebaseApp? app)
{
    public FirebaseApp? App { get; } = app;
}
