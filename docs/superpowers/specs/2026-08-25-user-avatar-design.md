# Avatar người dùng (tự upload + hiển thị khắp nơi) — Thiết kế

| | |
|---|---|
| **Ngày** | 25/08/2026 |
| **Trạng thái** | Đã duyệt, chờ lập kế hoạch triển khai |
| **Liên quan** | Backend: sửa `User` (+migration), thêm 3 endpoint `UsersController`, sửa `UserDto`/`TaskAssigneeDto`/`CommentDto`. Mobile: widget `UserAvatar` dùng chung, màn `/profile` mới, sửa `HomeScreen`. |

## 1. Bối cảnh

Nghiên cứu SuperApp trên máy ảo: avatar tròn ở góc trên-phải Home dẫn vào màn "My Profile" (avatar lớn + nút camera nổi để đổi ảnh), avatar người gửi cũng hiện trong danh sách thông báo. K-app hiện **không có avatar thật** — `User` entity không có trường ảnh, mọi nơi hiển thị người dùng (assignee, comment, lịch sử) đều dùng `CircleAvatar` với chữ cái đầu tên.

Rà lại các nơi thực sự gắn với 1 user cụ thể (loại các `CircleAvatar` dùng làm chấm màu trạng thái không liên quan — `work_task_list_item.dart`, `work_task_kanban_board.dart`, `dashboard_stat_grid.dart`): **assignee list, comment list (tác giả)**. `task_history_timeline_section.dart` cũng có `CircleAvatar` nhưng bên trong là **icon loại sự kiện** (đổi trạng thái, thêm assignee...) chứ không phải ảnh người dùng — giữ nguyên, không đổi thành avatar thật vì sẽ làm mất thông tin "loại sự kiện gì" đang truyền tải qua icon. Comment mentions hiển thị dạng chip `@tên`, không có avatar, nên cũng không đụng tới. Thông báo (`AppNotification`) hiện chưa lưu "ai gây ra sự kiện" nên không nằm trong phạm vi lần này (sẽ cần tính năng riêng để track người gửi).

## 2. Phạm vi

**Trong phạm vi:**
1. Tự upload/đổi/xoá avatar của chính mình, qua màn "Hồ sơ của tôi" mới.
2. Hiển thị avatar thật (thay vì chỉ chữ cái đầu) tại: assignee list, comment list (tác giả).
3. Avatar nhỏ trên Home, bấm vào mở màn Hồ sơ.
4. Ảnh lưu qua `IFileStorageService` (S3/MinIO) đã có, tái dùng hạ tầng của tính năng đính kèm file.

**Ngoài phạm vi (quyết định có chủ đích):**
- Avatar trong danh sách thông báo — `Notification` chưa lưu actor user, cần thiết kế riêng.
- Avatar trong task history timeline — `CircleAvatar` ở đó đang hiện icon loại sự kiện, không phải ảnh người dùng; đổi sẽ làm mất thông tin đang truyền tải.
- Crop/resize ảnh phía client trước khi upload — chấp nhận ảnh gốc, `CircleAvatar`/`BoxFit.cover` tự crop tròn khi hiển thị.
- Presigned/public URL từ S3 — giữ nguyên pattern bảo mật proxy-qua-API như Attachment.
- Avatar cho Location hay bất kỳ entity nào khác ngoài `User`.

## 3. Quyết định kiến trúc

### 3.1 Proxy qua API có xác thực, không public URL
`GET /users/{id}/avatar` stream ảnh qua `IFileStorageService`, giống hệt `DownloadAttachmentQueryHandler`. Không dùng presigned URL của S3 — nhất quán với cách Attachment đang bảo mật, không cần thêm hạ tầng mới.

### 3.2 `HasAvatar: bool` trên DTO, không lộ storage key
Các DTO liên quan user cần hiển thị avatar (`UserDto`, `TaskAssigneeDto`, `CommentDto`) thêm cờ `HasAvatar`/`...HasAvatar` thay vì lộ `AvatarStorageKey` ra ngoài. Cờ này cho mobile biết có nên gọi `GET .../avatar` hay hiển thị chữ cái đầu ngay — tránh gọi API/nhận 404 thừa cho phần lớn user chưa có ảnh.

### 3.3 Cache avatar phía mobile bằng Riverpod `family`, không thêm package
`FutureProvider.family<Uint8List?, String>(userId)` tải ảnh 1 lần/userId, Riverpod tự chia sẻ kết quả cho mọi widget cùng watch cùng `userId` trong phiên làm việc (nhiều dòng assignee/comment của cùng 1 người chỉ tải 1 lần). Không cần `cached_network_image` hay presigned URL.

## 4. Component design

### 4.1 Backend — Domain

Sửa `TaskMgmt.Domain/Entities/User.cs`: thêm `public string? AvatarStorageKey { get; set; }`.

Migration mới: `AddUserAvatar` — cột `AvatarStorageKey` (nullable `text`), không có gì khác thay đổi trên bảng `Users`.

### 4.2 Backend — CQRS (`Application/Features/Users/`)

- `UploadAvatarCommand(Stream Content, string FileName, long SizeBytes) : IRequest<UserDto>`
  - Validator: `SizeBytes <= 5 * 1024 * 1024`.
  - Handler: validate content-type qua whitelist ảnh (`.jpg/.jpeg/.png/.webp` — tái dùng `AttachmentFileValidator.TryGetAllowedContentType` + `MatchesSignature`, nhưng chặn nếu content-type không bắt đầu bằng `image/`, tức từ chối `.gif`/doc/pdf dù có trong whitelist gốc); nếu `User.AvatarStorageKey` cũ khác null thì `storage.DeleteAsync(oldKey)` trước; storage key mới `avatars/{userId}/{Guid.NewGuid()}{ext}`; `storage.UploadAsync(...)`; cập nhật `User.AvatarStorageKey`; trả về `UserDto` mới (`HasAvatar = true`).
- `DeleteAvatarCommand : IRequest<UserDto>`
  - Handler: nếu có `AvatarStorageKey` thì xoá khỏi storage, set `null`; trả về `UserDto` (`HasAvatar = false`). Gọi khi chưa có avatar vẫn trả về bình thường (no-op), không lỗi.
- `GetUserAvatarQuery(Guid UserId) : IRequest<UserAvatarResult>` (`UserAvatarResult(Stream Content, string ContentType)`)
  - Handler: load `User`, nếu `AvatarStorageKey == null` → `NotFoundException`; ngược lại `storage.DownloadAsync(key)`, trả kèm content-type suy từ phần mở rộng lưu song song (lưu `AvatarContentType` cùng cột? — **không cần cột riêng**: suy content-type từ đuôi file trong storage key bằng `AttachmentFileValidator.TryGetAllowedContentType` trên chính storage key, vì đuôi đã được giữ nguyên lúc tạo key).

`UploadAvatarCommand`/`DeleteAvatarCommand` đều lấy `UserId` từ `ICurrentUserService.UserId` (thao tác trên chính mình), không nhận tham số route.

### 4.3 Backend — DTO ripple

`UserDto` (`Features/Auth/Common/AuthResultDto.cs`): thêm `bool HasAvatar` — cập nhật 4 chỗ dựng (`LoginCommandHandler`, `RegisterCommandHandler`, `RefreshTokenCommandHandler`, `GetUsersQueryHandler`), tất cả suy từ `user.AvatarStorageKey != null`.

`TaskAssigneeDto`: thêm `bool UserHasAvatar`, suy từ `assignee.User!.AvatarStorageKey != null` tại nơi map hiện có.

`CommentDto`: thêm `bool AuthorHasAvatar` (map từ tác giả). `CommentMentionDto` **không đổi** — mentions hiển thị dạng chip text, không có avatar.

`TaskHistoryDto` **không đổi** — timeline dùng icon loại sự kiện, không phải avatar người dùng (xem mục 1).

### 4.4 Backend — API

`UsersController` (`api/v1/users`) thêm:
- `POST /me/avatar` — `[RequestSizeLimit(5 * 1024 * 1024)]`, `IFormFile file`, buffer vào `MemoryStream` giống `AttachmentsController.Upload`, gửi `UploadAvatarCommand`, trả `UserDto`.
- `DELETE /me/avatar` — gửi `DeleteAvatarCommand`, trả `UserDto`.
- `GET /{id:guid}/avatar` — gửi `GetUserAvatarQuery`, trả `File(stream, contentType)`.

### 4.5 Mobile — widget dùng chung (`lib/shared/widgets/user_avatar.dart`)

```dart
class UserAvatar extends ConsumerWidget {
  const UserAvatar({required this.userId, required this.hasAvatar, required this.fallbackText, this.radius = 20});

  final String userId;
  final bool hasAvatar;
  final String fallbackText; // chữ cái đầu, đã uppercase, do caller truyền vào
  final double radius;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    if (!hasAvatar) return CircleAvatar(radius: radius, child: Text(fallbackText));

    final bytesAsync = ref.watch(avatarBytesProvider(userId));
    return bytesAsync.when(
      data: (bytes) => bytes == null
          ? CircleAvatar(radius: radius, child: Text(fallbackText))
          : CircleAvatar(radius: radius, backgroundImage: MemoryImage(bytes)),
      loading: () => CircleAvatar(radius: radius, child: const SizedBox.square(dimension: 12, child: CircularProgressIndicator(strokeWidth: 2))),
      error: (_, _) => CircleAvatar(radius: radius, child: Text(fallbackText)),
    );
  }
}
```

`avatarBytesProvider = FutureProvider.family<Uint8List?, String>((ref, userId) async { ... })` gọi `UsersRepository.downloadAvatar(userId)`; bắt lỗi 404 (không nên xảy ra khi `hasAvatar=true` nhưng vẫn phòng hờ race — user vừa xoá avatar) trả `null` thay vì throw.

Thay 2 chỗ `CircleAvatar(child: Text(initial))` hiện có bằng `UserAvatar(...)`:
- `assignee_list_section.dart` (`userId: assignee.userId`, `hasAvatar: assignee.userHasAvatar`)
- `comment_list_section.dart` (avatar tác giả comment; phần mentions dạng chip `@tên` giữ nguyên, không đổi)

`task_history_timeline_section.dart` giữ nguyên — `CircleAvatar` ở đó là icon loại sự kiện (mục 1), không đụng tới.

### 4.6 Mobile — feature `users/` mới

- `domain/repositories/users_repository.dart`: `Future<Uint8List> downloadAvatar(String userId)`, `Future<User> uploadAvatar(Uint8List bytes, String fileName)`, `Future<User> deleteAvatar()`.
- `data/datasources/users_remote_data_source.dart`: 3 hàm Dio tương ứng, pattern giống `AttachmentRemoteDataSource` (`FormData.fromMap` cho upload, `ResponseType.bytes` cho download).
- `data/repositories/users_repository_impl.dart`.
- `presentation/providers/users_provider.dart`: `avatarBytesProvider` (mục 4.5) + `AsyncNotifier` cho upload/xoá avatar của chính mình (cập nhật lại `authControllerProvider.state` với `User` mới trả về, để `HomeScreen`/`ProfileScreen` phản ánh ngay không cần refetch riêng).

### 4.7 Mobile — màn Hồ sơ của tôi (`lib/features/users/presentation/screens/profile_screen.dart`)

Route mới `/profile` (`ProfileScreen.path`/`.name`), thêm vào `app_router.dart`.

Layout: avatar lớn (`UserAvatar` radius lớn, dùng `user.hasAvatar`/`user.id` từ `authControllerProvider`) + nút camera nổi góc dưới-phải (giống SuperApp) mở bottom sheet chọn "Chụp ảnh"/"Chọn từ thư viện" (tái dùng `ImagePicker().pickImage(source: ...)` đúng pattern `attachment_list_section.dart`), sau khi chọn thì gọi upload, hiện `CircularProgressIndicator` đè lên avatar lúc đang tải. Nút "Xoá avatar" (chỉ hiện khi `hasAvatar == true`) gọi delete. Bên dưới: `ListTile` chỉ đọc cho họ tên, email. Lỗi upload/xoá (vd sai định dạng, quá 5MB — validate sơ bộ phía client trước khi gửi để phản hồi nhanh, server vẫn validate lại) hiện qua `SnackBar`.

### 4.8 Mobile — Home

`home_screen.dart`: thay đoạn `Text('Xin chào, ${user.fullName}')` bằng `Row` gồm `UserAvatar(radius: 18, ...)` + text, bọc `InkWell`/`GestureDetector` mở `context.push(ProfileScreen.path)` khi bấm vào avatar.

## 5. Validation & giới hạn

- Định dạng: `.jpg/.jpeg/.png/.webp` — kiểm tra đuôi + magic bytes (tái dùng `AttachmentFileValidator`, lọc thêm điều kiện content-type bắt đầu bằng `image/`). Sai định dạng → 400 với thông báo rõ ("Chỉ nhận ảnh JPG/PNG/WEBP.").
- Dung lượng: tối đa 5MB (`RequestSizeLimit` + validator).
- Mỗi user chỉ có 1 avatar tại 1 thời điểm — upload mới luôn thay thế, không giữ lịch sử.

## 6. Testing

**Backend** (`TaskMgmt.Application.UnitTests`):
- `UploadAvatarCommandHandlerTests`: upload lần đầu → `AvatarStorageKey` được set, storage nhận đúng key/content-type; upload lần 2 → storage cũ bị xoá trước khi ghi key mới; sai định dạng/quá dung lượng → validator chặn.
- `DeleteAvatarCommandHandlerTests`: có avatar → xoá storage + clear field; chưa có avatar → no-op, không lỗi.
- `GetUserAvatarQueryHandlerTests`: có avatar → trả đúng stream/content-type; chưa có → `NotFoundException`.
- Cập nhật test hiện có của `LoginCommandHandler`/`RegisterCommandHandler`/`RefreshTokenCommandHandler`/`GetUsersQueryHandler` cho tham số `HasAvatar` mới trên `UserDto`, và test liên quan `TaskAssigneeDto`/`CommentDto` cho `UserHasAvatar`/`AuthorHasAvatar` mới.

**Mobile:**
- `UserAvatar` widget test: `hasAvatar=false` → hiện chữ cái đầu, không gọi provider; `hasAvatar=true` + provider trả bytes → hiện `CircleAvatar` với `backgroundImage`; provider lỗi → fallback chữ cái đầu.
- `ProfileScreen` widget test: chọn ảnh (fake `ImagePickerPlatform.instance`, theo đúng cách đã làm ở Upload UX) → gọi đúng upload; bấm Xoá avatar khi `hasAvatar=true` → gọi đúng delete; ẩn nút Xoá khi chưa có avatar.
- Cập nhật test hiện có của `assignee_list_section`/`comment_list_section` cho field `...HasAvatar` mới trên fake DTO/entity.
- Không tự động hoá việc chọn ảnh thật từ camera/thư viện (giới hạn platform-channel giống `image_picker` ở Upload UX) — xác minh thủ công trên máy ảo/thiết bị thật.

## 7. Dependencies mới

Không có — tái dùng `image_picker` (đã có từ Upload UX), `dio`, `flutter_riverpod`, `IFileStorageService` (đã có từ Attachments).

## 8. Rủi ro & lưu ý

- Ripple DTO (`UserDto`, `TaskAssigneeDto`, `CommentDto`) chạm nhiều file — cần rà kỹ toàn bộ chỗ dựng DTO thủ công (không phải factory `FromEntity`) để không sót tham số mới khi build.
- `avatarBytesProvider` cache theo `userId` trong suốt phiên app. `ProfileScreen`/`HomeScreen` không đọc bytes trực tiếp — cả hai render qua widget dùng chung `UserAvatar`, và `UserAvatar` cũng `ref.watch(avatarBytesProvider(userId))` như mọi nơi khác (comment, assignee list...). Vì vậy khi user đổi/xoá avatar của chính mình, `ProfileScreen`/`HomeScreen` chỉ phản ánh ngay nhờ `_pickAndUpload`/`_deleteAvatar` gọi tường minh `ref.invalidate(avatarBytesProvider(updatedUser.id))` trước khi cập nhật `authControllerProvider` — nếu thiếu bước invalidate này, `UserAvatar` sẽ tiếp tục hiện bytes cũ đã cache dù `authControllerProvider`/`hasAvatar` đã đúng. Các widget khác đang hiển thị avatar cũ của họ ở nơi khác (vd trong comment cũ) vẫn không tự cập nhật cho tới khi app khởi động lại phiên provider đó; chấp nhận được vì đây là ảnh đại diện cá nhân, không phải dữ liệu cần real-time.
- Cần đảm bảo `RequestSizeLimit(5MB)` trên endpoint avatar không xung đột với giới hạn 25MB đã đặt cho Attachment (2 endpoint khác nhau, áp dụng độc lập theo route — không có tranh chấp).
