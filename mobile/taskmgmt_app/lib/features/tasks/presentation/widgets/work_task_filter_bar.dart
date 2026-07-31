import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../locations/presentation/providers/location_provider.dart';
import '../../domain/entities/work_task.dart';
import '../providers/work_task_provider.dart';

class WorkTaskFilterBar extends ConsumerWidget {
  const WorkTaskFilterBar({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final filter = ref.watch(workTaskFilterProvider);
    final locationsAsync = ref.watch(locationsProvider);

    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      child: Row(
        children: [
          Expanded(
            child: DropdownButtonFormField<WorkTaskStatus?>(
              initialValue: filter.status,
              isExpanded: true,
              decoration: const InputDecoration(labelText: 'Trạng thái', isDense: true),
              items: [
                const DropdownMenuItem(value: null, child: Text('Tất cả')),
                ...WorkTaskStatus.values.map(
                  (status) => DropdownMenuItem(value: status, child: Text(workTaskStatusLabel(status))),
                ),
              ],
              onChanged: (value) {
                ref.read(workTaskFilterProvider.notifier).state =
                    filter.copyWith(status: value, clearStatus: value == null);
              },
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: locationsAsync.when(
              data: (locations) => DropdownButtonFormField<String?>(
                initialValue: filter.locationId,
                isExpanded: true,
                decoration: const InputDecoration(labelText: 'Vị trí', isDense: true),
                items: [
                  const DropdownMenuItem(value: null, child: Text('Tất cả')),
                  ...locations.map(
                    (location) => DropdownMenuItem(
                      value: location.id,
                      child: Text(location.name, overflow: TextOverflow.ellipsis),
                    ),
                  ),
                ],
                onChanged: (value) {
                  ref.read(workTaskFilterProvider.notifier).state =
                      filter.copyWith(locationId: value, clearLocationId: value == null);
                },
              ),
              loading: () => DropdownButtonFormField<String?>(
                items: const [],
                onChanged: null,
                decoration: const InputDecoration(labelText: 'Vị trí', isDense: true, hintText: 'Đang tải...'),
              ),
              error: (_, _) => DropdownButtonFormField<String?>(
                items: const [],
                onChanged: null,
                decoration: const InputDecoration(labelText: 'Vị trí', isDense: true, hintText: 'Không tải được'),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
