import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:get_it/get_it.dart';
import 'package:image_picker_platform_interface/image_picker_platform_interface.dart';

import 'package:taskmgmt_app/core/network/auth_event_bus.dart';
import 'package:taskmgmt_app/features/auth/domain/entities/user.dart';
import 'package:taskmgmt_app/features/auth/domain/repositories/auth_repository.dart';
import 'package:taskmgmt_app/features/auth/presentation/providers/auth_provider.dart';
import 'package:taskmgmt_app/features/users/domain/repositories/user_repository.dart';
import 'package:taskmgmt_app/features/users/presentation/providers/user_provider.dart';
import 'package:taskmgmt_app/features/users/presentation/screens/profile_screen.dart';

import 'fake_image_picker_platform.dart';

const _userWithAvatar = User(
  id: '1',
  email: 'a@b.com',
  fullName: 'Nguyễn Test',
  systemRole: SystemRole.member,
  hasAvatar: true,
);

const _userWithoutAvatar = User(
  id: '1',
  email: 'a@b.com',
  fullName: 'Nguyễn Test',
  systemRole: SystemRole.member,
  hasAvatar: false,
);

class _FakeAuthRepository implements AuthRepository {
  _FakeAuthRepository(this.initialUser);

  final User initialUser;

  @override
  Future<User?> restoreSession() async => initialUser;

  @override
  Future<User> login({required String email, required String password}) async => throw UnimplementedError();

  @override
  Future<User> register({required String email, required String fullName, required String password}) async =>
      throw UnimplementedError();

  @override
  Future<void> logout() async {}
}

class _FakeUserRepository implements UserRepository {
  _FakeUserRepository();

  bool deleteAvatarCalled = false;
  bool uploadAvatarCalled = false;
  Uint8List? lastUploadedBytes;
  String? lastUploadedFileName;

  @override
  Future<List<User>> getUsers() async => [];

  @override
  Future<Uint8List?> downloadAvatar(String userId) async => null;

  @override
  Future<User> uploadAvatar({required Uint8List bytes, required String fileName}) async {
    uploadAvatarCalled = true;
    lastUploadedBytes = bytes;
    lastUploadedFileName = fileName;
    return _userWithAvatar;
  }

  @override
  Future<User> deleteAvatar() async {
    deleteAvatarCalled = true;
    return _userWithoutAvatar;
  }
}

Widget _buildScreen(User user, UserRepository userRepository) => ProviderScope(
      overrides: [
        authRepositoryProvider.overrideWithValue(_FakeAuthRepository(user)),
        userRepositoryProvider.overrideWithValue(userRepository),
      ],
      child: const MaterialApp(home: ProfileScreen()),
    );

void main() {
  late FakeImagePickerPlatform imagePicker;

  setUp(() {
    // AuthController.build() gọi getIt<AuthEventBus>() - đăng ký thủ công vì test không gọi
    // setupLocator() (tránh phải fake toàn bộ cây phụ thuộc DI thật).
    GetIt.instance.registerLazySingleton<AuthEventBus>(() => AuthEventBus());
    imagePicker = FakeImagePickerPlatform();
    ImagePickerPlatform.instance = imagePicker;
  });

  tearDown(() => GetIt.instance.reset());

  testWidgets('Shows full name, email, and delete button when user has an avatar', (tester) async {
    await tester.pumpWidget(_buildScreen(_userWithAvatar, _FakeUserRepository()));
    await tester.pumpAndSettle();

    expect(find.text('Nguyễn Test'), findsOneWidget);
    expect(find.text('a@b.com'), findsOneWidget);
    expect(find.text('Xoá avatar'), findsOneWidget);
  });

  testWidgets('Hides delete button when user has no avatar', (tester) async {
    await tester.pumpWidget(_buildScreen(_userWithoutAvatar, _FakeUserRepository()));
    await tester.pumpAndSettle();

    expect(find.text('Xoá avatar'), findsNothing);
  });

  testWidgets('Tapping delete avatar calls the repository', (tester) async {
    final userRepository = _FakeUserRepository();
    await tester.pumpWidget(_buildScreen(_userWithAvatar, userRepository));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Xoá avatar'));
    await tester.pumpAndSettle();

    expect(userRepository.deleteAvatarCalled, isTrue);
    expect(find.text('Xoá avatar'), findsNothing);
  });

  testWidgets('Tapping the camera button, picking a photo, uploads it and refreshes the avatar',
      (tester) async {
    final userRepository = _FakeUserRepository();
    imagePicker.fileName = 'anh_moi.png';

    final container = ProviderContainer(
      overrides: [
        authRepositoryProvider.overrideWithValue(_FakeAuthRepository(_userWithoutAvatar)),
        userRepositoryProvider.overrideWithValue(userRepository),
      ],
    );
    addTearDown(container.dispose);

    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: const MaterialApp(home: ProfileScreen()),
      ),
    );
    await tester.pumpAndSettle();

    await tester.tap(find.byTooltip('Đổi avatar'));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Chọn từ thư viện'));
    await tester.pumpAndSettle();

    expect(imagePicker.lastSource, ImageSource.gallery);
    expect(userRepository.uploadAvatarCalled, isTrue);
    expect(userRepository.lastUploadedFileName, 'anh_moi.png');
    expect(userRepository.lastUploadedBytes, isNotNull);

    // Upload trả về user đã có avatar - AuthController phải phản ánh đúng trạng thái mới,
    // nhờ đó nút "Xoá avatar" xuất hiện dù ban đầu user chưa có avatar.
    expect(container.read(authControllerProvider).valueOrNull, _userWithAvatar);
    expect(find.text('Xoá avatar'), findsOneWidget);
  });
}
