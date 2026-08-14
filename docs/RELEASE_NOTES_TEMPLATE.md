# Release notes — TaskMgmt

Mẫu dùng chung cho mọi lần phát hành (backend lẫn app Flutter). Copy phần dưới, điền số phiên
bản/ngày tháng, xoá các mục không áp dụng.

---

## v[X.Y.Z] — [DD/MM/YYYY]

### Tóm tắt
[1-2 câu mô tả trọng tâm của bản phát hành này dành cho người không kỹ thuật — vd: "Bổ sung tính
năng bình luận trong công việc và sửa lỗi thông báo bị chậm."]

### Tính năng mới
- [Tên tính năng] — [mô tả ngắn gọn tác động tới người dùng]

### Cải thiện
- [Thay đổi] — [lý do/lợi ích]

### Sửa lỗi
- [Mô tả lỗi đã sửa, viết theo góc nhìn người dùng chứ không phải thuật ngữ code — vd: "Sửa lỗi
  không nhận được thông báo khi mở lại app sau khi để lâu ngoài nền" thay vì "Fixed stale
  unreadNotificationCountProvider on resume"]

### Thay đổi cần lưu ý (breaking changes)
- [Nếu có thay đổi bắt buộc người dùng phải làm gì đó, vd: đăng nhập lại, cập nhật app bắt buộc...]
  Để trống nếu không có.

### Yêu cầu hành động từ đội vận hành
- [ ] Chạy migration DB mới trước khi deploy (xem RUNBOOK.md mục 5) — chỉ tick nếu bản này có
      migration.
- [ ] Cập nhật biến môi trường mới (liệt kê nếu có)
- [ ] Publish thay đổi Remote Config nếu có đổi `api_base_url`

### Phiên bản build
- Backend: `[git commit hash hoặc tag]`
- App Android: versionCode `[N]`, versionName `[X.Y.Z]`
- App iOS: build `[N]`, version `[X.Y.Z]` (nếu áp dụng)

---

## Ví dụ đã điền (v0.4.0 — 14/08/2026)

### Tóm tắt
Hoàn thiện Dashboard thống kê, tối ưu hiệu năng, và chuẩn bị sẵn sàng triển khai production.

### Tính năng mới
- Dashboard thống kê nhanh: tổng số việc, đang thực hiện, quá hạn, sắp đến hạn + bảng Kanban theo
  trạng thái.
- Đổi được backend URL từ xa qua Firebase Remote Config, không cần phát hành lại app.

### Cải thiện
- Thêm rate-limiting cho API đăng nhập/đăng ký, chống brute-force.
- Thu hồi refresh token khi đăng xuất — tài khoản không còn hoạt động vô thời hạn sau khi người
  dùng bấm đăng xuất.

### Sửa lỗi
- Sửa lỗi số thông báo chưa đọc (badge chuông) không tự cập nhật khi mở lại app sau khi để ở nền
  một thời gian.
- Sửa lỗi tệp đính kèm bấm mở không phản hồi mà không rõ lý do.

### Yêu cầu hành động từ đội vận hành
- [x] Chạy migration `AddPerformanceIndexes` trước khi deploy.
- [ ] Không có biến môi trường mới.
