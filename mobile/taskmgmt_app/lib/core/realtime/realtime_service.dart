import 'dart:async';

import 'package:signalr_netcore/signalr_client.dart';

import '../config/app_config.dart';
import '../storage/token_storage.dart';
import 'task_updated_event.dart';

/// Kết nối tới NotificationHub (backend mục 3.2) để nhận cập nhật realtime khi app đang mở.
/// Server broadcast sự kiện "TaskUpdated" cho nhóm `task:{taskId}` - client phải tự
/// JoinTaskGroup/LeaveTaskGroup theo đúng công việc đang xem (xem [joinTaskGroup]).
class RealtimeService {
  RealtimeService(this._tokenStorage);

  final TokenStorage _tokenStorage;
  HubConnection? _connection;
  final _taskUpdatesController = StreamController<TaskUpdatedEvent>.broadcast();

  Stream<TaskUpdatedEvent> get taskUpdates => _taskUpdatesController.stream;

  Future<void> connect() async {
    if (_connection != null) return;

    final connection = HubConnectionBuilder()
        .withUrl(
          '${AppConfig.hubBaseUrl}/hubs/notifications',
          options: HttpConnectionOptions(
            accessTokenFactory: () async => (await _tokenStorage.readSession())?.accessToken ?? '',
          ),
        )
        .withAutomaticReconnect()
        .build();

    connection.on('TaskUpdated', (arguments) {
      if (arguments == null || arguments.isEmpty) return;
      final data = arguments[0] as Map<String, dynamic>?;
      if (data != null) {
        _taskUpdatesController.add(TaskUpdatedEvent.fromJson(data));
      }
    });

    _connection = connection;

    try {
      await connection.start();
    } catch (_) {
      // Không kết nối được (mất mạng/server tạm ngưng) - bỏ qua êm ái, UI vẫn dùng được nhờ
      // pull-to-refresh thủ công; withAutomaticReconnect() lo việc thử lại sau khi start() đã
      // thành công ít nhất 1 lần. Không dọn _connection để lần initialize() sau (đăng nhập lại)
      // không tạo kết nối trùng nếu HubConnection tự phục hồi được.
    }
  }

  Future<void> disconnect() async {
    final connection = _connection;
    _connection = null;
    await connection?.stop();
  }

  Future<void> joinTaskGroup(String taskId) async {
    if (_connection?.state != HubConnectionState.Connected) return;
    try {
      await _connection!.invoke('JoinTaskGroup', args: [taskId]);
    } catch (_) {}
  }

  Future<void> leaveTaskGroup(String taskId) async {
    if (_connection?.state != HubConnectionState.Connected) return;
    try {
      await _connection!.invoke('LeaveTaskGroup', args: [taskId]);
    } catch (_) {}
  }
}
