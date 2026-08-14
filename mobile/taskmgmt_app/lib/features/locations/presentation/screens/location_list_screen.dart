import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../shared/widgets/empty_state_view.dart';
import '../../../../shared/widgets/error_state_view.dart';
import '../../domain/entities/location.dart';
import '../providers/location_provider.dart';
import '../widgets/location_list_view.dart';
import '../widgets/location_map_view.dart';

class LocationListScreen extends ConsumerStatefulWidget {
  const LocationListScreen({super.key});

  static const path = '/locations';
  static const name = 'locations';

  @override
  ConsumerState<LocationListScreen> createState() => _LocationListScreenState();
}

class _LocationListScreenState extends ConsumerState<LocationListScreen> {
  bool _showMap = false;

  @override
  Widget build(BuildContext context) {
    final locationsAsync = ref.watch(locationsProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Vị trí'),
        actions: [
          IconButton(
            icon: Icon(_showMap ? Icons.list : Icons.map_outlined),
            tooltip: _showMap ? 'Xem danh sách' : 'Xem bản đồ',
            onPressed: () => setState(() => _showMap = !_showMap),
          ),
        ],
      ),
      body: locationsAsync.when(
        data: (locations) => _buildContent(locations),
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => ErrorStateView(
          message: error is ApiException ? error.message : 'Không thể tải danh sách vị trí.',
          onRetry: () => ref.read(locationsProvider.notifier).refresh(),
        ),
      ),
    );
  }

  Widget _buildContent(List<Location> locations) {
    if (locations.isEmpty) {
      return const EmptyStateView(icon: Icons.location_off_outlined, message: 'Chưa có vị trí nào.');
    }

    return _showMap
        ? LocationMapView(locations: locations)
        : RefreshIndicator(
            onRefresh: () => ref.read(locationsProvider.notifier).refresh(),
            child: LocationListView(locations: locations),
          );
  }
}
