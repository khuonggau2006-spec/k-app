import 'package:flutter_test/flutter_test.dart';
import 'package:taskmgmt_app/core/network/api_exception.dart';
import 'package:taskmgmt_app/features/attendance/presentation/screens/attendance_screen.dart';

void main() {
  group('attendanceErrorMessage', () {
    test('returns ApiException message when error is ApiException', () {
      final error = const ApiException('API error message');
      final message = attendanceErrorMessage(error, 'Fallback message');
      expect(message, equals('API error message'));
    });

    test('returns the specific field validation message, not the generic title', () {
      // Mô phỏng đúng response thật từ backend khi check-in ngoài phạm vi vị trí: title chung
      // "Validation failed" (từ GlobalExceptionHandler), lý do thật nằm trong fieldErrors.
      final error = const ApiException(
        'Validation failed',
        fieldErrors: {
          'Latitude': ['Ngoài phạm vi cho phép của mọi vị trí đã đăng ký.'],
        },
      );
      final message = attendanceErrorMessage(error, 'Fallback message');
      expect(message, equals('Validation failed\nNgoài phạm vi cho phép của mọi vị trí đã đăng ký.'));
    });

    test('returns LocationAccessException message when error is LocationAccessException', () {
      final error = LocationAccessException('Location access denied');
      final message = attendanceErrorMessage(error, 'Fallback message');
      expect(message, equals('Location access denied'));
    });

    test('returns fallback message for unrelated exceptions', () {
      final error = Exception('Some other error');
      final message = attendanceErrorMessage(error, 'Fallback message');
      expect(message, equals('Fallback message'));
    });

    test('returns fallback message for generic Object error', () {
      const error = 'Generic error string';
      final message = attendanceErrorMessage(error, 'Fallback message');
      expect(message, equals('Fallback message'));
    });
  });
}
