namespace TaskMgmt.Application.Common.Models;

public class FirebaseSettings
{
    public const string SectionName = "Firebase";

    // Đường dẫn tới file service-account JSON tải từ Firebase Console (KHÔNG commit vào git -
    // .gitignore đã có sẵn pattern firebase-service-account*.json). Để trống nếu chưa cấu hình
    // Firebase - tính năng push sẽ tự tắt thay vì làm sập ứng dụng.
    public string? CredentialsPath { get; set; }
}
