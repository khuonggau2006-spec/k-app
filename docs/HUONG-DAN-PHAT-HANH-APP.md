# Hướng dẫn phát hành app TaskMgmt (mục 5.8)

## 1. Android — đã sẵn sàng, chỉ còn thiếu tài khoản Play Console

### Ký release thật (đã cấu hình, đã build verify)

Trước đây `android/app/build.gradle.kts` ký release bằng debug key (chỉ để `flutter run --release`
chạy được lúc dev, **không dùng để phát hành**). Giờ đã có keystore thật:

- Keystore lưu tại `C:\Users\admin\.android-signing\taskmgmt-upload-key.jks` — **ngoài repo hoàn
  toàn**, không có rủi ro commit nhầm.
- `android/key.properties` trỏ tới keystore đó — đã nằm trong `.gitignore` (cả nested
  `android/.gitignore` lẫn root `.gitignore`).
- `build.gradle.kts` tự đọc `key.properties` nếu có; máy nào chưa có file này (vd: máy CI/dev
  khác) thì tự rơi về ký debug, không làm hỏng build thường ngày.

**Quan trọng**: mật khẩu keystore hiện đặt tạm là `TaskMgmt-Upload-2026-CHANGE-ME` — **đổi ngay**
trước khi dùng để phát hành thật (`keytool -storepasswd`), và **backup file `.jks` này ở nơi an
toàn khác** (ổ cứng ngoài, password manager...). Mất file này hoặc quên mật khẩu thì **không thể
cập nhật app đã phát hành nữa** — Google Play bắt buộc mọi bản cập nhật phải ký cùng 1 key với bản
đầu tiên, không có cách khôi phục.

### Build bản phát hành

```bash
# AAB (Android App Bundle) - định dạng Play Store yêu cầu, khuyến nghị dùng cái này
flutter build appbundle --release

# APK - dùng để test thủ công trên thiết bị thật hoặc phát hành ngoài Play Store (side-load)
flutter build apk --release
```

Output:
- AAB: `build/app/outputs/bundle/release/app-release.aab`
- APK: `build/app/outputs/flutter-apk/app-release.apk`

Xác nhận đã ký đúng key thật (không phải debug):
```bash
jarsigner -verify -verbose -certs build/app/outputs/bundle/release/app-release.aab
```
Tìm dòng `CN=TaskMgmt` trong phần certificate — nếu thấy `CN=Android Debug` nghĩa là
`key.properties` chưa được đọc đúng (kiểm tra lại đường dẫn `storeFile`).

### Tăng version cho mỗi lần phát hành

Sửa `pubspec.yaml`, dòng `version: 1.0.0+1` — số trước dấu `+` là `versionName` (hiển thị cho
người dùng), số sau là `versionCode` (Google Play dùng để so sánh bản mới/cũ, **bắt buộc tăng dần**
mỗi lần upload, không được trùng hoặc giảm).

### Các bước còn lại cần tài khoản Google Play Console thật (chưa làm được)

1. Đăng ký tài khoản Google Play Console (phí một lần $25).
2. Tạo app mới, điền thông tin: tên, mô tả, ảnh chụp màn hình, icon, danh mục.
3. **Privacy Policy URL** — bắt buộc vì app có network permission + thu thập dữ liệu người dùng
   (email, vị trí công việc...). Cần viết 1 trang chính sách bảo mật thật, host ở đâu đó công khai.
4. Điền **Data safety** form — khai báo trung thực dữ liệu app thu thập (email, tên, vị trí...) và
   mục đích dùng.
5. Content rating questionnaire.
6. Upload file AAB vào 1 release track (khuyến nghị: bắt đầu ở **Internal testing** hoặc **Closed
   testing** trước khi lên **Production**, để có thời gian phát hiện lỗi với nhóm nhỏ).
7. Chờ Google review (thường 1-7 ngày cho app mới).

## 2. iOS — không thể làm được từ máy Windows này

Build & phát hành lên TestFlight/App Store **bắt buộc cần**:
- Máy Mac (Xcode chỉ chạy trên macOS, không có bản Windows).
- Xcode cài từ Mac App Store.
- Tài khoản Apple Developer Program (phí **$99/năm**).

Đây là giới hạn phần cứng/nền tảng, không phải thiếu cấu hình có thể chuẩn bị trước như các mục
khác của G5. Khi có máy Mac:
```bash
flutter build ipa --release
```
rồi upload qua Xcode Organizer hoặc Transporter, tạo listing trên App Store Connect tương tự quy
trình Android ở trên (privacy policy, mô tả, ảnh chụp màn hình theo đúng kích thước từng loại thiết
bị iOS yêu cầu).

## 3. Checklist trước khi submit bản đầu tiên (cả 2 nền tảng)

- [ ] Đã chạy hết checklist UAT (xem artifact UAT — TaskMgmt G1-G4) trên thiết bị thật, không chỉ
      máy ảo.
- [ ] `API_URL` trỏ đúng backend **production** thật (qua Firebase Remote Config, mục 5.4), không
      phải `10.0.2.2`/`localhost` còn sót lại từ lúc dev.
- [ ] Backend production đã deploy, có HTTPS thật (mục 5.5/5.6), không phải HTTP.
- [ ] Đã backup keystore `.jks` + mật khẩu ở nơi an toàn ngoài máy đang dùng để build.
- [ ] Đã đổi mật khẩu keystore khỏi giá trị tạm `TaskMgmt-Upload-2026-CHANGE-ME`.
- [ ] Privacy Policy đã viết và host công khai.
