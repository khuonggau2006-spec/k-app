import 'package:flutter_test/flutter_test.dart';
import 'package:taskmgmt_app/core/config/app_config.dart';

void main() {
  tearDown(() => AppConfig.applyRemoteBaseUrl(null));

  test('apiBaseUrl and hubBaseUrl use the local dev default when Remote Config has not applied a value', () {
    expect(AppConfig.apiBaseUrl, endsWith(':5299/api/v1'));
    expect(AppConfig.hubBaseUrl, endsWith(':5299'));
  });

  test('applyRemoteBaseUrl overrides apiBaseUrl and hubBaseUrl', () {
    AppConfig.applyRemoteBaseUrl('https://api.taskmgmt.example.com');

    expect(AppConfig.apiBaseUrl, 'https://api.taskmgmt.example.com/api/v1');
    expect(AppConfig.hubBaseUrl, 'https://api.taskmgmt.example.com');
  });

  test('applyRemoteBaseUrl with an empty string falls back to the local dev default', () {
    AppConfig.applyRemoteBaseUrl('https://api.taskmgmt.example.com');
    AppConfig.applyRemoteBaseUrl('');

    expect(AppConfig.apiBaseUrl, endsWith(':5299/api/v1'));
  });

  test('applyRemoteBaseUrl with null resets back to the local dev default', () {
    AppConfig.applyRemoteBaseUrl('https://api.taskmgmt.example.com');
    AppConfig.applyRemoteBaseUrl(null);

    expect(AppConfig.apiBaseUrl, endsWith(':5299/api/v1'));
  });
}
