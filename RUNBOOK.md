# Runbook vận hành — TaskMgmt

Tài liệu cho đội vận hành xử lý các tình huống thường gặp trên production. Giả định hệ thống
chạy theo `docker-compose.prod.yml` ở gốc repo, trên 1 server (Docker Compose, chưa dùng
Kubernetes).

## 1. Kiến trúc & các service

| Service | Vai trò | Container |
|---|---|---|
| `nginx` | Reverse proxy, cổng vào duy nhất (80/443) | `taskmgmt-nginx` |
| `api` | Backend .NET (API + Hangfire + SignalR) | `taskmgmt-api` |
| `postgres` | Cơ sở dữ liệu chính | `taskmgmt-postgres` |
| `redis` | Cache + Hangfire không dùng Redis (dùng Postgres storage) + SignalR backplane | `taskmgmt-redis` |
| `seq` | Log tập trung (Serilog ghi tới đây) | `taskmgmt-seq` |

Chỉ `nginx` mở cổng ra ngoài internet. `api`/`postgres`/`redis`/`seq` chỉ giao tiếp nội bộ qua
Docker network, không expose ra host — kể cả Seq, vì log có thể chứa dữ liệu nhạy cảm.

## 2. Xem log

```bash
# Log realtime của 1 service
docker compose -f docker-compose.prod.yml logs -f api

# 200 dòng gần nhất, không theo dõi tiếp
docker compose -f docker-compose.prod.yml logs --tail=200 api

# Toàn bộ service cùng lúc
docker compose -f docker-compose.prod.yml logs -f
```

Log ứng dụng (`Microsoft.*`, `Hangfire.*`, `TaskMgmt.*`) ghi ra stdout/stderr của container `api`
theo cấu hình mặc định ASP.NET Core — không cần cấu hình thêm để xem qua `docker logs`.

**Hangfire Dashboard**: `https://<domain>/hangfire` — xem lịch sử job, job đang chạy/lỗi. Đăng
nhập bằng JWT hợp lệ qua query string `?access_token=<token>` (token hết hạn theo
`Jwt:AccessTokenExpirationMinutes`, mặc định 30 phút — hết hạn thì đăng nhập lại lấy token mới rồi
vào lại link).

**Seq (log tập trung, tìm kiếm/lọc log tiện hơn `docker logs`)**: không public ra internet, mở
qua SSH tunnel từ máy cá nhân:
```bash
ssh -L 5341:localhost:80 <user>@<server>
```
Rồi mở `http://localhost:5341` trên trình duyệt máy mình, đăng nhập bằng
`SEQ_FIRSTRUN_ADMINPASSWORD` đã đặt trong `.env.production`. Có thể lọc theo `StatusCode >= 500`,
`SourceContext like 'TaskMgmt%'`... để nhanh chóng tìm request lỗi.

**Sentry (cảnh báo lỗi real-time)**: nếu đã cấu hình `SENTRY_DSN`, exception chưa được xử lý sẽ tự
động xuất hiện trên dashboard Sentry (sentry.io) kèm stack trace đầy đủ, không cần vào server. Nếu
`SENTRY_DSN` để trống, tính năng này tắt hoàn toàn — dùng Seq/`docker logs` để tra lỗi thay thế.

## 3. Restart service

```bash
# Restart 1 service (không rebuild lại image)
docker compose -f docker-compose.prod.yml restart api

# Restart toàn bộ
docker compose -f docker-compose.prod.yml restart

# Deploy bản mới (rebuild image từ source rồi thay container cũ)
docker compose -f docker-compose.prod.yml up -d --build api
```

Sau khi restart `api`, chờ khoảng 15-30s rồi kiểm tra:
```bash
curl -f https://<domain>/health
```
Trả về `Healthy` (200) là service đã sẵn sàng nhận traffic.

## 4. Backup / restore database

### Backup thủ công
```bash
docker exec taskmgmt-postgres pg_dump -U taskmgmt_user -d taskmgmt_db -F c -f /tmp/backup.dump
docker cp taskmgmt-postgres:/tmp/backup.dump ./backup-$(date +%Y%m%d-%H%M).dump
```

### Backup tự động (khuyến nghị)
Thêm cron job trên server chạy hằng ngày, giữ lại tối thiểu 7 bản gần nhất, lưu ra ngoài server
(S3, ổ đĩa khác...) — mất luôn cả server thì bản backup nằm cùng ổ cũng vô nghĩa.

### Restore
```bash
docker cp ./backup-20260101-0300.dump taskmgmt-postgres:/tmp/restore.dump
docker exec taskmgmt-postgres pg_restore -U taskmgmt_user -d taskmgmt_db --clean --if-exists /tmp/restore.dump
```
`--clean --if-exists` sẽ xoá dữ liệu hiện có trước khi restore — chỉ chạy khi chắc chắn muốn ghi
đè toàn bộ DB hiện tại (vd: sau sự cố mất dữ liệu), không chạy nhầm trên DB đang có dữ liệu thật
cần giữ.

## 5. Áp dụng migration mới

Backend **không** tự động chạy migration khi khởi động — phải áp dụng thủ công trước khi deploy
bản mới, nếu không API sẽ lỗi ngay khi động tới bảng/cột chưa tồn tại.

```bash
# Chạy trên máy có kết nối được tới Postgres production (qua SSH tunnel hoặc trực tiếp trên server)
dotnet ef database update \
  --project backend/src/TaskMgmt.Infrastructure \
  --startup-project backend/src/TaskMgmt.API \
  --connection "Host=<host>;Port=5432;Database=taskmgmt_db;Username=<user>;Password=<pass>"
```

Nên generate SQL script để review trước khi chạy thật trên production, đặc biệt với migration có
khả năng mất dữ liệu (xoá cột, đổi kiểu dữ liệu):
```bash
dotnet ef migrations script --project backend/src/TaskMgmt.Infrastructure --startup-project backend/src/TaskMgmt.API -o review.sql
```

**Thứ tự deploy đúng**: backup DB → chạy `dotnet ef database update` → `docker compose up -d
--build api`. Migration trước, deploy code sau — tránh code mới chạy trên schema cũ.

## 6. Sự cố thường gặp

### `/health` trả về lỗi hoặc không phản hồi
1. `docker compose -f docker-compose.prod.yml ps` — service nào không ở trạng thái `running`?
2. `docker compose -f docker-compose.prod.yml logs --tail=100 api` — tìm exception ngay trước
   thời điểm lỗi.
3. Kiểm tra `postgres`/`redis` có đang chạy không — `api` phụ thuộc cả hai, một trong hai chết
   thì `api` không khởi động được.

### Người dùng không đăng nhập được hàng loạt
- Kiểm tra `postgres` còn sống, còn dung lượng đĩa (`df -h` trên server).
- Nếu vừa đổi `JWT_SECRET`: đúng như mong đợi — toàn bộ token cũ bị vô hiệu ngay, người dùng phải
  đăng nhập lại. Không phải bug.

### Không nhận được push notification
1. Kiểm tra biến `FIREBASE_CREDENTIALS_HOST_PATH` trỏ đúng file JSON còn tồn tại trên server.
2. `docker compose logs api | grep -i firebase` — nếu thấy dòng "Firebase chưa được cấu hình",
   nghĩa là container không đọc được file credentials (sai đường dẫn mount, hoặc quyền đọc file).
3. Push chỉ là lớp thông báo bổ sung — người dùng vẫn thấy đầy đủ trong Trung tâm thông báo trong
   app dù push lỗi, không phải sự cố nghiêm trọng cần xử lý gấp giữa đêm.

### Đĩa server gần đầy
- `docker system df` — xem image/container/volume cũ chiếm bao nhiêu.
- `docker image prune -a` sau khi deploy thành công bản mới (xoá image cũ không còn dùng).
- Log Postgres/container tích luỹ lâu ngày cũng chiếm nhiều dung lượng — cấu hình `logging` driver
  có giới hạn kích thước trong `docker-compose.prod.yml` nếu chưa có.

## 7. Đổi backend URL cho app đã phát hành (không cần build lại app)

Từ mục 5.4: app đọc `api_base_url` từ Firebase Remote Config. Muốn chuyển toàn bộ app đã cài trên
máy người dùng sang trỏ backend khác (vd: đổi server, bảo trì có kế hoạch):
1. Firebase Console → Remote Config → sửa giá trị `api_base_url` (dạng
   `https://api.taskmgmt.example.com`, không có dấu `/` cuối, không có `/api/v1`).
2. Publish thay đổi.
3. App sẽ áp dụng giá trị mới ở lần mở app kế tiếp (do `minimumFetchInterval` đặt 1 giờ — không
   tức thời, cần thời gian lan truyền tuỳ số lượng người dùng).

## 8. Liên hệ khi vượt quá khả năng xử lý cơ bản

Nếu sau khi làm theo mục 6 mà vẫn chưa xác định được nguyên nhân, hoặc sự cố liên quan tới mất dữ
liệu, escalate lên Tech Lead/DevOps thay vì tiếp tục thử — đặc biệt trước khi chạy bất kỳ lệnh nào
có khả năng xoá dữ liệu (`pg_restore --clean`, `docker volume rm`...).
