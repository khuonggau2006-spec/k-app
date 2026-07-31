class AppConfig {
  const AppConfig._();

  /// Backend chạy tại `dotnet run --urls "http://localhost:5299"`.
  /// Trên Android emulator, đổi `localhost` thành `10.0.2.2` để trỏ về máy host.
  static const String apiBaseUrl = 'http://localhost:5299/api/v1';
}
