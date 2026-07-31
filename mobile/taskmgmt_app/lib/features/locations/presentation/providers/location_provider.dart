import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/di/injection.dart';
import '../../domain/entities/location.dart';
import '../../domain/repositories/location_repository.dart';

final locationRepositoryProvider = Provider<LocationRepository>((ref) => getIt<LocationRepository>());

final locationsProvider = AsyncNotifierProvider<LocationsController, List<Location>>(LocationsController.new);

class LocationsController extends AsyncNotifier<List<Location>> {
  @override
  Future<List<Location>> build() {
    return ref.read(locationRepositoryProvider).getLocations();
  }

  Future<void> refresh() async {
    state = const AsyncLoading();
    state = await AsyncValue.guard(() => ref.read(locationRepositoryProvider).getLocations());
  }
}
