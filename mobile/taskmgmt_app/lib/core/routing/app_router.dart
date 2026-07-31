import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/presentation/providers/auth_provider.dart';
import '../../features/auth/presentation/screens/login_screen.dart';
import '../../features/auth/presentation/screens/register_screen.dart';
import '../../features/locations/presentation/screens/location_list_screen.dart';
import '../../features/tasks/presentation/screens/task_list_screen.dart';
import '../../features/tasks/presentation/screens/work_task_detail_screen.dart';

class _AuthRefreshNotifier extends ChangeNotifier {
  _AuthRefreshNotifier(Ref ref) {
    ref.listen(authControllerProvider, (_, _) => notifyListeners());
  }
}

final routerProvider = Provider<GoRouter>((ref) {
  final refreshNotifier = _AuthRefreshNotifier(ref);
  ref.onDispose(refreshNotifier.dispose);

  return GoRouter(
    initialLocation: LoginScreen.path,
    refreshListenable: refreshNotifier,
    redirect: (context, state) {
      final authState = ref.read(authControllerProvider);
      if (authState.isLoading) return null;

      final isAuthenticated = authState.valueOrNull != null;
      final isAuthRoute = state.matchedLocation == LoginScreen.path || state.matchedLocation == RegisterScreen.path;

      if (!isAuthenticated && !isAuthRoute) return LoginScreen.path;
      if (isAuthenticated && isAuthRoute) return TaskListScreen.path;
      return null;
    },
    routes: [
      GoRoute(
        path: LoginScreen.path,
        name: LoginScreen.name,
        builder: (context, state) => const LoginScreen(),
      ),
      GoRoute(
        path: RegisterScreen.path,
        name: RegisterScreen.name,
        builder: (context, state) => const RegisterScreen(),
      ),
      GoRoute(
        path: TaskListScreen.path,
        name: TaskListScreen.name,
        builder: (context, state) => const TaskListScreen(),
      ),
      GoRoute(
        path: LocationListScreen.path,
        name: LocationListScreen.name,
        builder: (context, state) => const LocationListScreen(),
      ),
      GoRoute(
        path: '/tasks/:id',
        name: WorkTaskDetailScreen.name,
        builder: (context, state) => WorkTaskDetailScreen(taskId: state.pathParameters['id']!),
      ),
    ],
  );
});
