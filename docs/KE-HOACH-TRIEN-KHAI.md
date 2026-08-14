# Kế hoạch triển khai chi tiết
### Dự án: Hệ thống Quản lý Công việc & Vị trí làm việc (Flutter · .NET 10 · PostgreSQL · Redis · Hangfire · Docker)

| | |
|---|---|
| **Phiên bản** | 1.0 |
| **Ngày tạo** | 28/07/2026 |
| **Tài liệu liên quan** | `PRD.md` (yêu cầu sản phẩm) |
| **Phương pháp quản lý** | Agile/Scrum, sprint 2 tuần, mỗi giai đoạn = 1–2 sprint |

---

## 1. Tổng quan lộ trình

| Giai đoạn | Tên | Thời gian | Tuần | Mục tiêu chính |
|---|---|---|---|---|
| **G0** | Chuẩn bị & Thiết kế nền tảng | 1–2 tuần | Tuần 1–2 | Hạ tầng, khung dự án, quy trình làm việc sẵn sàng |
| **G1** | MVP Core | 3–4 tuần | Tuần 3–6 | Auth, Vị trí, Công việc + công việc con, nhiều người tham gia |
| **G2** | Cộng tác & Lịch sử thay đổi | 2 tuần | Tuần 7–8 | Bình luận, đính kèm tệp, audit trail (timeline) |
| **G3** | Thông báo & Background Job | 2 tuần | Tuần 9–10 | Push notification, realtime, Hangfire, Redis cache |
| **G4** | Dashboard & Hoàn thiện | 2 tuần | Tuần 11–12 | Dashboard, tối ưu hiệu năng, kiểm thử toàn diện, UAT |
| **G5** | Triển khai Production | 1 tuần | Tuần 13 | Đóng gói, deploy, giám sát, phát hành |

> **Tổng thời gian ước tính: ~13 tuần** với đội hình gồm 1 Tech Lead, 1–2 BE Dev, 1–2 FE Flutter Dev, 1 QA (bán thời gian có thể tham gia từ G1). Con số mang tính tham khảo, điều chỉnh theo năng lực đội thực tế.

```
Tuần:        1   2   3   4   5   6   7   8   9  10  11  12  13
G0 Chuẩn bị  ██  ██
G1 MVP Core          ██  ██  ██  ██
G2 Cộng tác                          ██  ██
G3 Notification                              ██  ██
G4 Hoàn thiện                                        ██  ██
G5 Triển khai                                                ██
```

---

## 2. Nguyên tắc & Quy trình làm việc

- **Sprint 2 tuần**: mỗi sprint có sprint planning, daily standup, sprint review/demo, retro.
- **Definition of Done (DoD) chung** cho mọi task tính năng:
  1. Code đã merge vào `develop` qua Pull Request, được ít nhất 1 người review.
  2. Có unit test (tối thiểu cho logic domain/nghiệp vụ quan trọng) và test pass trên CI.
  3. Không phá vỡ build/lint trên pipeline CI.
  4. API mới được cập nhật vào Swagger/Postman collection.
  5. Tính năng đã test thủ công theo kịch bản QA cơ bản (happy path + 1–2 edge case).
- **Branching strategy**: `main` (production) ← `develop` (staging) ← `feature/*`, `bugfix/*`, `hotfix/*` (chi tiết ở mục 4.5 tài liệu cài đặt môi trường).
- Mỗi giai đoạn kết thúc bằng 1 buổi **Demo + Review** với stakeholder để chốt tiếp tục hay điều chỉnh.

---

## 3. Chi tiết từng giai đoạn

### G0 — Chuẩn bị & Thiết kế nền tảng (Tuần 1–2)

**Mục tiêu:** Có nền tảng kỹ thuật và quy trình làm việc sẵn sàng trước khi bắt đầu code tính năng nghiệp vụ.

**Milestone/Đầu ra:**
- Repo Git khởi tạo, branching strategy thống nhất, CI build cơ bản chạy xanh.
- ERD & schema PostgreSQL được review và chốt.
- Khung `.NET solution` (Clean Architecture) build và chạy được endpoint kiểm tra (`/health`).
- Khung Flutter project chạy được "Hello App" trên giả lập Android/iOS.
- `docker-compose.yml` dev chạy được PostgreSQL + Redis local.
- Firebase project được tạo, sẵn sàng cấu hình FCM.

| # | Công việc | Mô tả | Phụ trách |
|---|---|---|---|
| 0.1 | Khởi tạo Git repository trên GitHub, cấu hình branch protection cho `main`/`develop` | Bật required PR review, required CI check | Tech Lead |
| 0.2 | Thiết kế chi tiết ERD, review cùng team, viết migration khởi tạo (EF Core) | Dựa theo mục 6 trong `PRD.md` | BE Lead |
| 0.3 | Dựng khung `.NET solution` theo Clean Architecture (`Domain/Application/Infrastructure/API`) | Cài đặt MediatR, FluentValidation, Serilog, Swagger | BE |
| 0.4 | Dựng khung Flutter project (feature-first, `core/features/shared`) | Cài Riverpod (hoặc Bloc), Dio, DI (get_it) | FE |
| 0.5 | Viết `docker-compose.yml` cho môi trường dev: PostgreSQL + Redis (+ pgAdmin/RedisInsight tuỳ chọn) | Theo hướng dẫn ở `HUONG-DAN-CAI-DAT-MOI-TRUONG-PHAT-TRIEN.md` | DevOps/BE |
| 0.6 | Thiết lập Firebase project, tạo service account key phục vụ FCM | Lưu key vào secret manager/`.env`, không commit vào Git | BE |
| 0.7 | Cấu hình CI (GitHub Actions): tự động build + chạy unit test khi có Pull Request | Riêng pipeline cho BE (.NET) và FE (Flutter) | DevOps |
| 0.8 | Thống nhất coding convention (C# + Dart), checklist code review, commit message convention | Ghi thành `CONTRIBUTING.md` | Cả team |
| 0.9 | Thiết lập Hangfire cơ bản (storage trỏ PostgreSQL) và Redis client (`StackExchange.Redis`) trong project khung, chưa cần job/cache thực tế | Chuẩn bị hạ tầng cho G3 | BE |

**Điều kiện hoàn thành (Exit criteria):** Toàn bộ mục trên hoàn tất; team demo được: chạy `docker compose up`, gọi `GET /health` trả 200, mở app Flutter demo trên emulator.

---

### G1 — MVP Core: Auth, Vị trí, Công việc (Tuần 3–6)

**Mục tiêu:** Xây dựng các chức năng lõi để người dùng đăng nhập và thao tác được với công việc/vị trí/người tham gia — tương ứng FR-1, FR-2, FR-3, FR-4, FR-5 trong `PRD.md`.

**Milestone/Đầu ra:** Người dùng có thể đăng nhập, quản lý vị trí làm việc, tạo công việc kèm công việc con, gán nhiều người tham gia với vai trò khác nhau, xem danh sách và chi tiết công việc trên app Flutter.

| # | Công việc | Mô tả | Phụ trách |
|---|---|---|---|
| 1.1 | API Auth: đăng ký, đăng nhập, refresh token, quên mật khẩu | ASP.NET Core Identity + JWT | BE |
| 1.2 | RBAC middleware/policy theo `SystemRole` (Admin/Manager/Member) | Áp dụng `[Authorize]` theo policy | BE |
| 1.3 | API CRUD Location (+ vị trí phân cấp tuỳ chọn) | Bao gồm validate toạ độ | BE |
| 1.4 | API CRUD WorkTask (bao gồm Subtask qua `ParentTaskId`, giới hạn 3 cấp) | Kèm filter/sort/phân trang | BE |
| 1.5 | API TaskAssignee: thêm/gỡ người tham gia, đổi vai trò (Owner/Assignee/Reviewer/Watcher) | Kiểm tra quyền theo vai trò trong task | BE |
| 1.6 | Unit test cho logic domain quan trọng (giới hạn cấp subtask, validate vai trò Owner duy nhất...) | xUnit/NUnit | BE/QA |
| 1.7 | Màn hình đăng nhập/đăng ký (Flutter) | Kèm xử lý lỗi, lưu token an toàn | FE |
| 1.8 | Màn hình danh sách vị trí + bản đồ (Google Maps/Mapbox) | | FE |
| 1.9 | Màn hình danh sách công việc (filter theo trạng thái/vị trí) + tạo/sửa công việc | | FE |
| 1.10 | Màn hình chi tiết công việc: thông tin chung, danh sách người tham gia, cây công việc con | | FE |
| 1.11 | Viết test case & test thủ công cho Auth + Task CRUD | Checklist happy path & edge case | QA |

**Điều kiện hoàn thành:** Demo được luồng: đăng nhập → tạo vị trí → tạo công việc cha có 2 công việc con → gán 3 người với vai trò khác nhau → xem lại trên app.

---

### G2 — Cộng tác & Lịch sử thay đổi (Tuần 7–8)

**Mục tiêu:** Hoàn thiện tương tác trong công việc và cơ chế audit trail — tương ứng FR-3.6, FR-3.7, FR-6 trong `PRD.md`.

**Milestone/Đầu ra:** Người dùng bình luận, đính kèm tệp trong công việc; mọi thay đổi được ghi lại và hiển thị dạng timeline.

| # | Công việc | Mô tả | Phụ trách |
|---|---|---|---|
| 2.1 | API Comment (thêm/xem bình luận theo task) | Hỗ trợ @mention (lưu dữ liệu, xử lý notification ở G3) | BE |
| 2.2 | API Attachment: upload/xoá tệp, tích hợp Object Storage (S3-compatible/MinIO) | Giới hạn dung lượng/định dạng | BE |
| 2.3 | Domain Event Handler ghi `TaskHistory` cho mọi thay đổi (title, status, assignee, subtask, comment, attachment...) | Theo thiết kế mục 5.2/6 trong `PRD.md` | BE |
| 2.4 | API lấy lịch sử thay đổi theo task (chỉ đọc, có filter theo loại/người) | | BE |
| 2.5 | UI bình luận trong màn hình chi tiết công việc | | FE |
| 2.6 | UI đính kèm/xem tệp (ảnh, PDF...) | | FE |
| 2.7 | UI timeline lịch sử thay đổi | | FE |
| 2.8 | Test case: đảm bảo lịch sử không thể sửa/xoá qua API (kể cả Admin) | | QA |

**Điều kiện hoàn thành:** Demo được: bình luận + đính kèm tệp vào 1 công việc → đổi trạng thái/gán lại người → xem đầy đủ các thay đổi trên timeline.

---

### G3 — Thông báo & Background Job (Tuần 9–10)

**Mục tiêu:** Hoàn thiện hệ thống thông báo thời gian thực và các tác vụ nền — tương ứng FR-7, mục 5.5–5.6 và mục 8 trong `PRD.md`.

**Milestone/Đầu ra:** Người liên quan nhận được push notification + cập nhật realtime khi công việc thay đổi; các job nhắc hạn chạy đúng lịch qua Hangfire; API đọc nhiều đã được tăng tốc bằng Redis cache.

| # | Công việc | Mô tả | Phụ trách |
|---|---|---|---|
| 3.1 | Tích hợp Firebase Admin SDK gửi push (FCM) | API đăng ký/huỷ device token | BE |
| 3.2 | SignalR Hub cho realtime update | Cấu hình Redis backplane (cho khả năng scale) | BE |
| 3.3 | Cấu hình Hangfire Server + Dashboard (`/hangfire`, bảo vệ bằng Admin auth) | Storage dùng PostgreSQL | BE |
| 3.4 | Viết các recurring job: `SendDueSoonReminderJob`, `SendOverdueReminderJob`, `CleanupExpiredTokensJob` | Theo bảng job ở mục 5.6 `PRD.md` | BE |
| 3.5 | Viết `SendPushNotificationJob` (fire-and-forget) gọi từ Notification Event Handler | Có retry policy | BE |
| 3.6 | Tích hợp Redis cache-aside cho: danh sách/chi tiết công việc, danh sách vị trí, đếm thông báo chưa đọc | Theo bảng cache ở mục 5.5 `PRD.md`; đảm bảo invalidate đúng khi có ghi | BE |
| 3.7 | API Notification (danh sách, đánh dấu đã đọc) | | BE |
| 3.8 | Xử lý push phía Flutter: `firebase_messaging` + `flutter_local_notifications` + deep link | | FE |
| 3.9 | Kết nối SignalR client trong Flutter để cập nhật UI realtime khi app mở | | FE |
| 3.10 | UI trung tâm thông báo (in-app notification center) | | FE |
| 3.11 | Test end-to-end: tạo thay đổi → nhận push trên thiết bị thật + cập nhật realtime khi app mở | | QA |

**Điều kiện hoàn thành:** Demo được: đổi trạng thái 1 công việc trên thiết bị A → thiết bị B (đang mở app) thấy cập nhật realtime; đóng app → vẫn nhận được push; theo dõi job nhắc hạn chạy đúng trên Hangfire Dashboard.

---

### G4 — Dashboard & Hoàn thiện (Tuần 11–12)

**Mục tiêu:** Hoàn thiện trải nghiệm tổng thể, tối ưu hiệu năng và đảm bảo chất lượng trước khi lên production — tương ứng FR-8 và mục 4 (NFR) trong `PRD.md`.

**Milestone/Đầu ra:** Dashboard hoạt động, hệ thống đạt các chỉ tiêu hiệu năng đề ra, vượt qua kiểm thử toàn diện và UAT.

| # | Công việc | Mô tả | Phụ trách |
|---|---|---|---|
| 4.1 | API thống kê dashboard (số việc quá hạn/sắp hạn/đang thực hiện...) có cache Redis | | BE |
| 4.2 | Rà soát & tối ưu query PostgreSQL (index, tránh N+1) | Dùng EF Core logging + `EXPLAIN ANALYZE` | BE |
| 4.3 | Rà soát bảo mật: rate-limit, token blacklist Redis, kiểm tra RBAC toàn bộ API | Theo mục 9 `PRD.md` | BE |
| 4.4 | UI Dashboard: thống kê nhanh, danh sách/kanban theo trạng thái | | FE |
| 4.5 | Tối ưu UX: loading state, empty state, xử lý lỗi mạng, đa ngôn ngữ (VI/EN) | | FE |
| 4.6 | Test hiệu năng (load test API cơ bản), kiểm tra chỉ tiêu P95 < 300ms | k6/JMeter | QA/DevOps |
| 4.7 | Regression test toàn bộ chức năng G1–G4 | Theo test plan tổng hợp | QA |
| 4.8 | UAT với người dùng thật/stakeholder, thu thập phản hồi và fix issue ưu tiên cao | | Cả team |

**Điều kiện hoàn thành:** Không còn bug mức Critical/High mở; chỉ tiêu hiệu năng đạt yêu cầu; stakeholder ký duyệt UAT.

---

### G5 — Triển khai Production (Tuần 13)

**Mục tiêu:** Đưa hệ thống lên môi trường thật, sẵn sàng vận hành.

**Milestone/Đầu ra:** Backend chạy ổn định trên staging → production; app Flutter được phát hành (nội bộ hoặc lên store); có giám sát và tài liệu vận hành.

| # | Công việc | Mô tả | Phụ trách |
|---|---|---|---|
| 5.1 | Viết Dockerfile tối ưu cho API (multi-stage build) | | DevOps |
| 5.2 | Chuẩn bị `docker-compose.prod.yml` hoặc manifest Kubernetes (nếu dùng K8s) | Bao gồm API, PostgreSQL, Redis, reverse proxy (Nginx) | DevOps |
| 5.3 | Cấu hình biến môi trường/secret cho production (connection string, Firebase key, JWT secret...) | Qua secret manager | DevOps |
| 5.4 | Chuyển đổi địa chỉ gọi backend thành biến `API_URL`, đọc từ Firebase Remote Config | Cho phép đổi endpoint backend (staging/production) mà không cần build lại app | FE |
| 5.5 | Deploy lên staging, chạy smoke test | | DevOps/QA |
| 5.6 | Deploy lên production, cấu hình domain + HTTPS (Let's Encrypt/SSL cert) | | DevOps |
| 5.7 | Thiết lập giám sát: health-check, log tập trung (Seq/ELK), cảnh báo lỗi (Sentry) | | DevOps |
| 5.8 | Build & phát hành Flutter app: APK/AAB (Android) và TestFlight/App Store (iOS) nếu áp dụng | | FE |
| 5.9 | Viết tài liệu vận hành (runbook): cách xem log, restart service, backup/restore DB | | DevOps |
| 5.10 | Viết release notes, thông báo & đào tạo người dùng cuối | | PM/BA |

**Điều kiện hoàn thành:** Hệ thống chạy ổn định trên production ≥ 48h không có sự cố nghiêm trọng; đội vận hành có thể tự xử lý sự cố cơ bản theo runbook.

---

## 4. Vai trò & nguồn lực đề xuất

| Vai trò | Số lượng đề xuất | Tham gia từ | Trách nhiệm chính |
|---|---|---|---|
| Tech Lead / DevOps | 1 | G0 | Kiến trúc, CI/CD, hạ tầng, deploy |
| Backend Developer (.NET) | 1–2 | G0 | API, business logic, tích hợp Hangfire/Redis/FCM |
| Frontend Developer (Flutter) | 1–2 | G0 (song song) | UI/UX, tích hợp API, push notification client |
| QA/Tester | 1 (bán thời gian G1, toàn thời gian G4) | G1 | Test case, test thủ công, load test, regression |
| PM/BA | 1 (kiêm nhiệm) | Xuyên suốt | Quản lý tiến độ, thu thập yêu cầu, UAT |

---

## 5. Theo dõi rủi ro tiến độ

| Rủi ro tiến độ | Ảnh hưởng | Biện pháp |
|---|---|---|
| Thiết kế DB/API thay đổi giữa chừng ở G1–G2 | Trễ tiến độ các giai đoạn sau | Chốt ERD & API contract ngay từ G0, review kỹ trước khi code |
| Tích hợp FCM/SignalR/Hangfire phức tạp hơn dự kiến ở G3 | Trễ milestone thông báo | Dành buffer 20% thời gian G3, ưu tiên làm luồng push cơ bản trước, nâng cao sau |
| Thiếu thiết bị thật để test push notification | Không phát hiện lỗi sớm | Chuẩn bị tối thiểu 2 thiết bị thật (Android + iOS nếu có) từ đầu G3 |
| Hiệu năng không đạt chỉ tiêu ở G4 | Kéo dài thời gian tối ưu | Theo dõi P95 latency từ sớm (ngay G1) qua log/metrics, không để dồn đến G4 mới đo |

---

*Kế hoạch này là bản tham khảo ban đầu, cần được rà soát lại sau mỗi giai đoạn (retro) để điều chỉnh cho sát thực tế năng lực đội và phát sinh nghiệp vụ.*
