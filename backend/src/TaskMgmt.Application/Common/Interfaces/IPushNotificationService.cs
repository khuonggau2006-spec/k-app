namespace TaskMgmt.Application.Common.Interfaces;

public record PushSendResult(int SuccessCount, int FailureCount);

public interface IPushNotificationService
{
    // Gửi tới toàn bộ thiết bị đã đăng ký của 1 user. Tự bỏ qua (trả về 0/0) nếu Firebase
    // chưa được cấu hình hoặc user không có device token nào.
    Task<PushSendResult> SendToUserAsync(
        Guid userId,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data,
        CancellationToken cancellationToken);
}
