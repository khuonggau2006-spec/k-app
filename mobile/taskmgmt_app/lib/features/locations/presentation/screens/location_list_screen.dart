import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/network/api_exception.dart';
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
        error: (error, _) => _ErrorState(
          message: error is ApiException ? error.message : 'Không thể tải danh sách vị trí.',
          onRetry: () => ref.read(locationsProvider.notifier).refresh(),
        ),
      ),
    );
  }

  Widget _buildContent(List<Location> locations) {
    if (locations.isEmpty) {
      return const _EmptyState();
    }

    return _showMap
        ? LocationMapView(locations: locations)
        : RefreshIndicator(
            onRefresh: () => ref.read(locationsProvider.notifier).refresh(),
            child: LocationListView(locations: locations),
          );
  }
}

class _EmptyState extends StatelessWidget {
  const _EmptyState();

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.location_off_outlined, size: 48, color: Theme.of(context).colorScheme.outline),
          const SizedBox(height: 16),
          const Text('Chưa có vị trí nào.'),
        ],
      ),
    );
  }
}

class _ErrorState extends StatelessWidget {
  const _ErrorState({required this.message, required this.onRetry});

  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.error_outline, size: 48, color: Theme.of(context).colorScheme.error),
            const SizedBox(height: 16),
            Text(message, textAlign: TextAlign.center),
            const SizedBox(height: 16),
            FilledButton(onPressed: onRetry, child: const Text('Thử lại')),
          ],
        ),
      ),
    );
  }
}
