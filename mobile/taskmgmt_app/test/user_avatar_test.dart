import 'dart:convert';
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:taskmgmt_app/features/auth/domain/entities/user.dart';
import 'package:taskmgmt_app/features/users/domain/repositories/user_repository.dart';
import 'package:taskmgmt_app/features/users/presentation/providers/user_provider.dart';
import 'package:taskmgmt_app/shared/widgets/user_avatar.dart';

// PNG 1x1 hợp lệ tối thiểu - MemoryImage cần decode được, không thể dùng bytes rác.
final _onePixelPng = base64Decode(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=',
);

class _FakeUserRepository implements UserRepository {
  _FakeUserRepository({this.bytes, this.error});

  final Uint8List? bytes;
  final Object? error;

  @override
  Future<List<User>> getUsers() async => [];

  @override
  Future<Uint8List?> downloadAvatar(String userId) async {
    if (error != null) throw error!;
    return bytes;
  }

  @override
  Future<User> uploadAvatar({required Uint8List bytes, required String fileName}) async =>
      throw UnimplementedError();

  @override
  Future<User> deleteAvatar() async => throw UnimplementedError();
}

Widget _buildWidget(UserRepository repository, {required bool hasAvatar}) => ProviderScope(
      overrides: [userRepositoryProvider.overrideWithValue(repository)],
      child: MaterialApp(
        home: Scaffold(
          body: UserAvatar(userId: 'u1', hasAvatar: hasAvatar, fallbackText: 'A'),
        ),
      ),
    );

void main() {
  testWidgets('hasAvatar=false shows fallback text immediately, no network call', (tester) async {
    var called = false;
    final repository = _FakeUserRepository();
    // Bọc để phát hiện có gọi downloadAvatar không - dùng repository riêng theo dõi lời gọi.
    await tester.pumpWidget(_buildWidget(_TrackingRepository(inner: repository, onCalled: () => called = true), hasAvatar: false));
    await tester.pumpAndSettle();

    expect(find.text('A'), findsOneWidget);
    expect(called, isFalse);
  });

  testWidgets('hasAvatar=true and bytes load shows CircleAvatar with backgroundImage', (tester) async {
    final repository = _FakeUserRepository(bytes: _onePixelPng);
    await tester.pumpWidget(_buildWidget(repository, hasAvatar: true));
    await tester.pumpAndSettle();

    final avatar = tester.widget<CircleAvatar>(find.byType(CircleAvatar));
    expect(avatar.backgroundImage, isNotNull);
    expect(find.text('A'), findsNothing);
  });

  testWidgets('hasAvatar=true and download fails falls back to text', (tester) async {
    final repository = _FakeUserRepository(error: Exception('network error'));
    await tester.pumpWidget(_buildWidget(repository, hasAvatar: true));
    await tester.pumpAndSettle();

    expect(find.text('A'), findsOneWidget);
  });
}

class _TrackingRepository implements UserRepository {
  _TrackingRepository({required this.inner, required this.onCalled});

  final UserRepository inner;
  final VoidCallback onCalled;

  @override
  Future<List<User>> getUsers() => inner.getUsers();

  @override
  Future<Uint8List?> downloadAvatar(String userId) {
    onCalled();
    return inner.downloadAvatar(userId);
  }

  @override
  Future<User> uploadAvatar({required Uint8List bytes, required String fileName}) =>
      inner.uploadAvatar(bytes: bytes, fileName: fileName);

  @override
  Future<User> deleteAvatar() => inner.deleteAvatar();
}
