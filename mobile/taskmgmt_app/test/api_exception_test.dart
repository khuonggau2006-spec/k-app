import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:taskmgmt_app/core/network/api_exception.dart';

RequestOptions get _options => RequestOptions(path: '/test');

void main() {
  group('mapDioException', () {
    test('Response with JSON detail uses that message as fieldErrors', () {
      final exception = DioException(
        requestOptions: _options,
        response: Response(
          requestOptions: _options,
          statusCode: 400,
          data: {
            'detail': 'Email đã được sử dụng.',
            'errors': {
              'Email': ['Email không hợp lệ.'],
            },
          },
        ),
      );

      final result = mapDioException(exception);

      expect(result.message, 'Email đã được sử dụng.');
      expect(result.fieldErrors?['Email'], ['Email không hợp lệ.']);
    });

    test('401 without JSON body maps to session-expired message', () {
      final exception = DioException(
        requestOptions: _options,
        response: Response(requestOptions: _options, statusCode: 401),
      );

      expect(mapDioException(exception).message, contains('Phiên đăng nhập đã hết hạn'));
    });

    test('429 without JSON body maps to rate-limit message', () {
      final exception = DioException(
        requestOptions: _options,
        response: Response(requestOptions: _options, statusCode: 429),
      );

      expect(mapDioException(exception).message, contains('quá nhanh'));
    });

    test('connectionTimeout maps to a timeout message, not a generic one', () {
      final exception = DioException(requestOptions: _options, type: DioExceptionType.connectionTimeout);

      expect(mapDioException(exception).message, contains('quá lâu'));
    });

    test('connectionError maps to a check-your-network message', () {
      final exception = DioException(requestOptions: _options, type: DioExceptionType.connectionError);

      expect(mapDioException(exception).message, contains('kiểm tra kết nối mạng'));
    });

    test('cancel maps to a cancelled-request message', () {
      final exception = DioException(requestOptions: _options, type: DioExceptionType.cancel);

      expect(mapDioException(exception).message, contains('huỷ'));
    });
  });
}
