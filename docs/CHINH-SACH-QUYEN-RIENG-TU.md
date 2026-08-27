# Chính sách quyền riêng tư — TaskMgmt

*Cập nhật lần cuối: 27/08/2026*

Tài liệu này áp dụng cho ứng dụng di động **TaskMgmt** (gói `com.taskmgmt.taskmgmt_app`).
TaskMgmt là công cụ quản lý công việc nhóm nội bộ — tài khoản do quản trị viên tổ chức cấp,
không mở đăng ký công khai cho người ngoài.

## 1. Dữ liệu chúng tôi thu thập

| Loại dữ liệu | Mô tả | Mục đích |
|---|---|---|
| Họ tên, email | Nhập khi đăng ký/được cấp tài khoản | Xác thực đăng nhập, hiển thị danh tính trong nhóm |
| Mật khẩu | Chỉ lưu dạng **băm (hash)**, không bao giờ lưu hoặc truyền dạng chữ thường (plaintext) sau khi tạo | Xác thực đăng nhập |
| Nội dung công việc | Tiêu đề, mô tả, trạng thái, bình luận, lịch sử thay đổi công việc | Chức năng cốt lõi của app |
| Tệp đính kèm | File người dùng chủ động tải lên gắn với 1 công việc | Chia sẻ tài liệu trong nhóm |
| Ảnh đại diện | Ảnh người dùng tự chụp hoặc chọn từ thư viện để đặt làm avatar (tuỳ chọn) | Hiển thị danh tính trong nhóm |
| Token thiết bị (FCM) | Mã định danh thiết bị do Firebase Cloud Messaging cấp | Gửi thông báo đẩy (nhắc hạn, có người nhắc tên, đổi trạng thái công việc...) |
| Nhật ký kỹ thuật | IP, thời điểm request, lỗi hệ thống (phía máy chủ) | Vận hành, khắc phục sự cố, bảo mật |

**Chúng tôi KHÔNG thu thập:**
- Vị trí GPS/thời gian thực của thiết bị người dùng. Tính năng "Vị trí" trong app chỉ hiển thị
  danh sách địa điểm **cố định do quản trị viên tạo sẵn** (ví dụ: văn phòng, công trường) để gán
  cho công việc — không truy cập cảm biến định vị của điện thoại.
- Danh bạ, lịch, thư viện media nói chung (trừ file đính kèm và ảnh đại diện mà người dùng tự chủ
  động chọn qua trình chọn ảnh/máy ảnh — app không tự động quét hay truy cập thư viện ảnh).
- Dữ liệu tài chính, sức khoẻ.
- Không tích hợp SDK quảng cáo hoặc SDK phân tích hành vi người dùng bên thứ ba (không dùng
  Google Analytics/Firebase Analytics).

## 2. Chia sẻ dữ liệu với bên thứ ba

Chúng tôi chỉ dùng một dịch vụ bên thứ ba duy nhất xử lý dữ liệu người dùng:

- **Firebase Cloud Messaging (Google)**: nhận token thiết bị để gửi thông báo đẩy. Google xử lý
  dữ liệu này theo [Chính sách quyền riêng tư của Google](https://policies.google.com/privacy).

Chúng tôi không bán, cho thuê, hoặc chia sẻ dữ liệu người dùng cho bất kỳ bên thứ ba nào khác
ngoài mục đích vận hành app nêu trên.

## 3. Lưu trữ và bảo mật

- Dữ liệu nghiệp vụ (tài khoản, công việc, bình luận) lưu trong cơ sở dữ liệu PostgreSQL do đội
  phát triển vận hành.
- Tệp đính kèm lưu trong kho lưu trữ đối tượng (S3-compatible), dùng khoá lưu trữ ngẫu nhiên
  (không dựa vào tên file gốc) để tránh truy cập trái phép.
- Mật khẩu được băm trước khi lưu, không thể khôi phục ngược lại thành dạng gốc.
- Kết nối giữa app và máy chủ dùng HTTPS/TLS.
- Phiên đăng nhập dùng access token (hết hạn sau thời gian ngắn) và refresh token (có thể bị thu
  hồi khi đăng xuất).

## 4. Quyền của người dùng

Người dùng có quyền:
- Xem thông tin công việc, bình luận, lịch sử liên quan đến mình trong app.
- Đăng xuất để thu hồi phiên đăng nhập trên thiết bị đó.
- **Tự xoá ảnh đại diện bất kỳ lúc nào**: vào Hồ sơ của tôi → Xoá avatar, ảnh bị xoá vĩnh viễn khỏi
  hệ thống ngay lập tức, không cần liên hệ.
- **Yêu cầu xoá tài khoản và dữ liệu cá nhân**: hiện tại app chưa có nút tự xoá tài khoản trong
  ứng dụng. Người dùng gửi yêu cầu qua email liên hệ bên dưới, quản trị viên sẽ xử lý xoá dữ liệu
  trong vòng 30 ngày, trừ dữ liệu bắt buộc phải giữ lại theo quy định pháp luật hoặc nhu cầu vận
  hành nội bộ hợp lý (ví dụ: nhật ký kỹ thuật phục vụ điều tra sự cố bảo mật).

## 5. Trẻ em

TaskMgmt không hướng đến và không cố ý thu thập dữ liệu từ người dùng dưới 13 tuổi. Đây là công
cụ quản lý công việc dành cho nhân sự trong tổ chức.

## 6. Thay đổi chính sách

Chính sách này có thể được cập nhật khi app có thay đổi về tính năng thu thập dữ liệu. Phiên bản
mới nhất luôn được đăng tại địa chỉ công khai đi kèm app trên Google Play.

## 7. Liên hệ

Mọi câu hỏi về quyền riêng tư hoặc yêu cầu xoá dữ liệu, liên hệ: **khuonggau2006@gmail.com**

---

> **Lưu ý dành cho đội phát triển (không phải nội dung công khai)**: Tài liệu này do AI soạn dựa
> trên rà soát code thực tế (không đoán), phản ánh đúng hành vi thu thập dữ liệu tại thời điểm
> viết. Đây **không phải tư vấn pháp lý** — nên có luật sư/người phụ trách pháp lý rà soát trước
> khi công bố chính thức, đặc biệt nếu tổ chức có phạm vi người dùng ở EU (GDPR) hoặc cần điều
> khoản hợp đồng lao động/nội bộ cụ thể hơn.
