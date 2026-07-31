import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';

import '../../domain/entities/location.dart';

class LocationMapView extends StatelessWidget {
  const LocationMapView({super.key, required this.locations});

  final List<Location> locations;

  static const _defaultCenter = LatLng(16.0, 106.0); // Trung tâm Việt Nam, dùng khi không có vị trí nào.

  @override
  Widget build(BuildContext context) {
    final center = _computeCenter();

    return FlutterMap(
      options: MapOptions(
        initialCenter: center,
        initialZoom: locations.length == 1 ? 15 : 6,
      ),
      children: [
        TileLayer(
          urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
          userAgentPackageName: 'com.taskmgmt.taskmgmt_app',
        ),
        MarkerLayer(
          markers: locations
              .map(
                (location) => Marker(
                  point: LatLng(location.latitude, location.longitude),
                  width: 40,
                  height: 40,
                  child: GestureDetector(
                    onTap: () => _showLocationInfo(context, location),
                    child: const Icon(Icons.location_on, color: Colors.red, size: 40),
                  ),
                ),
              )
              .toList(),
        ),
        RichAttributionWidget(
          attributions: [
            TextSourceAttribution(
              'OpenStreetMap contributors',
              onTap: () {},
            ),
          ],
        ),
      ],
    );
  }

  LatLng _computeCenter() {
    if (locations.isEmpty) return _defaultCenter;

    final avgLat = locations.map((l) => l.latitude).reduce((a, b) => a + b) / locations.length;
    final avgLng = locations.map((l) => l.longitude).reduce((a, b) => a + b) / locations.length;
    return LatLng(avgLat, avgLng);
  }

  void _showLocationInfo(BuildContext context, Location location) {
    showModalBottomSheet(
      context: context,
      builder: (context) => SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(location.name, style: Theme.of(context).textTheme.titleMedium),
              if (location.address != null && location.address!.isNotEmpty) ...[
                const SizedBox(height: 4),
                Text(location.address!),
              ],
              const SizedBox(height: 4),
              Text(
                '${location.latitude.toStringAsFixed(5)}, ${location.longitude.toStringAsFixed(5)}',
                style: Theme.of(context).textTheme.bodySmall,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
