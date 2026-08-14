import 'package:dio/dio.dart';

import '../../../../core/network/api_exception.dart';
import '../models/notification_model.dart';

class PagedNotificationResult {
  const PagedNotificationResult({
    required this.items,
    required this.pageNumber,
    required this.pageSize,
    required this.totalCount,
    required this.totalPages,
  });

  final List<NotificationModel> items;
  final int pageNumber;
  final int pageSize;
  final int totalCount;
  final int totalPages;
}

class NotificationRemoteDataSource {
  NotificationRemoteDataSource(this._dio);

  final Dio _dio;

  Future<PagedNotificationResult> getNotifications({bool unreadOnly = false, int pageSize = 50}) async {
    try {
      final response = await _dio.get<Map<String, dynamic>>(
        '/notifications',
        queryParameters: {'unreadOnly': unreadOnly, 'pageSize': pageSize},
      );
      final data = response.data!;
      return PagedNotificationResult(
        items: (data['items'] as List<dynamic>)
            .map((json) => NotificationModel.fromJson(json as Map<String, dynamic>))
            .toList(),
        pageNumber: data['pageNumber'] as int,
        pageSize: data['pageSize'] as int,
        totalCount: data['totalCount'] as int,
        totalPages: data['totalPages'] as int,
      );
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<int> getUnreadCount() async {
    try {
      final response = await _dio.get<Map<String, dynamic>>('/notifications/unread-count');
      return response.data!['count'] as int;
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<void> markAsRead(String id) async {
    try {
      await _dio.put<void>('/notifications/$id/read');
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }

  Future<void> markAllAsRead() async {
    try {
      await _dio.put<void>('/notifications/read-all');
    } on DioException catch (e) {
      throw mapDioException(e);
    }
  }
}
