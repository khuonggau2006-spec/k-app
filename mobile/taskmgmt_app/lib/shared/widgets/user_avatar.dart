import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../features/users/presentation/providers/user_provider.dart';

class UserAvatar extends ConsumerWidget {
  const UserAvatar({
    super.key,
    required this.userId,
    required this.hasAvatar,
    required this.fallbackText,
    this.radius = 20,
  });

  final String userId;
  final bool hasAvatar;
  final String fallbackText;
  final double radius;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    if (!hasAvatar) {
      return CircleAvatar(radius: radius, child: Text(fallbackText));
    }

    final bytesAsync = ref.watch(avatarBytesProvider(userId));
    return bytesAsync.when(
      data: (bytes) => bytes == null
          ? CircleAvatar(radius: radius, child: Text(fallbackText))
          : CircleAvatar(radius: radius, backgroundImage: MemoryImage(bytes)),
      loading: () => CircleAvatar(
        radius: radius,
        child: SizedBox.square(
          dimension: radius * 0.6,
          child: const CircularProgressIndicator(strokeWidth: 2),
        ),
      ),
      error: (_, _) => CircleAvatar(radius: radius, child: Text(fallbackText)),
    );
  }
}
