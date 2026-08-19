# Upload UX cho tệp đính kèm — Thiết kế

| | |
|---|---|
| **Ngày** | 18/08/2026 |
| **Trạng thái** | Đã duyệt, chờ lập kế hoạch triển khai |
| **Liên quan** | `mobile/taskmgmt_app/lib/features/attachments/` (feature có sẵn, đã có trình xem tệp đính kèm từ spec 17/08), backend không đổi |

## 1. Bối cảnh

`AttachmentListSection._upload()` hiện chỉ gọi thẳng `FilePicker.platform.pickFiles(withData: true)` (đúng 1 file/lần, generic picker của OS) và chỉ hiện spinner mờ (không rõ tiến trình %) trong lúc upload — dù Dio đã hỗ trợ sẵn `onSendProgress` cho multipart POST nhưng chưa được nối vào. Đây là mục cuối cùng còn lại trong nhóm "áp dụng ngay" của `SuperApp Adoption Playbook`.

Backend không cần sửa gì: endpoint `POST /worktasks/{taskId}/attachments` đã nhận multipart form-data sẵn, Dio tự tính % qua header `Content-Length` có sẵn khi gửi.

## 2. Phạm vi

**Trong phạm vi:**
1. Bottom-sheet chọn nguồn: Camera / Thư viện ảnh / Tệp (dùng `image_picker` cho 2 nguồn đầu).
2. Chọn nhiều file cùng lúc khi chọn "Tệp" (`allowMultiple: true`), upload tuần tự từng file.
3. Tiến trình upload thật (%) qua `Dio.onSendProgress`, hiển thị dạng "Đang tải 2/5 · 42%".
4. Lỗi 1 file không dừng hàng đợi — tiếp tục các file còn lại, gộp báo lỗi ở cuối.

**Ngoài phạm vi (quyết định có chủ đích):**
- Multi-select cho Camera/Thư viện ảnh — chỉ "Tệp" được chọn nhiều, đúng theo quyết định đã chốt.
- Nút huỷ upload giữa chừng, tạm dừng/tiếp tục hàng đợi.
- Giữ lại danh sách file lỗi sau khi hàng đợi xong để người dùng bấm thử lại từng file riêng lẻ — hàng đợi xoá sạch sau khi chạy xong, chỉ còn 1 SnackBar tổng hợp.
- Nén/resize ảnh trước khi upload.

## 3. Quyết định kiến trúc: hàng đợi upload là hàm thuần, tách khỏi widget và khỏi picker

`runUploadQueue()` — 1 hàm nhận `List<PickedUpload>` (đã chọn xong, không quan tâm chọn từ đâu) + closure `upload(file, onSendProgress)` + callback `onUpdate(queue)`, xử lý tuần tự + tiến trình + lỗi-không-dừng. Tách khỏi:
- **Widget** (`AttachmentListSection`): chỉ giữ `List<UploadQueueItem> _queue` làm state hiển thị, gọi `runUploadQueue` rồi `setState` trong `onUpdate`.
- **Picker** (`image_picker`/`file_picker`): các plugin này dùng platform channel thật, không mock được trong `flutter_test` (giống `flutter_pdfview`/`video_player` ở tính năng trước) — nên logic "chọn nguồn nào, gọi API nào" cố tình mỏng và không test, còn `runUploadQueue` (giá trị thật của tính năng: tuần tự, tiến trình, chịu lỗi từng phần) được test đầy đủ bằng `test()` thuần.

Đây là closure-based dependency inversion — cùng pattern đã dùng cho `downloadAttachmentToTempFile` ở tính năng trình xem tệp đính kèm trước đó, đã chứng minh hiệu quả cho việc test logic nghiệp vụ tách khỏi I/O thật.

## 4. Component design

### `runUploadQueue` (mới, `presentation/utils/upload_queue.dart`)
```dart
class PickedUpload {
  const PickedUpload({required this.fileName, required this.bytes});
  final String fileName;
  final Uint8List bytes;
}

enum UploadItemStatus { uploading, done, error }

class UploadQueueItem {
  UploadQueueItem({required this.fileName, this.status = UploadItemStatus.uploading, this.progress = 0, this.errorMessage});
  final String fileName;
  UploadItemStatus status;
  double progress;
  String? errorMessage;
}

Future<List<UploadQueueItem>> runUploadQueue({
  required List<PickedUpload> files,
  required Future<void> Function(PickedUpload file, void Function(int sent, int total) onSendProgress) upload,
  required void Function(List<UploadQueueItem> queue) onUpdate,
});
```
Upload tuần tự theo đúng thứ tự `files`; lỗi 1 file → đánh dấu `error` + `errorMessage`, tiếp tục file kế tiếp, không throw ra ngoài. Gọi `onUpdate` sau mỗi lần tiến trình đổi (kể cả từng % nhỏ) và sau mỗi file hoàn tất/lỗi.

### `AttachmentRepository.uploadAttachment` (sửa)
Thêm tham số tuỳ chọn `void Function(int sent, int total)? onSendProgress`, nối xuống `AttachmentRemoteDataSource.uploadAttachment` → `Dio.post(..., onSendProgress: onSendProgress)`. `AttachmentsController.uploadAttachment` (provider) cũng nhận và truyền tiếp tham số này.

### Bottom-sheet chọn nguồn (sửa `attachment_list_section.dart`)
`showModalBottomSheet` 3 lựa chọn (theo pattern `show...Sheet` đã có, vd. `showWorkTaskFormSheet`):
- **Camera**: `ImagePicker().pickImage(source: ImageSource.camera)` → 1 `XFile?` → `PickedUpload(fileName: xfile.name, bytes: await xfile.readAsBytes())`.
- **Thư viện ảnh**: tương tự với `ImageSource.gallery`.
- **Tệp**: `FilePicker.platform.pickFiles(withData: true, allowMultiple: true)` → map từng `PlatformFile` có `bytes != null` thành `PickedUpload`.

Kết quả (`List<PickedUpload>`, có thể 0-1 phần tử cho Camera/Thư viện, 0-N cho Tệp) đưa vào `runUploadQueue`, `onUpdate` gọi `setState(() => _queue = queue)`.

### UI (`attachment_list_section.dart` build method)
- Icon tải lên: `onPressed` đổi từ gọi thẳng `_upload()` (cũ) sang mở bottom-sheet; disable khi `_queue` đang chạy (thay `_isUploading` bằng `_queue.isNotEmpty`).
- Khi `_queue` không rỗng: hiện 1 khối nhỏ phía trên danh sách đính kèm, mỗi dòng `fileName + LinearProgressIndicator(value: item.progress) + '${(item.progress * 100).round()}%'`, dòng đang `error` hiện icon lỗi màu đỏ thay progress bar.
- Sau khi `runUploadQueue` trả về: `setState(() => _queue = [])`; nếu có item `status == error`, gộp `ScaffoldMessenger` 1 SnackBar liệt kê tên file lỗi.

## 5. Testing

| Test | Case |
|---|---|
| `runUploadQueue` (unit, không cần widget tree) | 3 file thành công tuần tự đúng thứ tự; `onSendProgress` giả lập 0%→100% cập nhật đúng `progress` qua `onUpdate`; 1 file lỗi giữa hàng đợi (throw exception) → item đó `status = error` + `errorMessage`, 2 file sau vẫn upload bình thường (không bị bỏ qua); trả về đúng `List<UploadQueueItem>` cuối cùng phản ánh trạng thái từng file |
| `AttachmentListSection` widget test | Gọi thẳng `runUploadQueue` qua fake repository (không qua `image_picker`/`file_picker` thật) để verify UI hiển thị đúng progress bar/% khi `_queue` có item, và xoá `_queue` + hiện SnackBar tổng hợp đúng khi hàng đợi xong có lỗi |

Không sửa test nào phía backend — API không đổi (chỉ thêm `onSendProgress` phía client, backend không biết/không cần biết).

## 6. Dependencies mới

`image_picker: ^1.2.3` — miễn phí, chuẩn cộng đồng Flutter cho camera/thư viện ảnh.

## 7. Rủi ro & lưu ý

- `image_picker` cần khai báo quyền camera/thư viện ảnh trong `AndroidManifest.xml`/`Info.plist` — kiểm tra không trùng/xung đột với quyền đã khai báo sẵn cho `file_picker`/thông báo đẩy.
- `runUploadQueue` gọi `onUpdate` rất thường xuyên khi có nhiều update tiến trình nhỏ (mỗi lần Dio bắn `onSendProgress`) — UI `setState` theo tần suất đó là bình thường với `LinearProgressIndicator`, không cần throttle thêm ở phạm vi tính năng này.
