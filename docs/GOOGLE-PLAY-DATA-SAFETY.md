# Hướng dẫn điền mục "Data safety" trên Google Play Console

Áp dụng cho app TaskMgmt (`com.taskmgmt.taskmgmt_app`). Play Console form chỉ có tiếng Anh nên
bảng dưới giữ nguyên nhãn gốc, kèm giải thích tiếng Việt và giá trị nên chọn. Dựa trên rà soát
thực tế code app tại thời điểm viết (14/08/2026) — nếu sau này thêm SDK/tính năng mới (quảng cáo,
analytics, chụp ảnh trực tiếp...) phải cập nhật lại form này trước khi nộp bản build mới.

Vào: Play Console → app → **Policy → App content → Data safety → Manage**.

## Bước 1 — Data collection and security

| Câu hỏi | Trả lời |
|---|---|
| Does your app collect or share any of the required user data types? | **Yes** |
| Is all of the user data collected by your app encrypted in transit? | **Yes** (bắt buộc phải hoàn tất HTTPS ở mục 5.6 trước khi publish thật — xem [RUNBOOK.md](../RUNBOOK.md)) |
| Do you provide a way for users to request that their data is deleted? | **Yes** → link tới trang chính sách quyền riêng tư (mục 4 trong [CHINH-SACH-QUYEN-RIENG-TU.md](CHINH-SACH-QUYEN-RIENG-TU.md), có email liên hệ) |

## Bước 2 — Data types

Chỉ tick đúng các mục sau, các mục khác (Location, Financial info, Health and fitness, Photos and
videos, Audio files, Contacts, Calendar, Web browsing, Search history) để **trống/No**.

### Personal info
- **Name**: Collected ✅ / Shared ❌ / Optional: **No** (bắt buộc) / Purpose: **App functionality, Account management**
- **Email address**: Collected ✅ / Shared ❌ / Optional: **No** (bắt buộc) / Purpose: **App functionality, Account management**

### Files and docs
- **Files and docs**: Collected ✅ / Shared ❌ / Optional: **Yes** (người dùng chọn có đính kèm hay không) / Purpose: **App functionality**

### App activity
- **Other user-generated content** (tiêu đề/mô tả công việc, bình luận): Collected ✅ / Shared ❌ / Optional: **No** / Purpose: **App functionality**

### Device or other IDs
- **Device or other IDs** (FCM device token): Collected ✅ / Shared: **✅ Yes, với Google (Firebase Cloud Messaging)** / Purpose: **App functionality** (gửi thông báo đẩy)
  - Khi khai "Shared", Play Console sẽ hỏi thêm bên nhận dữ liệu — chọn **Google** hoặc mục tương
    đương "Firebase" nếu Play Console gợi ý sẵn (Play Console gần đây có tính năng tự nhận diện
    SDK Firebase đã khai báo trong AAB và tự gợi ý điền — nên dùng tính năng đó nếu form hiện ra
    thay vì tự điền tay, vì wording/cấu trúc form có thể đổi theo thời gian).

## Bước 3 — Các câu hỏi phụ thường gặp cho từng loại dữ liệu đã tick

Với **Name** và **Email address**:
- "Is this data processed ephemerally?" → **No**
- "Is this data required or optional?" → **Required**

Với **Files and docs**:
- "Is this data processed ephemerally?" → **No**
- "Is this data required or optional?" → **Optional**

Với **Other user-generated content**:
- "Is this data required or optional?" → **Required** (đây là nội dung cốt lõi của app quản lý
  công việc)

Với **Device or other IDs**:
- "Is this data processed ephemerally?" → **No**
- "Is this data required or optional?" → **Required** (để nhận thông báo đẩy; nếu sau này thêm
  tuỳ chọn tắt thông báo hoàn toàn trong app thì đổi thành Optional)

## Bước 4 — Privacy policy URL

Điền URL công khai trỏ tới nội dung [CHINH-SACH-QUYEN-RIENG-TU.md](CHINH-SACH-QUYEN-RIENG-TU.md).
File markdown trong repo **không tự có URL công khai** — cần host ở một trong các nơi sau trước
khi điền vào Play Console:
1. GitHub Pages của repo này (miễn phí, nhanh nhất nếu repo public).
2. Trang tĩnh dưới domain production thật khi đã mua domain (ví dụ `https://<domain>/privacy`).
3. Google Sites (miễn phí, không cần domain riêng).

## Ghi chú quan trọng

- "Yêu cầu xoá dữ liệu" hiện tại **xử lý thủ công** (chưa có nút tự xoá tài khoản trong app/API).
  Khi khai "Yes" ở Play Console, đội vận hành phải thực sự xử lý được yêu cầu qua email trong
  vòng thời gian hợp lý — đây là cam kết vận hành, không phải tính năng có sẵn trong code.
- Nếu sau này thêm tính năng mới có truy cập dữ liệu nhạy cảm hơn (camera, vị trí GPS thời gian
  thực, danh bạ...), **phải cập nhật lại cả Data safety form và Privacy Policy trước khi phát
  hành bản cập nhật đó** — đây là yêu cầu bắt buộc của Google Play, không cập nhật có thể bị gỡ
  app.
