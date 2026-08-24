# Chấm công GPS (check-in/check-out theo ngày) — Thiết kế

| | |
|---|---|
| **Ngày** | 24/08/2026 |
| **Trạng thái** | Đã duyệt, chờ lập kế hoạch triển khai |
| **Liên quan** | Backend: entity mới `AttendanceRecord`, sửa `Location` (thêm bán kính cho phép). Mobile: feature mới `attendance/`. Không liên quan tới `WorkTask`/`TaskAssignee`. |

## 1. Bối cảnh

Nghiên cứu module "Chấm công" của SuperApp (911 Group) trên máy ảo cho thấy đây là hệ thống chấm công nhân sự cấp doanh nghiệp đầy đủ: phân ca, tính đi muộn/về sớm/vắng mặt theo ca, nghỉ phép, ngày lễ, báo cáo theo team. K-app hiện **không có khái niệm ca làm việc**, và đây là tính năng hoàn toàn mới, mở rộng phạm vi ngoài PRD hiện tại (`KE-HOACH-TRIEN-KHAI.md` G0–G5 không có mục nào về chấm công).

Sau khi trao đổi, đã chốt: xây bản chấm công **theo ngày, độc lập với `WorkTask`** (không phải "check-in tại vị trí của 1 công việc cụ thể"), nhưng **tối giản hoá mạnh** so với SuperApp — chỉ ghi nhận check-in/check-out kèm GPS, không có ca làm việc, không tính đi muộn/về sớm/vắng mặt, không nghỉ phép/ngày lễ, không có màn xem của Manager/Admin cho người khác.

## 2. Phạm vi

**Trong phạm vi:**
1. Check-in / check-out 1 lần/ngày, ghi nhận thời điểm + toạ độ GPS.
2. Validate khoảng cách **server-side** bằng công thức Haversine — hợp lệ nếu nằm trong bán kính cho phép của **bất kỳ** `Location` đang hoạt động nào (không cần gán trước Location cho user).
3. Lịch sử chấm công theo tháng + thống kê tối giản (số ngày đã check-in, tổng giờ làm) trong cùng 1 màn.
4. Mỗi user chỉ xem được dữ liệu của chính mình.

**Ngoài phạm vi (quyết định có chủ đích):**
- Ca làm việc/lịch làm, phân ca cho từng người.
- Tính đi muộn/về sớm/vắng mặt (không có giờ chuẩn để so sánh).
- Nghỉ phép, ngày lễ, workflow phê duyệt (đã loại trừ từ playbook gốc — K-app không làm approval đa cấp).
- Gán trước Location cho user (phương án B2 đã cân nhắc và loại) — mọi Location active đều là điểm check-in hợp lệ.
- Theo dõi GPS liên tục / tô đỏ "ngoài phạm vi" trước khi bấm nút (SuperApp làm vậy) — chỉ đọc GPS lúc bấm nút, server từ chối thì báo lỗi.
- Màn Manager/Admin xem chấm công của người khác ("team report").

## 3. Quyết định kiến trúc

### 3.1 `WorkDate` tính theo giờ Việt Nam, không phải UTC thô
Mỗi bản ghi thuộc về đúng 1 ngày làm việc (`DateOnly WorkDate`), tính bằng `DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).Date` — nếu cắt theo UTC thô, check-in cuối ngày giờ VN có thể bị gộp nhầm sang UTC ngày hôm sau/trước.

### 3.2 Check-in mở (B1) — không gán Location trước
Lúc check-in, server duyệt mọi `Location.IsActive == true`, tính khoảng cách Haversine từ toạ độ user gửi lên tới từng Location; khớp bán kính (`Location.CheckInRadiusMeters`, mặc định 100m) của bất kỳ location nào thì hợp lệ, lưu `CheckInLocationId` là location đó. Không cần bảng gán `UserLocation` mới, không cần màn quản trị riêng — đơn giản hơn nhiều, đúng tinh thần tối giản (A1) đã chốt.

### 3.3 Attempt-then-error, không theo dõi GPS liên tục
Mobile chỉ gọi `Geolocator.getCurrentPosition()` **đúng lúc user bấm nút**, không watch vị trí liên tục như SuperApp (SuperApp tô đỏ "NGOÀI PHẠM VI CHO PHÉP" ngay cả trước khi bấm). Đơn giản hơn, không cần quyền location "Always", không tốn pin theo dõi nền — chấp nhận đánh đổi là user chỉ biết ngoài phạm vi *sau* khi bấm, không phải trước.

## 4. Component design

### 4.1 Backend — Domain

`TaskMgmt.Domain/Entities/AttendanceRecord.cs` (mới):
```csharp
public class AttendanceRecord : AuditableEntity
{
    public required Guid UserId { get; set; }
    public User? User { get; set; }
    public required DateOnly WorkDate { get; set; }
    public DateTimeOffset? CheckInAtUtc { get; set; }
    public double? CheckInLatitude { get; set; }
    public double? CheckInLongitude { get; set; }
    public Guid? CheckInLocationId { get; set; }
    public Location? CheckInLocation { get; set; }
    public DateTimeOffset? CheckOutAtUtc { get; set; }
    public double? CheckOutLatitude { get; set; }
    public double? CheckOutLongitude { get; set; }
    public Guid? CheckOutLocationId { get; set; }
    public Location? CheckOutLocation { get; set; }
}
```
Unique index `(UserId, WorkDate)` — 1 bản ghi/user/ngày.

Sửa `TaskMgmt.Domain/Entities/Location.cs`: thêm `public double CheckInRadiusMeters { get; set; } = 100;`.

`TaskMgmt.Application/Common/GeoDistance.cs` (mới, hàm thuần):
```csharp
public static class GeoDistance
{
    private const double EarthRadiusMeters = 6_371_000;

    public static double CalculateMeters(double lat1, double lng1, double lat2, double lng2)
    {
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLng = DegreesToRadians(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
}
```

### 4.2 Backend — CQRS (`Application/Features/Attendance/`)

- `CheckInCommand(double Latitude, double Longitude) : IRequest<AttendanceRecordDto>` — validator chặn nếu hôm nay đã có bản ghi với `CheckInAtUtc != null`; handler tính `WorkDate`, duyệt Location active tìm khoảng cách ≤ `CheckInRadiusMeters`, không có location nào khớp thì ném lỗi *"Ngoài phạm vi cho phép của mọi vị trí đã đăng ký."*
- `CheckOutCommand(double Latitude, double Longitude) : IRequest<AttendanceRecordDto>` — validator chặn nếu hôm nay chưa có bản ghi đã check-in (`CheckInAtUtc != null && CheckOutAtUtc == null`); handler không validate lại khoảng cách với location nào (check-out ghi nhận toạ độ nhưng không chặn theo phạm vi — người dùng có thể check-out từ xa nếu đã check-in hợp lệ trước đó).
- `GetTodayAttendanceQuery : IRequest<AttendanceRecordDto?>`
- `GetAttendanceHistoryQuery(int Year, int Month) : IRequest<List<AttendanceRecordDto>>`
- `GetAttendanceStatsQuery(int Year, int Month) : IRequest<AttendanceStatsDto>`

DTO (`Features/Attendance/Common/`):
```csharp
public record AttendanceRecordDto(
    Guid Id, DateOnly WorkDate,
    DateTimeOffset? CheckInAtUtc, string? CheckInLocationName,
    DateTimeOffset? CheckOutAtUtc, string? CheckOutLocationName);

public record AttendanceStatsDto(int DaysCheckedIn, double TotalHoursWorked);
```
`TotalHoursWorked` chỉ cộng những ngày có cả `CheckInAtUtc` và `CheckOutAtUtc`; ngày có check-in nhưng chưa check-out vẫn tính vào `DaysCheckedIn` nhưng không cộng giờ.

### 4.3 Backend — API

`AttendanceController` (`api/v1/attendance`, `[Authorize]` thường — không cần policy `RequireManager`, mọi user tự thao tác trên dữ liệu của chính mình qua `ICurrentUserService.UserId`, không có tham số userId trên route):
- `POST /check-in`, `POST /check-out`
- `GET /today`, `GET /history?year=&month=`, `GET /stats?year=&month=`

### 4.4 Mobile — `lib/features/attendance/`

- Domain: `AttendanceRecord`, `AttendanceStats`.
- Data: `AttendanceRemoteDataSource` (5 lệnh Dio), `AttendanceRepositoryImpl`.
- Presentation: `AttendanceController` (Riverpod `AsyncNotifier`, pattern giống `AttachmentsController`), `AttendanceScreen` (2 tab: "Check-in", "Lịch sử").

**Tab Check-in:** trạng thái hôm nay (chưa check-in / đã check-in lúc HH:mm tại `<location>` / hoàn thành HH:mm→HH:mm), 2 nút CHECK-IN/CHECK-OUT disable theo đúng trạng thái, đọc GPS qua `Geolocator.getCurrentPosition()` lúc bấm, lỗi từ server hiện qua SnackBar.

**Tab Lịch sử:** date picker theo tháng, header hiện `daysCheckedIn`/`totalHoursWorked` của tháng đó, danh sách mỗi ngày 1 dòng (giờ check-in→check-out, tên location).

**Điểm vào:** `IconButton` (`Icons.fingerprint`) trong `AppBar.actions` của `TaskListScreen`, cùng hàng Dashboard/Thông báo/Vị trí, `context.push(AttendanceScreen.path)`.

## 5. Testing

**Backend** (`TaskMgmt.Application.UnitTests`, dùng `TestDbContextFactory` + `Fake*` có sẵn):
- `GeoDistanceTests`: 2 điểm trùng nhau = 0m; 2 toạ độ cách khoảng cách biết trước, sai số chấp nhận được.
- `CheckInCommandHandlerTests`: trong bán kính → tạo bản ghi đúng `CheckInLocationId`; ngoài bán kính mọi location → lỗi; đã check-in hôm nay → validator chặn.
- `CheckOutCommandHandlerTests`: check-out bình thường sau khi đã check-in → cập nhật đúng bản ghi; chưa check-in mà check-out → validator chặn.
- `GetAttendanceStatsQueryHandlerTests`: tính đúng `DaysCheckedIn`/`TotalHoursWorked` từ nhiều bản ghi mẫu, kể cả bản ghi chưa check-out.

**Mobile:**
- `AttendanceController`/repository qua fake repository (không đụng `geolocator` thật — plugin platform-channel, không chạy được trong `flutter_test`, giống lý do `image_picker`/`file_picker`).
- Widget test `AttendanceScreen`: đúng trạng thái/nút theo dữ liệu giả từ `GetTodayAttendanceQuery`, tab Lịch sử render đúng danh sách + số liệu từ dữ liệu giả.
- Gọi GPS thật lúc bấm nút xác minh thủ công trên máy ảo/thiết bị thật (không tự động hoá được).

## 6. Dependencies mới

`geolocator` (bản ổn định mới nhất tại lúc lập kế hoạch triển khai) — đọc GPS thiết bị thật, `flutter_map`/`latlong2` hiện có chỉ vẽ bản đồ, không đọc vị trí máy.

## 7. Rủi ro & lưu ý

- Cần khai báo quyền vị trí: Android `ACCESS_FINE_LOCATION` (`AndroidManifest.xml`), iOS `NSLocationWhenInUseUsageDescription` (`Info.plist`) — kiểm tra không xung đột với quyền camera đã thêm ở tính năng Upload UX.
- Hệ thống hiện chưa có `Location` nào với `CheckInRadiusMeters` phù hợp thực tế — cần dữ liệu `Location` active tồn tại để check-in có thể thành công; nếu DB trống location, mọi check-in sẽ luôn thất bại (hành vi đúng theo thiết kế, không phải bug).
- Độ chính xác GPS trên thiết bị thật/máy ảo có thể dao động vài chục mét — bán kính mặc định 100m nên đủ dung sai, nhưng cần thử nghiệm thực tế trước khi đặt cố định cho location thật.
- Do đọc GPS chỉ lúc bấm nút (3.3), UX sẽ có độ trễ ngắn (chờ `getCurrentPosition()`) trước khi biết kết quả — cần loading state trên nút trong lúc chờ.
