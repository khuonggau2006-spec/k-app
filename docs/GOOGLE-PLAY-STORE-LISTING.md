# Nội dung listing Google Play — TaskMgmt

Nội dung soạn sẵn để dán trực tiếp vào Play Console khi tạo listing (**Store presence → Main store
listing**). Dựa trên tính năng thực tế của app (rà soát code), không phóng đại.

## 1. Tên app (App name) — tối đa 30 ký tự

```
TaskMgmt - Quản lý công việc
```
(28 ký tự)

## 2. Mô tả ngắn (Short description) — tối đa 80 ký tự

```
Giao việc, theo dõi tiến độ và nhận thông báo tức thời cho cả nhóm.
```
(68 ký tự)

## 3. Mô tả đầy đủ (Full description) — tối đa 4000 ký tự

```
TaskMgmt là công cụ quản lý công việc nhóm gọn nhẹ, giúp cả nhóm giao việc, theo dõi tiến độ và
trao đổi ngay trong từng công việc — không cần chuyển qua lại giữa nhiều ứng dụng.

TÍNH NĂNG CHÍNH

• Quản lý công việc: tạo, giao việc, đặt hạn hoàn thành, theo dõi trạng thái (Cần làm → Đang làm
  → Đang xem xét → Hoàn thành), chia công việc lớn thành công việc con.

• Cộng tác trong từng công việc: thêm nhiều người tham gia với vai trò Owner/Assignee riêng, bình
  luận và nhắc tên (@) để thông báo đúng người cần biết.

• Thông báo tức thời: nhận thông báo ngay khi có người thêm mình vào việc, công việc đổi trạng
  thái, có bình luận mới, bị nhắc tên, hoặc công việc sắp/đã quá hạn — kể cả khi không mở app.

• Đính kèm tài liệu: gắn file trực tiếp vào công việc để cả nhóm cùng xem, không cần gửi qua kênh
  khác.

• Lịch sử thay đổi: mọi thay đổi trên một công việc được ghi lại đầy đủ, minh bạch, không thể sửa
  hay xoá — dễ tra cứu khi cần biết "ai đã làm gì, lúc nào".

• Dashboard trực quan: xem nhanh tổng số việc, việc đang thực hiện, việc quá hạn, việc sắp đến
  hạn, và bảng Kanban chia theo trạng thái.

• Gán địa điểm: liên kết công việc với địa điểm cụ thể (văn phòng, công trường...) để dễ tra cứu
  theo khu vực làm việc.

• Phân quyền theo vai trò: Member, Manager, Admin — mỗi vai trò có quyền hạn phù hợp, tách bạch
  giữa xem, sửa và quản trị hệ thống.

DÀNH CHO AI

TaskMgmt phù hợp với các nhóm/tổ chức cần một nơi tập trung để giao việc và theo dõi tiến độ, thay
vì rải rác qua tin nhắn hoặc bảng tính. Tài khoản được quản trị viên tổ chức cấp — không mở đăng ký
công khai.

Mọi câu hỏi hoặc góp ý, liên hệ: khuonggau2006@gmail.com
```

## 4. Phân loại & thông tin khác

| Trường | Giá trị đề xuất |
|---|---|
| Category (danh mục) | **Business** hoặc **Productivity** — chọn Productivity vì trọng tâm là quản lý công việc cá nhân/nhóm |
| Tags | quản lý công việc, task management, cộng tác nhóm, năng suất |
| Content rating | Everyone (không có nội dung nhạy cảm — điền content rating questionnaire trung thực trên Play Console, đây chỉ là dự đoán kết quả) |
| Contact email | khuonggau2006@gmail.com |
| Privacy policy URL | Xem [GOOGLE-PLAY-DATA-SAFETY.md](GOOGLE-PLAY-DATA-SAFETY.md) mục 4 |
| Website | (chưa có — bỏ trống nếu chưa có domain) |

## 5. Assets hình ảnh

Đã tạo sẵn trong [`docs/store-assets/`](store-assets/):

| File | Kích thước | Dùng cho |
|---|---|---|
| `play-store-icon-512.png` | 512×512 | Store listing icon (bắt buộc) |
| `feature-graphic-1024x500.png` | 1024×500 | Feature graphic (bắt buộc) |

Icon launcher thật trong app (mipmap + adaptive icon) cũng đã được thay từ logo Flutter mặc định
sang icon TaskMgmt thật — đã build và verify trên emulator (icon hiển thị đúng, không bị cắt bởi
mask tròn của adaptive icon).

**Còn thiếu — cần làm gần ngày phát hành thật:**
- **Screenshots** (2-8 ảnh, khuyến nghị theo tỷ lệ điện thoại): chưa chụp vì dữ liệu hiện tại trên
  môi trường dev/QA là dữ liệu test lộn xộn ("Task moi test invalidate cache"...), không phù hợp
  hiển thị công khai. Cần tạo vài công việc mẫu sạch/thực tế (ví dụ: "Chuẩn bị báo cáo tuần",
  "Kiểm tra thiết bị tại Chi nhánh 1"...) trên **backend production thật** rồi chụp lại từ app, gần
  sát ngày nộp lên Play Console để tránh dữ liệu lỗi thời.
