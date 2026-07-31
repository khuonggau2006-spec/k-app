# TaskMgmt — Hệ thống Quản lý Công việc & Vị trí làm việc

Flutter · .NET 10 · PostgreSQL · Redis · Hangfire · Docker

## Tài liệu

- [`docs/KE-HOACH-TRIEN-KHAI.md`](docs/KE-HOACH-TRIEN-KHAI.md) — kế hoạch triển khai theo giai đoạn (G0–G5)
- [`docs/HUONG-DAN-CAI-DAT-MOI-TRUONG-PHAT-TRIEN.md`](docs/HUONG-DAN-CAI-DAT-MOI-TRUONG-PHAT-TRIEN.md) — cài đặt môi trường dev
- `CONTRIBUTING.md` — coding convention, quy trình branch/commit/PR

## Cấu trúc thư mục

```
K-app/
├── backend/            # .NET 10 solution (Clean Architecture)
│   ├── src/
│   │   ├── TaskMgmt.Domain/
│   │   ├── TaskMgmt.Application/
│   │   ├── TaskMgmt.Infrastructure/
│   │   └── TaskMgmt.API/
│   └── tests/
├── mobile/
│   └── taskmgmt_app/    # Flutter app (feature-first)
├── deploy/              # docker-compose.prod.yml, nginx, k8s (nếu có)
├── docs/                # tài liệu dự án (PRD, ERD, kế hoạch...)
├── .github/workflows/   # CI cho backend & mobile
└── docker-compose.yml   # PostgreSQL + Redis cho môi trường dev
```

## Bắt đầu nhanh (dev local)

```bash
# 1. Chạy hạ tầng nền (PostgreSQL, Redis)
docker compose up -d

# 2. Chạy Backend API
cd backend/src/TaskMgmt.API
dotnet run

# 3. Chạy Flutter app
cd mobile/taskmgmt_app
flutter run
```

Xem chi tiết cài đặt môi trường tại [`docs/HUONG-DAN-CAI-DAT-MOI-TRUONG-PHAT-TRIEN.md`](docs/HUONG-DAN-CAI-DAT-MOI-TRUONG-PHAT-TRIEN.md).
