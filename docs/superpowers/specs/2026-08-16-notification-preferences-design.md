# Cài đặt loại thông báo (Notification Preferences) — Thiết kế

| | |
|---|---|
| **Ngày** | 16/08/2026 |
| **Trạng thái** | Đã duyệt, chờ lập kế hoạch triển khai |
| **Liên quan** | `docs/superpowers/specs/` (chưa có spec badge — badge chuông đã có sẵn từ commit `56ba7ea` "G3-G4"), `backend/src/TaskMgmt.Application/Features/Notifications/Common/TaskNotificationHelper.cs` |

## 1. Bối cảnh

Rà soát tính năng thông báo hiện có phát hiện: badge số chưa đọc trên app bar (`TaskListScreen`) và Trung tâm thông báo (`NotificationCenterScreen`) đã hoạt động đầy đủ (provider, repository, endpoint `GET /notifications/unread-count`). Phần còn thiếu là khả năng người dùng **tự bật/tắt từng loại thông báo** — hiện mọi hành động (đổi trạng thái, thêm bình luận, gắn người tham gia...) đều tạo thông báo trong-app **và** đẩy push, không có cách nào để user giảm bớt push cho những loại họ không quan tâm.

Rà soát backend cho thấy tài liệu `docs/DATABASE-DESIGN.md` (viết trước khi code) có đề cập bảng `task_mutes`, nhưng **bảng này chưa từng được implement** — danh sách entity thật trong `TaskMgmt.Domain/Entities/` không có `TaskMute`. Đây là tính năng hoàn toàn mới, không kế thừa cơ chế nào có sẵn.

Danh sách loại thông báo thật (chuỗi tự do ở cột `Notification.Type`, không phải enum cứng DB), tổng hợp từ toàn bộ lời gọi `TaskNotificationHelper.NotifyAsync` trong code:

`FieldChanged`, `StatusChanged`, `Deleted`, `AssigneeAdded`, `AssigneeRemoved`, `AssigneeRoleChanged`, `CommentAdded`, `AttachmentAdded`, `DueSoon`, `Overdue`.

## 2. Phạm vi

**Trong phạm vi:**
1. Backend: bảng `notification_preferences`, 2 endpoint (đọc trạng thái 10 loại, bật/tắt 1 loại), sửa `TaskNotificationHelper.NotifyAsync` để tôn trọng preference trước khi enqueue push.
2. Mobile: màn `notification_preferences_screen.dart` (10 `SwitchListTile`), mở từ icon settings trên app bar của `NotificationCenterScreen`.
3. Cache-aside cho set loại đã tắt theo user, theo đúng pattern `CacheKeys`/`ICacheService` hiện có.

**Ngoài phạm vi (quyết định có chủ đích):**
- Gộp nhóm loại thông báo (vd. "Cộng tác", "Nhắc hạn") — chốt hiển thị phẳng 10 toggle riêng lẻ, không thêm lớp nhóm.
- Tắt/replace `task_mutes` (mute theo từng task cụ thể) — đây là khái niệm khác (mute theo loại, không phải theo task), không làm trong spec này.
- Đồng bộ preference qua nhiều thiết bị real-time (SignalR) — lưu server-side nên tự động nhất quán khi load lại, không cần đẩy realtime.

## 3. Quyết định kiến trúc: bảng thưa (sparse), vắng mặt = mặc định bật

Cân nhắc 3 phương án lưu trạng thái bật/tắt:

- **A — Bảng thưa** `notification_preferences(user_id, type)`: chỉ có row khi user **tắt** 1 loại. Vắng mặt = bật (mặc định).
- **B — Cột mảng trên `users`** (`text[]`): ít bảng hơn nhưng Npgsql cần cấu hình map kiểu mảng riêng, khó truy vấn thống kê theo type sau này.
- **C — Ma trận đầy đủ** (1 row/user/type cho cả 10 loại, luôn tồn tại): tường minh nhưng cần backfill toàn bộ user hiện có và re-seed mỗi khi thêm loại thông báo mới.

Chọn **A**: không cần backfill dữ liệu cũ, tự động hỗ trợ loại thông báo thêm sau này (chỉ cần thêm string vào danh sách cứng phía BE, không đụng migration), và khớp đúng ngữ nghĩa "mặc định bật hết, user tắt bớt" đã chốt.

**Hành vi khi tắt 1 loại**: chỉ chặn **push** (enqueue FCM), **vẫn ghi** `Notification` trong-app bình thường (vẫn hiện trong Trung tâm thông báo, vẫn tính vào unread count). Quyết định này giữ được lịch sử đầy đủ trong app, chỉ giảm phiền nhiễu ở kênh đẩy — khác hẳn hành vi "tắt hoàn toàn" vốn sẽ xoá luôn dữ liệu lịch sử loại đó.

## 4. Data model

### `NotificationPreference` (kế thừa `BaseEntity`)

| Trường | Kiểu | Ghi chú |
|---|---|---|
| `UserId` | `Guid` (required) | FK tới `User` |
| `Type` | `string` (required, max 50) | Khớp đúng chuỗi `Notification.Type` (`FieldChanged`, `CommentAdded`...) |

Unique index `(UserId, Type)` — 1 user không thể có 2 row cùng loại. Migration mới: `AddNotificationPreferences` (EF Core, `backend/src/TaskMgmt.Infrastructure`).

Danh sách 10 loại hợp lệ định nghĩa **cứng phía backend** (không lưu DB) trong 1 hằng số dùng chung, ví dụ `NotificationTypes.All` — dùng để: (a) validate `type` truyền lên khi PUT, (b) trả đủ 10 dòng khi GET kể cả loại chưa có row nào (mặc định bật).

## 5. API Endpoints (backend)

Cùng `NotificationsController` (`api/v1/notifications`), mỗi endpoint là Query/Command riêng dưới `Features/Notifications/{Queries,Commands}/`, theo đúng pattern hiện có (`GetUnreadNotificationCountQuery`, `MarkNotificationAsReadCommand`...).

### `GET /notifications/preferences`
- Trả về mảng 10 phần tử `{ type, isEnabled }` — union giữa `NotificationTypes.All` (cứng) và các row đã tắt của user hiện tại (`ICurrentUserService.UserId`).

### `PUT /notifications/preferences/{type}`
- Request: `{ isEnabled: bool }`.
- Validate `type` nằm trong `NotificationTypes.All` — sai trả `400`.
- `isEnabled = false` → insert row nếu chưa có (idempotent, bỏ qua nếu đã tồn tại).
- `isEnabled = true` → xoá row nếu có (idempotent, không lỗi nếu vốn đã bật).
- Invalidate `CacheKeys.DisabledNotificationTypes(userId)` sau khi ghi.

### Sửa `TaskNotificationHelper.NotifyAsync`
Dùng tham số `ICacheService cache` đã có sẵn trong signature để đọc set loại đã tắt qua cache-aside (`CacheKeys.DisabledNotificationTypes(userId)`, TTL 5 phút — cùng bậc với `UnreadNotificationCountExpiration`). Nếu `type` nằm trong set đã tắt: **bỏ qua** dòng `jobScheduler.EnqueuePushNotification(...)`, **giữ nguyên** dòng `context.Notifications.Add(...)` phía trên. Đây là điểm hội tụ duy nhất nên chỉ sửa 1 chỗ, mọi event handler gọi `NotifyAsync` tự động được áp dụng.

## 6. Mobile (Flutter)

- `features/notifications/domain/entities/notification_preference.dart`: `{ type, isEnabled }` + map tĩnh `type → nhãn tiếng Việt` (vd. `CommentAdded` → "Có bình luận mới", `DueSoon` → "Sắp đến hạn", `Overdue` → "Quá hạn"...).
- Mở rộng `NotificationRepository`/`NotificationRepositoryImpl`/`NotificationRemoteDataSource` hiện có: thêm `getPreferences()` và `updatePreference(type, isEnabled)` — không tạo repository riêng, vì cùng domain "notifications".
- `preferences_provider.dart`: `AsyncNotifierProvider<NotificationPreferencesController, List<NotificationPreference>>`; method `toggle(type)` cập nhật state optimistic trước, gọi PUT, rollback + `ApiException`-based snackbar nếu lỗi (theo đúng pattern lỗi đang dùng ở `WorkTasksController`/`ErrorStateView`).
- `notification_preferences_screen.dart`: `ListView` 10 `SwitchListTile`, mỗi item gọi `toggle`.
- Route mới `/notifications/preferences` trong `app_router.dart`; icon `Icons.settings_outlined` trên app bar của `notification_center_screen.dart` push tới route này.

## 7. Testing

Thư mục mới `backend/tests/TaskMgmt.Application.UnitTests/Features/Notifications/` (bổ sung file, thư mục `Features/Notifications` đã tồn tại theo `NotificationTests.cs`).

| Handler / Helper | Case cần cover |
|---|---|
| `TaskNotificationHelper.NotifyAsync` | Loại chưa tắt → `Add` Notification + gọi `EnqueuePushNotification`; loại đã tắt → vẫn `Add` Notification nhưng **không** gọi `EnqueuePushNotification` |
| `GetNotificationPreferencesQueryHandler` | User chưa tắt gì → trả 10/10 `isEnabled = true`; user đã tắt 2 loại → 2 loại đó `isEnabled = false` |
| `UpdateNotificationPreferenceCommandHandler` | Tắt loại chưa có row → tạo row; tắt loại đã tắt sẵn → idempotent không lỗi; bật lại → xoá row; `type` không hợp lệ → lỗi `400` |

Mobile: widget test cho `notification_preferences_screen.dart` (tap switch → gọi đúng `toggle(type)`; lỗi API → switch trở lại trạng thái cũ + hiện snackbar).

Định nghĩa hoàn thành (theo DoD chung của dự án): build/lint xanh, test pass trên CI, API mới cập nhật vào Swagger, test thủ công bật/tắt 1 loại rồi trigger đúng hành động đó kiểm tra không nhận push nhưng vẫn thấy trong Trung tâm thông báo.

## 8. Rủi ro & lưu ý

- Cache 5 phút cho set loại đã tắt nghĩa là user tắt 1 loại có thể vẫn nhận **tối đa 1 push cuối** trong vòng 5 phút sau khi tắt (do cache cũ chưa invalidate kịp ở request đọc — thực tế đã invalidate ngay khi ghi nên trường hợp này chỉ xảy ra nếu 2 request chạy đồng thời, chấp nhận được, không cần xử lý đặc biệt).
- Danh sách 10 loại hiện là hằng số code (`NotificationTypes.All`) — nếu sau này thêm loại thông báo mới mà quên thêm vào hằng số này, loại đó sẽ **không xuất hiện** trong màn cài đặt (mặc định coi như luôn bật, không tắt được) cho đến khi cập nhật hằng số. Cần nhớ cập nhật đồng thời khi thêm `NotifyAsync(..., type: "LoạiMới", ...)` ở bất kỳ event handler nào.
