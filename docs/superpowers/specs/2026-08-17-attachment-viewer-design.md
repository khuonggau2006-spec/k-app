# Trình xem tệp đính kèm trong app — Thiết kế

| | |
|---|---|
| **Ngày** | 17/08/2026 |
| **Trạng thái** | Đã duyệt, chờ lập kế hoạch triển khai |
| **Liên quan** | `mobile/taskmgmt_app/lib/features/attachments/` (feature có sẵn), backend không đổi |

## 1. Bối cảnh

`AttachmentListSection` hiện tải tệp đính kèm về máy rồi giao thẳng cho hệ điều hành mở bằng ứng dụng ngoài (`OpenFilex.open`) — không có xem trước trong app. Đây là gợi ý "áp dụng ngay" từ playbook nghiên cứu SuperApp trước đó (`SuperApp Adoption Playbook`), phần duy nhất còn lại thuộc nhóm "trong scope MVP hiện tại" chưa làm.

Rà soát code hiện có cho thấy **backend không cần sửa gì**: endpoint `GET /worktasks/{taskId}/attachments/{attachmentId}/download` đã trả đủ bytes kèm `ContentType`/`FileName` đúng chuẩn, và `Attachment` (domain entity mobile) đã có sẵn `contentType` + getter `isImage`. Đây là tính năng thuần mobile/frontend.

## 2. Phạm vi

**Trong phạm vi:**
1. Xem trước trong app cho 3 loại: ảnh (lướt ngang giữa các ảnh cùng task, pinch-zoom), PDF (đơn file), video (đơn file, có control play/pause).
2. Route mới `/tasks/:taskId/attachments/:attachmentId`, mở từ `AttachmentListSection` khi bấm vào 1 trong 3 loại trên.
3. Cache bytes ảnh trong bộ nhớ khi lướt gallery (không tải lại khi lướt qua lướt lại).

**Ngoài phạm vi (quyết định có chủ đích):**
- Các loại file khác (Word/Excel/PowerPoint, file không rõ định dạng...) — không có thư viện Flutter miễn phí xem tốt trong app, giữ nguyên hành vi `OpenFilex` mở app ngoài hiện tại.
- Nút "chia sẻ"/"mở bằng ứng dụng khác" trong màn viewer — chưa cần, `OpenFilex` vẫn là đường vào cho các loại file ngoài phạm vi.
- Chỉnh sửa/annotate PDF, transcript video, thumbnail preview trong danh sách đính kèm.
- Upload UX (chọn nguồn file, tiến trình tải) — mục riêng trong playbook, không làm ở đây.

## 3. Quyết định kiến trúc: 1 màn hình rẽ nhánh, tách widget theo loại file

Một `AttachmentViewerScreen` duy nhất đọc `contentType` rồi build 1 trong 3 widget con (`ImageGalleryView`/`PdfFileView`/`VideoFileView`), thay vì 3 màn hình + 3 route riêng biệt.

Lý do: 3 route riêng sẽ cần tách phần khung tải/loading/lỗi dùng chung ra thêm 1 lớp nữa cho quy mô tính năng này mà không thêm lợi ích rõ rệt — trong khi tách theo widget con (mỗi loại file 1 file riêng dưới `presentation/widgets/`) vẫn giữ đúng nguyên tắc "mỗi file một trách nhiệm", test độc lập được, chỉ gọn hơn ở tầng routing.

`AttachmentViewerScreen` dùng lại `attachmentsProvider(taskId)` đã được `AttachmentListSection` load sẵn (Riverpod cache theo `taskId`) — không gọi API lấy danh sách thêm lần nữa, chỉ `firstWhere` theo `attachmentId` trong danh sách đó.

## 4. Component design

### `AttachmentViewerScreen`
- `ConsumerWidget`, nhận `taskId`/`attachmentId` từ route path.
- Watch `attachmentsProvider(taskId)`; khi có data, tìm attachment theo id.
- Rẽ nhánh theo `contentType`:
  - `isImage` → `ImageGalleryView(images: <lọc isImage từ toàn bộ list>, initialIndex: <vị trí attachment trong images>)`.
  - `== 'application/pdf'` → `PdfFileView(attachment: ...)`.
  - `startsWith('video/')` → `VideoFileView(attachment: ...)`.
  - Còn lại: không route tới màn này (xem mục 5) — nếu lỡ vào (vd. deep-link cũ), hiện `ErrorStateView` với thông báo "Loại tệp này không hỗ trợ xem trước".

### `ImageGalleryView`
- `PageView.builder(itemCount: images.length, initialPage: initialIndex)`.
- Mỗi trang: `FutureBuilder<Uint8List>` gọi `ref.read(attachmentsProvider(taskId).notifier).downloadAttachment(image.id)`, cache kết quả vào `Map<String, Uint8List> _cache` ở State (không tải lại nếu đã có trong cache khi build lại trang).
- Data → `PhotoView(imageProvider: MemoryImage(bytes))` (pinch-zoom/pan có sẵn từ package).
- Loading → `CircularProgressIndicator`. Error → `ErrorStateView` + nút thử lại (gọi lại `downloadAttachment`, không xoá cache của các trang khác).

### `PdfFileView` / `VideoFileView`
- Tái dùng đúng đoạn tải-về-file-tạm hiện có trong `AttachmentListSection._openAttachment` (`getTemporaryDirectory()` + tạo file `${dir.path}/attachments/${id}_${fileName}` + ghi bytes) — trích thành 1 hàm dùng chung `downloadToTempFile(...)` thay vì copy lại logic.
- `PdfFileView`: `PDFView(filePath: tempFile.path, onError: ..., onPageError: ...)` → lỗi hiện `ErrorStateView`.
- `VideoFileView`: `VideoPlayerController.file(tempFile)` bọc trong `Chewie` (có sẵn control play/pause/seek/fullscreen từ package).
- Cả hai: trạng thái loading khi đang tải file tạm dùng chung 1 widget nhỏ (`Center(child: CircularProgressIndicator())`), lỗi tải dùng `ErrorStateView` + thử lại.

### Sửa `AttachmentListSection`
- `_openAttachment`: nếu `attachment.isImage || attachment.contentType == 'application/pdf' || attachment.contentType.startsWith('video/')` → `context.push('/tasks/${widget.taskId}/attachments/${attachment.id}')`.
- Ngược lại: giữ nguyên đúng logic tải-file-tạm + `OpenFilex.open` hiện tại, không đổi.

## 5. Routing

Thêm route trong `app_router.dart`:
```dart
GoRoute(
  path: '/tasks/:taskId/attachments/:attachmentId',
  name: AttachmentViewerScreen.name,
  builder: (context, state) => AttachmentViewerScreen(
    taskId: state.pathParameters['taskId']!,
    attachmentId: state.pathParameters['attachmentId']!,
  ),
),
```

## 6. Dependencies mới (pubspec.yaml)

`photo_view`, `flutter_pdfview`, `video_player`, `chewie` — cả 4 đều miễn phí, cùng bộ package SuperApp (app tham chiếu trong playbook trước đó) thực tế dùng cho đúng mục đích này.

## 7. Testing

| Test | Case |
|---|---|
| `ImageGalleryView` widget test | N ảnh → N trang; lướt sang trang 2 gọi đúng `downloadAttachment` cho ảnh #2; lướt lại trang 1 KHÔNG gọi tải lại (đếm số lần gọi qua fake repository) |
| `AttachmentViewerScreen` widget test | 3 attachment (ảnh/pdf/video) → route đúng loại `contentType` build đúng widget con tương ứng |
| `PdfFileView`/`VideoFileView` | Không cần test riêng thư viện ngoài (`flutter_pdfview`/`chewie` đã có test của chính package); chỉ test phần tải file tạm + trạng thái loading/error qua fake repository throw exception |

Không có test backend mới — API không đổi (đã verify ở mục 1).

## 8. Rủi ro & lưu ý

- `flutter_pdfview` dùng platform view (native Android/iOS), cần build lại app (không hot-reload được thay đổi native) — bình thường với package loại này, không phải lỗi.
- Ảnh dung lượng lớn tải hết vào `Uint8List` trong RAM (không stream) — chấp nhận được vì giới hạn upload đã có sẵn 25MB/file (`RequestSizeLimit` ở `AttachmentsController`), không cần thêm giới hạn riêng cho viewer.
- Cache ảnh trong `ImageGalleryView` chỉ sống trong vòng đời widget (đóng màn hình là mất) — không cần cache bền vững qua các lần mở, đúng mức độ cần thiết cho tính năng này.
