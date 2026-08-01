namespace TaskMgmt.Application.Common.Interfaces;

// Trừu tượng hoá cache-aside khỏi Redis cụ thể, cùng tinh thần với IRealtimeNotifier/IPushNotificationService.
// RemoveByPrefixAsync dùng để xoá cả nhóm key danh sách (nhiều biến thể filter/paging) chỉ bằng 1 lời gọi.
public interface ICacheService
{
    // Trả về (Found, Value) thay vì chỉ "T?" - với T là kiểu giá trị không ràng buộc (vd int dùng
    // cho đếm chưa đọc), default(T) (0) không thể phân biệt được với "cache miss" nếu chỉ dựa vào
    // null-check trên T?, vì T? trên generic không ràng buộc bị xoá kiểu (erase) về chính T tại
    // runtime khi T là struct - "cached is not null" sẽ SAI (luôn true) cho mọi giá trị kiểu số.
    Task<(bool Found, T? Value)> TryGetAsync<T>(string key, CancellationToken cancellationToken);

    Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken);

    Task RemoveAsync(string key, CancellationToken cancellationToken);

    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken);
}
