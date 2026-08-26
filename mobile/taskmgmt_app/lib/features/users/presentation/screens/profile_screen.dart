import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../shared/widgets/user_avatar.dart';
import '../../../auth/presentation/providers/auth_provider.dart';
import '../providers/user_provider.dart';

class ProfileScreen extends ConsumerStatefulWidget {
  const ProfileScreen({super.key});

  static const path = '/profile';
  static const name = 'profile';

  @override
  ConsumerState<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends ConsumerState<ProfileScreen> {
  bool _isBusy = false;

  void _showError(String message) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
  }

  Future<void> _pickAndUpload(ImageSource source) async {
    try {
      final xfile = await ImagePicker().pickImage(source: source);
      if (xfile == null) return;

      setState(() => _isBusy = true);
      final bytes = await xfile.readAsBytes();
      final updatedUser =
          await ref.read(userRepositoryProvider).uploadAvatar(bytes: bytes, fileName: xfile.name);
      ref.invalidate(avatarBytesProvider(updatedUser.id));
      ref.read(authControllerProvider.notifier).updateUser(updatedUser);
    } catch (e) {
      _showError(e is ApiException ? e.allMessages.join('\n') : 'Không thể tải ảnh lên.');
    } finally {
      if (mounted) setState(() => _isBusy = false);
    }
  }

  Future<void> _showSourceSheet() {
    return showModalBottomSheet<void>(
      context: context,
      builder: (sheetContext) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              leading: const Icon(Icons.photo_camera_outlined),
              title: const Text('Chụp ảnh'),
              onTap: () {
                Navigator.of(sheetContext).pop();
                _pickAndUpload(ImageSource.camera);
              },
            ),
            ListTile(
              leading: const Icon(Icons.photo_library_outlined),
              title: const Text('Chọn từ thư viện'),
              onTap: () {
                Navigator.of(sheetContext).pop();
                _pickAndUpload(ImageSource.gallery);
              },
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _deleteAvatar() async {
    try {
      setState(() => _isBusy = true);
      final updatedUser = await ref.read(userRepositoryProvider).deleteAvatar();
      ref.invalidate(avatarBytesProvider(updatedUser.id));
      ref.read(authControllerProvider.notifier).updateUser(updatedUser);
    } catch (e) {
      _showError(e is ApiException ? e.allMessages.join('\n') : 'Không thể xoá avatar.');
    } finally {
      if (mounted) setState(() => _isBusy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final user = ref.watch(authControllerProvider).valueOrNull;

    return Scaffold(
      appBar: AppBar(title: const Text('Hồ sơ của tôi')),
      body: user == null
          ? const Center(child: CircularProgressIndicator())
          : ListView(
              padding: const EdgeInsets.all(16),
              children: [
                Center(
                  child: Stack(
                    children: [
                      UserAvatar(
                        userId: user.id,
                        hasAvatar: user.hasAvatar,
                        fallbackText: user.fullName.isNotEmpty ? user.fullName[0].toUpperCase() : '?',
                        radius: 56,
                      ),
                      if (_isBusy)
                        const Positioned.fill(
                          child: CircleAvatar(
                            radius: 56,
                            backgroundColor: Colors.black38,
                            child: CircularProgressIndicator(),
                          ),
                        ),
                      Positioned(
                        bottom: 0,
                        right: 0,
                        child: IconButton.filled(
                          icon: const Icon(Icons.camera_alt),
                          tooltip: 'Đổi avatar',
                          onPressed: _isBusy ? null : _showSourceSheet,
                        ),
                      ),
                    ],
                  ),
                ),
                if (user.hasAvatar) ...[
                  const SizedBox(height: 8),
                  Center(
                    child: TextButton(
                      onPressed: _isBusy ? null : _deleteAvatar,
                      child: const Text('Xoá avatar'),
                    ),
                  ),
                ],
                const SizedBox(height: 24),
                ListTile(
                  leading: const Icon(Icons.person_outline),
                  title: const Text('Họ tên'),
                  subtitle: Text(user.fullName),
                ),
                ListTile(
                  leading: const Icon(Icons.email_outlined),
                  title: const Text('Email'),
                  subtitle: Text(user.email),
                ),
              ],
            ),
    );
  }
}
