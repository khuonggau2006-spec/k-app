# Quy ước đóng góp mã nguồn

## Branching

| Nhánh | Mục đích |
|---|---|
| `main` | Production, chỉ merge qua PR đã duyệt |
| `develop` | Tích hợp cho staging |
| `feature/<ten-tinh-nang>` | Tách từ `develop` |
| `bugfix/<mo-ta>` | Sửa lỗi phát hiện ở staging |
| `hotfix/<mo-ta>` | Sửa lỗi khẩn cấp trên `main` |

## Commit message (Conventional Commits)

`feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`

Ví dụ: `feat(task): thêm API tạo công việc`

## Definition of Done

1. Code merge vào `develop` qua Pull Request, được ít nhất 1 người review.
2. Có unit test cho logic domain/nghiệp vụ quan trọng, test pass trên CI.
3. Không phá vỡ build/lint trên pipeline CI.
4. API mới cập nhật vào Swagger/Postman collection.
5. Test thủ công theo kịch bản QA cơ bản (happy path + 1–2 edge case).

## Coding convention

### C# (backend)

- Theo `.editorconfig` ở `backend/.editorconfig`.
- Namespace theo cấu trúc thư mục (`TaskMgmt.Domain.Entities`, ...).
- CQRS qua MediatR: mỗi use case là 1 `Command`/`Query` + `Handler` trong `Application/Features/<Module>`.
- Validate input bằng FluentValidation, không validate thủ công trong Handler.

### Dart (mobile)

- Theo `flutter_lints` mặc định (xem `mobile/taskmgmt_app/analysis_options.yaml`).
- Mỗi feature tự chứa `data/domain/presentation` riêng trong `lib/features/<feature>/`.
- Code dùng chung đặt ở `lib/core/` (hạ tầng kỹ thuật) hoặc `lib/shared/` (UI/model dùng chung).
