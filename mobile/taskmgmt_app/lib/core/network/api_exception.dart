import 'package:dio/dio.dart';

class ApiException implements Exception {
  const ApiException(this.message, {this.fieldErrors});

  final String message;
  final Map<String, List<String>>? fieldErrors;

  List<String> get allMessages => [
        message,
        ...?fieldErrors?.values.expand((messages) => messages),
      ];

  @override
  String toString() => allMessages.join('\n');
}

ApiException mapDioException(DioException exception) {
  final data = exception.response?.data;

  if (data is Map<String, dynamic>) {
    final detail = data['detail'] as String?;
    final title = data['title'] as String?;
    final errorsRaw = data['errors'];

    Map<String, List<String>>? fieldErrors;
    if (errorsRaw is Map) {
      fieldErrors = errorsRaw.map(
        (key, value) => MapEntry(key as String, (value as List).map((e) => e.toString()).toList()),
      );
    }

    return ApiException(detail ?? title ?? 'Đã có lỗi xảy ra.', fieldErrors: fieldErrors);
  }

  // 401 không kèm body JSON là challenge của JWT Bearer middleware (token hết hạn/không
  // hợp lệ), khác với 401 có body từ backend (ví dụ sai mật khẩu lúc đăng nhập).
  if (exception.response?.statusCode == 401) {
    return const ApiException('Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.');
  }

  // 429 (rate limit) không kèm body JSON - middleware giới hạn tần suất trả về rỗng.
  if (exception.response?.statusCode == 429) {
    return const ApiException('Bạn thao tác quá nhanh. Vui lòng thử lại sau ít phút.');
  }

  // Không có response nghĩa là request chưa tới được server - phân biệt rõ nguyên nhân
  // thay vì gộp chung 1 thông báo, để người dùng biết nên chờ hay nên kiểm tra mạng.
  return switch (exception.type) {
    DioExceptionType.connectionTimeout ||
    DioExceptionType.sendTimeout ||
    DioExceptionType.receiveTimeout =>
      const ApiException('Kết nối tới máy chủ quá lâu. Vui lòng thử lại.'),
    DioExceptionType.connectionError =>
      const ApiException('Không thể kết nối tới máy chủ. Vui lòng kiểm tra kết nối mạng.'),
    DioExceptionType.badCertificate =>
      const ApiException('Kết nối không an toàn tới máy chủ. Vui lòng thử lại sau.'),
    DioExceptionType.cancel => const ApiException('Yêu cầu đã bị huỷ.'),
    _ => const ApiException('Không thể kết nối tới máy chủ. Vui lòng kiểm tra kết nối mạng.'),
  };
}
