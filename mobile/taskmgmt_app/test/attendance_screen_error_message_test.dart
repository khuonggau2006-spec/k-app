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
