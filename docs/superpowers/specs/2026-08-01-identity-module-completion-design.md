# Hoàn thiện Module Identity — Thiết kế

| | |
|---|---|
| **Ngày** | 01/08/2026 |
| **Trạng thái** | Đã duyệt, chờ lập kế hoạch triển khai |
| **Liên quan** | `docs/KE-HOACH-TRIEN-KHAI.md` mục 1.1 (G1), `docs/PRD.md` |

## 1. Bối cảnh

Module Identity (Auth) hiện có Register, Login, RefreshToken (backend + mobile), RBAC theo `SystemRole`, và `CleanupExpiredTokensJob` dọn refresh token hết hạn. Rà soát so với kế hoạch G1 mục 1.1 ("đăng ký, đăng nhập, refresh token, quên mật khẩu") và Definition of Done của dự án, phát hiện các phần còn thiếu:

- Không có luồng Quên/Đặt lại mật khẩu.
- Không có API Logout — chỉ dựa vào job dọn định kỳ, người dùng không thể chủ động thu hồi phiên khi đăng xuất.
- Không có unit test cho `Login`/`Register`/`RefreshToken` handler.

Spec này chốt phạm vi và thiết kế để lấp các khoảng trống trên.

## 2. Phạm vi

**Trong phạm vi:**
1. API + UI Quên/Đặt lại mật khẩu (backend sinh và xác thực token; kênh gửi thật — email/SMS — chưa quyết định, tạm thời log token phía server).
2. API Logout — thu hồi đúng refresh token hiện tại của phiên đang đăng xuất (không thu hồi các thiết bị khác).
3. Unit test cho toàn bộ Auth handler: 3 handler cũ (`Login`, `Register`, `RefreshToken`) + 3 handler mới (`ForgotPassword`, `ResetPassword`, `Logout`).

**Ngoài phạm vi (quyết định có chủ đích, không làm trong lần này):**
- Rate-limit chống brute-force cho login.
- Tích hợp dịch vụ gửi email/SMS thật.
- Tách usecase riêng cho mobile auth (hiện logic nằm trong repository/provider — không sai kiến trúc, chỉ khác mức độ tách lớp).

## 3. Quyết định kiến trúc: bảng token riêng cho reset password

Dùng entity `PasswordResetToken` riêng, **không** gắn thêm cột `Purpose` vào bảng `RefreshTokens` hiện có.

Lý do: `RefreshTokenCommandHandler` hiện tra cứu `RefreshTokens` chỉ theo chuỗi `Token`. Nếu dùng chung bảng, một reset-token về mặt lý thuyết có thể bị handler refresh-token chấp nhận nhầm như một refresh token hợp lệ (cùng cấu trúc bảng, cùng cách query). Tách bảng loại bỏ hoàn toàn rủi ro nhầm lẫn ngữ nghĩa này, đồng thời giữ đúng nguyên tắc mỗi entity một trách nhiệm đang được áp dụng nhất quán trong Domain layer (`RefreshToken`, `DeviceToken`, `TaskHistory`... đều là entity độc lập).

## 4. Data model

### `PasswordResetToken` (kế thừa `BaseEntity`)

| Trường | Kiểu | Ghi chú |
|---|---|---|
| `UserId` | `Guid` (required) | FK tới `User` |
| `Token` | `string` (required) | Chuỗi random, sinh bằng cùng cơ chế `RefreshTokenGenerator` |
| `ExpiresAtUtc` | `DateTimeOffset` | 15 phút kể từ lúc tạo |
| `UsedAtUtc` | `DateTimeOffset?` | null = chưa dùng; set khi reset-password thành công, chặn tái sử dụng |

Property tiện ích `IsActive => UsedAtUtc is null && ExpiresAtUtc > DateTimeOffset.UtcNow` (mirror `RefreshToken.IsActive`).

Migration mới: `AddPasswordResetTokens` (EF Core, chạy từ `backend/src/TaskMgmt.Infrastructure`).

`CleanupExpiredTokensJob` (đã có) mở rộng để xoá thêm `PasswordResetToken` hết hạn hoặc đã dùng, chạy chung nhịp với dọn refresh token — không tạo job riêng.

## 5. API Endpoints (backend)

Tất cả nằm trong `AuthController` (`api/v1/auth`), mỗi endpoint là 1 Command + Handler + FluentValidation Validator dưới `Features/Auth/Commands/<UseCase>/`, theo đúng pattern hiện có của module (validate qua `ValidationBehavior`, không validate tay trong handler).

### `POST /auth/forgot-password`
- Request: `{ email }`
- Luôn trả `200` với message chung, **bất kể email có tồn tại trong hệ thống hay không** — chống dò email hợp lệ (user enumeration).
- Nếu user tồn tại và `IsActive`: sinh `PasswordResetToken`, lưu DB, `ILogger.LogInformation` ra token (giải pháp tạm thời thay cho gửi email thật — dễ thay thế bằng lời gọi service gửi khi kênh gửi được quyết định).
- Nếu user không tồn tại hoặc không active: không làm gì, vẫn trả cùng response 200.

### `POST /auth/reset-password`
- Request: `{ token, newPassword }`
- Validate: token tồn tại, `IsActive` (chưa dùng, chưa hết hạn) → nếu không, trả lỗi chung "token không hợp lệ hoặc đã hết hạn" (không phân biệt lý do cụ thể, tránh lộ thông tin).
- Cập nhật `User.PasswordHash` bằng `IPasswordHasher` hiện có.
- Set `UsedAtUtc` trên token.
- **Thu hồi toàn bộ `RefreshToken` đang active của user** (set `RevokedAtUtc`) — đổi mật khẩu phải buộc đăng nhập lại trên mọi thiết bị, đây là hành vi bảo mật khác với Logout thường (mục dưới) và là chủ đích thiết kế.

### `POST /auth/logout` (yêu cầu đã đăng nhập — theo `FallbackPolicy` mặc định của API, không cần `[Authorize]` tường minh)
- Request: `{ refreshToken }`
- Tìm `RefreshToken` theo chuỗi **và** `UserId` khớp user hiện tại (lấy từ `ICurrentUserService.UserId`).
- **Idempotent, luôn thành công**: nếu tìm thấy và đang active → set `RevokedAtUtc`; nếu không tìm thấy (không tồn tại, thuộc user khác, hoặc đã bị revoke/rotate trước đó) → không làm gì, vẫn trả thành công. Mirror đúng convention đã có ở `UnregisterDeviceTokenCommandHandler` (đăng xuất/gỡ token không được phép báo lỗi vì token đã bị rotate ngầm do tab/thiết bị khác tự refresh trước đó là tình huống hợp lệ, không phải lỗi).
- Chỉ thu hồi đúng token được truyền lên — **không ảnh hưởng các thiết bị khác** đang đăng nhập.

## 6. Mobile (Flutter)

- `AuthRemoteDataSource` (`lib/features/auth/data/datasources/auth_remote_data_source.dart`): thêm 3 method `forgotPassword(email)`, `resetPassword(token, newPassword)`, `logout(refreshToken)`, theo pattern try/catch + `mapDioException` đang dùng cho `login`/`register`/`refreshToken`.
- `AuthRepository.logout()`: gọi API logout **trước khi** xoá token cục bộ; nếu API lỗi (mất mạng, token đã hết hạn...) vẫn tiếp tục xoá session cục bộ — không được kẹt UI vì lỗi gọi API logout.
- 2 màn hình mới dưới `presentation/screens/`:
  - `ForgotPasswordScreen`: nhập email → gọi API → luôn hiện thông báo chung dạng "Nếu email tồn tại trong hệ thống, hướng dẫn đặt lại mật khẩu đã được gửi."
  - `ResetPasswordScreen`: nhập token (nhận thủ công ngoài luồng, vì chưa có kênh gửi thật) + mật khẩu mới + xác nhận mật khẩu.
- Route mới cho 2 màn hình trong `app_router.dart`; thêm link "Quên mật khẩu?" trên `LoginScreen`.

## 7. Testing

Thư mục mới `backend/tests/TaskMgmt.Application.UnitTests/Features/Auth/` (hiện chưa tồn tại), dùng `TestDbContextFactory` + `Fake*` service double đã có sẵn trong `Common/`, theo đúng pattern các module khác (`WorkTasks`, `TaskAssignees`...).

| Handler | Case cần cover |
|---|---|
| `LoginCommandHandler` (bù test cũ) | Đăng nhập đúng thông tin; sai mật khẩu; user không tồn tại; user `IsActive = false` |
| `RegisterCommandHandler` (bù test cũ) | Đăng ký thành công; email đã tồn tại |
| `RefreshTokenCommandHandler` (bù test cũ) | Refresh hợp lệ (rotation đúng); token hết hạn; token đã bị revoke; user không active |
| `ForgotPasswordCommandHandler` | Email tồn tại → tạo token; email không tồn tại → vẫn trả success, không tạo token; user không active → không tạo token |
| `ResetPasswordCommandHandler` | Token hợp lệ → đổi mật khẩu + thu hồi toàn bộ refresh token; token hết hạn; token đã dùng rồi; token không tồn tại |
| `LogoutCommandHandler` | Token thuộc user hiện tại → revoke đúng token; token không tồn tại; token thuộc user khác → từ chối |

Định nghĩa hoàn thành (theo DoD chung của dự án): build/lint xanh, test pass trên CI, API mới cập nhật vào Swagger, test thủ công happy path + 1-2 edge case.

## 8. Rủi ro & lưu ý

- Vì chưa có kênh gửi thật, `ForgotPassword` chỉ log token ra server log — **không dùng được cho production** ở dạng này; cần thay bằng gọi service gửi email thật trước khi lên production (theo dõi ở G5 hoặc một spec riêng khi kênh gửi được quyết định).
- Việc thu hồi toàn bộ refresh token khi reset password là hành vi cố ý khác với Logout thường — cần ghi rõ trong response/UX để người dùng hiểu vì sao bị đăng xuất trên mọi thiết bị sau khi đổi mật khẩu.
