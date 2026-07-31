import 'package:flutter/material.dart';

import '../../domain/entities/location.dart';

class LocationListView extends StatelessWidget {
  const LocationListView({super.key, required this.locations});

  final List<Location> locations;

  @override
  Widget build(BuildContext context) {
    return ListView.separated(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.all(8),
      itemCount: locations.length,
      separatorBuilder: (context, index) => const Divider(height: 1),
      itemBuilder: (context, index) {
        final location = locations[index];
        return ListTile(
          leading: const CircleAvatar(child: Icon(Icons.location_on_outlined)),
          title: Text(location.name),
          subtitle: Text(
            [
              if (location.address != null && location.address!.isNotEmpty) location.address!,
              '${location.latitude.toStringAsFixed(5)}, ${location.longitude.toStringAsFixed(5)}',
            ].join('\n'),
          ),
          isThreeLine: location.address != null && location.address!.isNotEmpty,
        );
      },
    );
  }
}
