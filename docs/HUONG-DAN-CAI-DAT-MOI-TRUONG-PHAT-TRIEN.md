# Hướng dẫn cài đặt môi trường phát triển & công cụ phụ trợ
### Dự án: Hệ thống Quản lý Công việc & Vị trí làm việc (Flutter · .NET 10 · PostgreSQL · Redis · Hangfire · Docker)

Tài liệu này hướng dẫn cài đặt **từ đầu, từng bước** toàn bộ công cụ cần thiết để bắt đầu code cho dự án, dành cho máy **Windows** hoặc **macOS** (Linux dùng lệnh tương tự macOS ở phần Terminal). Làm theo đúng thứ tự các phần để tránh thiếu phụ thuộc.

## Checklist tổng quan

| # | Công cụ | Mục đích | Bắt buộc |
|---|---|---|---|
| 1 | .NET 10 SDK + Visual Studio 2022 hoặc VS Code | Code & chạy Backend API | ✅ |
| 2 | Flutter SDK + Android Studio (+ Xcode nếu dùng Mac) | Code & chạy App mobile | ✅ |
| 3 | Docker Desktop / Docker Engine + Docker Compose | Chạy PostgreSQL, Redis, và toàn bộ hệ thống local | ✅ |
| 4 | Git + tài khoản GitHub | Quản lý mã nguồn | ✅ |
| 5 | Postman | Test API backend | ✅ |
| 6 | DBeaver hoặc pgAdmin | Xem/thao tác dữ liệu PostgreSQL trực quan | Khuyến nghị |
| 7 | RedisInsight | Xem/thao tác dữ liệu Redis trực quan | Khuyến nghị |

---

## Phần 1 — .NET 10 SDK và IDE

### 1.1. Cài đặt .NET 10 SDK

**Windows:**
1. Truy cập trang chính thức: `https://dotnet.microsoft.com/download/dotnet/10.0`.
2. Tải bản **SDK x64** (không phải Runtime — SDK mới có đủ công cụ để build/chạy code).
3. Chạy file `.exe` vừa tải, nhấn **Install**, chờ hoàn tất.
4. Hoặc dùng `winget` trong PowerShell (cách nhanh hơn):
   ```powershell
   winget install Microsoft.DotNet.SDK.10
   ```

**macOS:**
```bash
brew install --cask dotnet-sdk
```
Hoặc tải trực tiếp bản `.pkg` từ trang chính thức và cài như phần mềm thông thường.

**Kiểm tra cài đặt thành công** (mở Terminal/PowerShell mới):
```bash
dotnet --version
# Kết quả mong đợi: 10.0.x
dotnet --list-sdks
```

### 1.2. Chọn IDE — Visual Studio 2022 (khuyến nghị cho Windows) hoặc VS Code (đa nền tảng)

**Phương án A — Visual Studio 2022 (Windows, đầy đủ tính năng nhất cho .NET):**
1. Tải Visual Studio 2022 Community (miễn phí) tại `https://visualstudio.microsoft.com/vs/`.
2. Chạy Visual Studio Installer, chọn workload **ASP.NET and web development**.
3. Trong tab **Individual components**, đảm bảo tick chọn **.NET 10 SDK** (nếu chưa tự nhận từ bước 1.1).
4. Nhấn **Install**, chờ tải xong (~15–30 phút tuỳ mạng).
5. Mở Visual Studio → **Create a new project** → chọn **ASP.NET Core Web API** → chọn Framework **.NET 10.0** để kiểm tra IDE đã nhận SDK.

**Phương án B — Visual Studio Code (Windows/macOS/Linux, nhẹ hơn):**
1. Tải VS Code tại `https://code.visualstudio.com/`.
2. Cài đặt như ứng dụng thông thường.
3. Mở VS Code → vào tab **Extensions** (biểu tượng ô vuông bên trái) → cài các extension:
   - **C# Dev Kit** (Microsoft) — bắt buộc, hỗ trợ IntelliSense, debug, chạy project .NET.
   - **C#** (Microsoft) — tự động cài kèm C# Dev Kit.
   - **NuGet Package Manager** (tuỳ chọn, quản lý package qua UI).
4. Mở Terminal tích hợp trong VS Code (`Ctrl/Cmd + \``), thử tạo project test:
   ```bash
   dotnet new webapi -n TestApi
   cd TestApi
   dotnet run
   ```
   Nếu thấy API chạy ở `https://localhost:xxxx/swagger` là thành công.

> **Gợi ý chọn:** Nếu bạn dùng Windows và làm việc chuyên sâu với backend .NET → chọn **Visual Studio 2022**. Nếu dùng macOS/Linux, hoặc muốn 1 IDE dùng chung cho cả .NET lẫn Flutter → chọn **VS Code**.

---

## Phần 2 — Flutter SDK, Android Studio và Xcode

### 2.1. Cài đặt Flutter SDK

**Windows:**
1. Tải Flutter SDK (bản zip) tại `https://docs.flutter.dev/get-started/install/windows`.
2. Giải nén vào thư mục **không chứa dấu cách và không cần quyền admin**, ví dụ: `C:\src\flutter`.
3. Thêm Flutter vào biến môi trường `PATH`:
   - Mở **Edit environment variables** → **Environment Variables** → chọn `Path` → **Edit** → **New** → thêm `C:\src\flutter\bin`.
4. Mở PowerShell mới, kiểm tra:
   ```powershell
   flutter --version
   ```

**macOS:**
```bash
brew install --cask flutter
```
Hoặc tải bản zip tại `https://docs.flutter.dev/get-started/install/macos`, giải nén, thêm vào `PATH` trong `~/.zshrc` hoặc `~/.bash_profile`:
```bash
export PATH="$PATH:/đường-dẫn-tới/flutter/bin"
```
Sau đó chạy `source ~/.zshrc` (hoặc mở lại Terminal).

> Dart SDK **đã được đóng gói sẵn** trong Flutter SDK, không cần cài riêng.

### 2.2. Cài đặt Android Studio (bắt buộc cho cả Windows/macOS/Linux — dùng để build Android & lấy Android SDK)

1. Tải tại `https://developer.android.com/studio`, cài đặt theo trình cài đặt mặc định (Standard installation).
2. Mở Android Studio lần đầu → đi qua **Setup Wizard** → để mặc định, nó sẽ tự tải **Android SDK**, **Android SDK Platform-Tools**, **Android Virtual Device**.
3. Vào **More Actions → SDK Manager** (hoặc **Settings → Languages & Frameworks → Android SDK**):
   - Tab **SDK Platforms**: tick chọn phiên bản Android mới nhất (vd. Android 15/API 35) và phiên bản tối thiểu dự án hỗ trợ (API 26 theo NFR trong PRD).
   - Tab **SDK Tools**: đảm bảo tick **Android SDK Build-Tools**, **Android Emulator**, **Android SDK Platform-Tools**.
4. Cài plugin Flutter/Dart cho Android Studio:
   - **Settings → Plugins** → tìm **Flutter** → **Install** (sẽ tự cài kèm plugin **Dart**).
5. Tạo máy ảo Android (AVD) để chạy thử app:
   - **More Actions → Virtual Device Manager → Create Device** → chọn dòng máy (vd. Pixel 7) → chọn system image (vd. Android 14) → **Finish**.
6. Chấp nhận license Android SDK (bắt buộc, nếu không `flutter doctor` sẽ báo lỗi):
   ```bash
   flutter doctor --android-licenses
   ```
   Gõ `y` để đồng ý tất cả.

### 2.3. Cài đặt Xcode (chỉ dành cho macOS — bắt buộc nếu muốn build/test app iOS)

1. Mở **App Store** trên Mac → tìm **Xcode** → **Get/Install** (dung lượng lớn, ~10–15GB, nên cài khi có Wi-Fi ổn định).
2. Sau khi cài xong, mở Terminal, cài Command Line Tools:
   ```bash
   sudo xcode-select --install
   sudo xcodebuild -license accept
   ```
3. Mở Xcode lần đầu để nó cài thêm component cần thiết (chọn **Install** khi được hỏi).
4. Cài **CocoaPods** (quản lý dependency native cho iOS, Flutter cần dùng):
   ```bash
   sudo gem install cocoapods
   ```
5. Cài Simulator để test: mở Xcode → **Settings → Platforms** → tải thêm iOS Simulator runtime nếu cần.

> Nếu bạn dùng Windows/Linux, **bỏ qua bước 2.3** — không thể build iOS trên các hệ điều hành này (đây là giới hạn của Apple, không phải của dự án). Vẫn có thể phát triển phần logic chung và test trên Android; khi cần build iOS, dùng máy Mac (của đồng đội hoặc CI cloud như Codemagic).

### 2.4. Kiểm tra toàn bộ môi trường Flutter

Chạy lệnh chẩn đoán tổng hợp:
```bash
flutter doctor -v
```
Kết quả tốt sẽ có dấu ✅ (hoặc `[✓]`) ở các mục: Flutter, Android toolchain, Xcode (nếu trên Mac), Chrome (nếu dùng cho web), Android Studio, VS Code (nếu có cài extension), Connected device. Nếu có dấu `[!]` hoặc `[✗]`, đọc kỹ gợi ý lệnh sửa mà `flutter doctor` in ra và làm theo.

**Chạy thử app mẫu:**
```bash
flutter create demo_app
cd demo_app
flutter run
```
Chọn thiết bị/emulator khi được hỏi — nếu app "Counter" mặc định chạy được là môi trường đã sẵn sàng.

---

## Phần 3 — Docker & Docker Compose, chạy PostgreSQL và Redis

### 3.1. Cài đặt Docker

**Windows/macOS:**
1. Tải **Docker Desktop** tại `https://www.docker.com/products/docker-desktop/`.
2. Cài đặt như ứng dụng thông thường; trên Windows cần bật **WSL2** nếu được yêu cầu (Docker Desktop sẽ tự hướng dẫn/tải).
3. Mở Docker Desktop, chờ biểu tượng cá voi ở khay hệ thống chuyển sang trạng thái "running".

**Linux (Ubuntu ví dụ):**
```bash
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER
# đăng xuất/đăng nhập lại để áp dụng quyền
```

**Kiểm tra cài đặt:**
```bash
docker --version
docker compose version
docker run hello-world
```
Nếu lệnh `hello-world` in ra thông báo chào mừng là Docker đã hoạt động đúng.

### 3.2. Tạo file `docker-compose.yml` cho môi trường dev

Tạo file `docker-compose.yml` ở thư mục gốc dự án (ví dụ cạnh thư mục `backend/` và `mobile/`):

```yaml
version: "3.9"

services:
  postgres:
    image: postgres:16
    container_name: taskmgmt-postgres
    restart: unless-stopped
    environment:
      POSTGRES_USER: taskmgmt_user
      POSTGRES_PASSWORD: taskmgmt_pass
      POSTGRES_DB: taskmgmt_db
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  redis:
    image: redis:7-alpine
    container_name: taskmgmt-redis
    restart: unless-stopped
    command: ["redis-server", "--requirepass", "redis_pass"]
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data

  pgadmin:
    image: dpage/pgadmin4:latest
    container_name: taskmgmt-pgadmin
    restart: unless-stopped
    environment:
      PGADMIN_DEFAULT_EMAIL: admin@taskmgmt.local
      PGADMIN_DEFAULT_PASSWORD: admin_pass
    ports:
      - "5050:80"
    depends_on:
      - postgres

  redisinsight:
    image: redis/redisinsight:latest
    container_name: taskmgmt-redisinsight
    restart: unless-stopped
    ports:
      - "5540:5540"
    depends_on:
      - redis

volumes:
  postgres_data:
  redis_data:
```

> `pgadmin` và `redisinsight` là **tuỳ chọn** — nếu bạn định cài bản desktop của DBeaver/RedisInsight riêng (mục Phần 5) thì có thể bỏ 2 service này khỏi file để nhẹ máy hơn. Mật khẩu mẫu ở trên chỉ dùng cho **local dev**, không dùng cho staging/production.

### 3.3. Chạy và kiểm tra

Tại thư mục chứa `docker-compose.yml`:
```bash
# Chạy toàn bộ service ở chế độ nền
docker compose up -d

# Xem danh sách container đang chạy
docker compose ps

# Xem log của 1 service (vd. postgres) khi cần debug
docker compose logs -f postgres

# Dừng toàn bộ (giữ lại dữ liệu trong volume)
docker compose down

# Dừng và xoá luôn dữ liệu (dùng khi cần làm sạch hoàn toàn)
docker compose down -v
```

Kết quả `docker compose ps` cần thấy 4 container (`postgres`, `redis`, `pgadmin`, `redisinsight`) ở trạng thái `running`/`healthy`.

**Thông tin kết nối để cấu hình trong `appsettings.Development.json` của Backend:**
```json
{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Port=5432;Database=taskmgmt_db;Username=taskmgmt_user;Password=taskmgmt_pass",
    "Redis": "localhost:6379,password=redis_pass"
  }
}
```

---

## Phần 4 — Git & GitHub

### 4.1. Cài đặt Git

**Windows:** Tải tại `https://git-scm.com/download/win`, cài theo mặc định (giữ nguyên các lựa chọn khuyến nghị của trình cài đặt).

**macOS:**
```bash
brew install git
```

**Kiểm tra:**
```bash
git --version
```

### 4.2. Cấu hình thông tin cá nhân (bắt buộc trước khi commit)

```bash
git config --global user.name "Tên của bạn"
git config --global user.email "email-gan-voi-github@example.com"
git config --global init.defaultBranch main
git config --global core.autocrlf input   # macOS/Linux
# git config --global core.autocrlf true  # Windows
```

### 4.3. Tạo SSH Key và liên kết với GitHub (khuyến nghị thay vì dùng HTTPS + password)

1. Tạo SSH key mới:
   ```bash
   ssh-keygen -t ed25519 -C "email-gan-voi-github@example.com"
   ```
   Nhấn Enter để chấp nhận đường dẫn mặc định, có thể đặt passphrase hoặc để trống.
2. Copy public key:
   ```bash
   # macOS
   pbcopy < ~/.ssh/id_ed25519.pub
   # Windows (PowerShell)
   Get-Content $env:USERPROFILE\.ssh\id_ed25519.pub | Set-Clipboard
   ```
3. Vào GitHub → **Settings → SSH and GPG keys → New SSH key** → dán key vừa copy → **Add SSH key**.
4. Kiểm tra kết nối:
   ```bash
   ssh -T git@github.com
   ```
   Thấy dòng `Hi <username>! You've successfully authenticated...` là thành công.

### 4.4. Clone repository dự án và cấu hình remote

```bash
git clone git@github.com:<ten-to-chuc>/<ten-repo>.git
cd <ten-repo>
git remote -v   # kiểm tra remote "origin" đã trỏ đúng repo
```

### 4.5. Quy ước làm việc với branch (khớp với `KE-HOACH-TRIEN-KHAI.md`)

| Nhánh | Mục đích |
|---|---|
| `main` | Code đang chạy production, chỉ merge qua PR đã duyệt |
| `develop` | Code tích hợp cho môi trường staging |
| `feature/<ten-tinh-nang>` | Nhánh phát triển tính năng mới, tách từ `develop` |
| `bugfix/<mo-ta>` | Sửa lỗi phát hiện ở staging |
| `hotfix/<mo-ta>` | Sửa lỗi khẩn cấp trực tiếp trên `main` |

**Quy trình làm việc hàng ngày:**
```bash
git checkout develop
git pull origin develop
git checkout -b feature/task-crud-api

# ... code, commit theo Conventional Commits ...
git add .
git commit -m "feat(task): thêm API tạo công việc"

git push -u origin feature/task-crud-api
# Sau đó tạo Pull Request trên GitHub từ feature/task-crud-api → develop
```

**Chuẩn commit message gợi ý (Conventional Commits):** `feat: `, `fix: `, `docs: `, `refactor: `, `test: `, `chore: ` — giúp lịch sử commit dễ đọc và có thể tự sinh changelog sau này.

### 4.6. (Tuỳ chọn) GitHub Desktop

Nếu chưa quen dòng lệnh Git, có thể cài **GitHub Desktop** (`https://desktop.github.com/`) — cung cấp giao diện kéo-thả cho các thao tác commit/push/pull/tạo branch, phù hợp cho người mới làm quen Git.

---

## Phần 5 — Postman và công cụ quản lý Database/Redis trực quan

### 5.1. Postman — Test API Backend

1. Tải tại `https://www.postman.com/downloads/`, cài đặt như ứng dụng thông thường (có thể dùng luôn bản web nếu không muốn cài desktop).
2. Đăng nhập/tạo tài khoản Postman (miễn phí) để đồng bộ và chia sẻ collection với team.
3. Tạo **Workspace** riêng cho dự án, ví dụ: `TaskMgmt - Team`.
4. Tạo **Environment** để lưu biến dùng chung, ví dụ `Local`:
   | Biến | Giá trị |
   |---|---|
   | `base_url` | `https://localhost:7000/api/v1` (theo port thật của API khi chạy local) |
   | `access_token` | (để trống, sẽ set tự động sau khi gọi API login) |
5. Tạo **Collection** đặt tên `TaskMgmt API`, nhóm request theo module: `Auth`, `Locations`, `Tasks`, `Notifications`...
6. Với các API cần xác thực, vào tab **Authorization** của collection → chọn **Bearer Token** → dùng biến `{{access_token}}`.
7. Có thể viết **Test script** đơn giản ở tab **Tests** của request login để tự động lưu token vào biến môi trường:
   ```javascript
   const res = pm.response.json();
   pm.environment.set("access_token", res.accessToken);
   ```
8. Khi API đã có Swagger (tự sinh từ code), có thể **Import** trực tiếp file `swagger.json`/OpenAPI URL vào Postman để tự tạo sẵn toàn bộ request (**Import → Link** → dán `https://localhost:7000/swagger/v1/swagger.json`).

### 5.2. DBeaver hoặc pgAdmin — Quản lý PostgreSQL trực quan

**Phương án A — DBeaver (khuyến nghị, hỗ trợ đa hệ CSDL kể cả Redis dạng plugin):**
1. Tải bản Community (miễn phí) tại `https://dbeaver.io/download/`, cài đặt theo mặc định.
2. Mở DBeaver → **New Database Connection** → chọn **PostgreSQL** → **Next**.
3. Điền thông tin kết nối (khớp với `docker-compose.yml` ở Phần 3):
   - Host: `localhost`
   - Port: `5432`
   - Database: `taskmgmt_db`
   - Username: `taskmgmt_user`
   - Password: `taskmgmt_pass`
4. Nhấn **Test Connection** (lần đầu DBeaver sẽ hỏi tải driver PostgreSQL, chọn **Download**) → thấy "Connected" là thành công → **Finish**.
5. Giờ có thể duyệt bảng, chạy SQL query trực tiếp qua giao diện.

**Phương án B — pgAdmin (chính chủ PostgreSQL):**
- Nếu đã bật service `pgadmin` trong `docker-compose.yml` ở Phần 3: mở trình duyệt tại `http://localhost:5050`, đăng nhập bằng `PGADMIN_DEFAULT_EMAIL`/`PGADMIN_DEFAULT_PASSWORD` đã khai báo → **Add New Server** → điền Host `postgres` (tên service trong Docker network), Port `5432`, username/password như trên.
- Hoặc cài bản desktop tại `https://www.pgadmin.org/download/`.

### 5.3. RedisInsight — Quản lý Redis trực quan

**Cách 1 — Dùng container đã khai báo trong `docker-compose.yml`:**
1. Sau khi `docker compose up -d`, mở trình duyệt tại `http://localhost:5540`.
2. Chọn **Add Redis database** → điền:
   - Host: `redis` (tên service, nếu RedisInsight và Redis chạy chung Docker network) hoặc `localhost` nếu truy cập từ máy host.
   - Port: `6379`
   - Password: `redis_pass`
3. Nhấn **Add** → thấy danh sách key hiện ra là kết nối thành công.

**Cách 2 — Cài bản desktop:** Tải tại `https://redis.io/insight/` (chọn hệ điều hành phù hợp), cài đặt và kết nối tương tự với Host `localhost`.

---

## Tổng kết — Bảng lệnh kiểm tra nhanh

Sau khi hoàn tất tất cả các phần trên, chạy nhanh các lệnh sau để xác nhận môi trường đã sẵn sàng 100%:

```bash
dotnet --version          # → 10.0.x
flutter doctor -v         # → không còn dấu [✗] ở các mục bắt buộc
docker --version          # → Docker version ...
docker compose version    # → Docker Compose version ...
git --version              # → git version ...
ssh -T git@github.com      # → Hi <username>! ...
docker compose up -d       # → 4 container running
```

Nếu tất cả lệnh trên chạy không lỗi, môi trường phát triển đã sẵn sàng để bắt đầu **Giai đoạn 0** trong `KE-HOACH-TRIEN-KHAI.md`.

---

*Nếu gặp lỗi trong quá trình cài đặt, ưu tiên kiểm tra: (1) đã mở lại Terminal/PowerShell sau khi cài để nhận biến môi trường mới, (2) đã có kết nối Internet ổn định khi tải component lớn (Android SDK, Xcode), (3) chạy `flutter doctor -v` hoặc `docker compose logs` để đọc thông báo lỗi cụ thể trước khi tìm kiếm thêm.*
