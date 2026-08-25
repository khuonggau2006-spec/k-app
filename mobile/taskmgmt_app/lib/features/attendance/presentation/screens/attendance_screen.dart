import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:geolocator/geolocator.dart';
import 'package:intl/intl.dart';

import '../../../../core/network/api_exception.dart';
import '../../../../shared/widgets/empty_state_view.dart';
import '../../../../shared/widgets/error_state_view.dart';
import '../../domain/entities/attendance_record.dart';
import '../providers/attendance_provider.dart';

class LocationAccessException implements Exception {
  LocationAccessException(this.message);
  final String message;
}

String attendanceErrorMessage(Object error, String fallback) {
  if (error is ApiException) return error.message;
  if (error is LocationAccessException) return error.message;
  return fallback;
}

class AttendanceScreen extends ConsumerStatefulWidget {
  const AttendanceScreen({super.key});

  static const path = '/attendance';
  static const name = 'attendance';

  @override
  ConsumerState<AttendanceScreen> createState() => _AttendanceScreenState();
}

class _AttendanceScreenState extends ConsumerState<AttendanceScreen> {
  bool _isSubmitting = false;
  DateTime _selectedMonth = DateTime.now();

  Future<Position> _getCurrentPosition() async {
    var permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
    }
    if (permission == LocationPermission.denied || permission == LocationPermission.deniedForever) {
      throw LocationAccessException('Cần quyền truy cập vị trí để chấm công.');
    }
    return Geolocator.getCurrentPosition();
  }

  Future<void> _checkIn() async {
    setState(() => _isSubmitting = true);
    try {
      final position = await _getCurrentPosition();
      await ref
          .read(todayAttendanceProvider.notifier)
          .checkIn(latitude: position.latitude, longitude: position.longitude);
    } catch (e) {
      if (!mounted) return;
      final message = attendanceErrorMessage(e, 'Không thể check-in.');
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  Future<void> _checkOut() async {
    setState(() => _isSubmitting = true);
    try {
      final position = await _getCurrentPosition();
      await ref
          .read(todayAttendanceProvider.notifier)
          .checkOut(latitude: position.latitude, longitude: position.longitude);
    } catch (e) {
      if (!mounted) return;
      final message = attendanceErrorMessage(e, 'Không thể check-out.');
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return DefaultTabController(
      length: 2,
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Chấm công'),
          bottom: const TabBar(tabs: [Tab(text: 'Check-in'), Tab(text: 'Lịch sử')]),
        ),
        body: TabBarView(
          children: [_buildCheckInTab(), _buildHistoryTab()],
        ),
      ),
    );
  }

  Widget _buildCheckInTab() {
    final todayAsync = ref.watch(todayAttendanceProvider);

    return todayAsync.when(
      data: (today) => Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            _buildStatusCard(today),
            const SizedBox(height: 24),
            Row(
              children: [
                Expanded(
                  child: FilledButton(
                    onPressed: _isSubmitting || (today?.isCheckedIn ?? false) ? null : _checkIn,
                    child: const Text('CHECK-IN'),
                  ),
                ),
                const SizedBox(width: 16),
                Expanded(
                  child: OutlinedButton(
                    onPressed: _isSubmitting || !(today?.isCheckedIn ?? false) || (today?.isCheckedOut ?? false)
                        ? null
                        : _checkOut,
                    child: const Text('CHECK-OUT'),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
      loading: () => const Center(child: CircularProgressIndicator()),
      error: (error, _) => ErrorStateView(
        message: error is ApiException ? error.message : 'Không tải được trạng thái chấm công.',
        onRetry: () => ref.invalidate(todayAttendanceProvider),
      ),
    );
  }

  Widget _buildStatusCard(AttendanceRecord? today) {
    final format = DateFormat('HH:mm');
    String text;
    if (today == null || !today.isCheckedIn) {
      text = 'Chưa check-in hôm nay.';
    } else if (!today.isCheckedOut) {
      text = 'Đã check-in lúc ${format.format(today.checkInAtUtc!.toLocal())} tại ${today.checkInLocationName}.';
    } else {
      text = 'Hoàn thành: ${format.format(today.checkInAtUtc!.toLocal())} → '
          '${format.format(today.checkOutAtUtc!.toLocal())}';
    }
    return Card(child: Padding(padding: const EdgeInsets.all(16), child: Text(text)));
  }

  Widget _buildHistoryTab() {
    final args = (year: _selectedMonth.year, month: _selectedMonth.month);
    final historyAsync = ref.watch(attendanceHistoryProvider(args));
    final statsAsync = ref.watch(attendanceStatsProvider(args));

    return Column(
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            IconButton(
              icon: const Icon(Icons.chevron_left),
              onPressed: () => setState(
                () => _selectedMonth = DateTime(_selectedMonth.year, _selectedMonth.month - 1),
              ),
            ),
            Text('Tháng ${_selectedMonth.month}/${_selectedMonth.year}'),
            IconButton(
              icon: const Icon(Icons.chevron_right),
              onPressed: () => setState(
                () => _selectedMonth = DateTime(_selectedMonth.year, _selectedMonth.month + 1),
              ),
            ),
          ],
        ),
        statsAsync.when(
          data: (stats) => Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: Text('${stats.daysCheckedIn} ngày đã check-in · ${stats.totalHoursWorked.toStringAsFixed(1)} giờ làm'),
          ),
          loading: () => const SizedBox.shrink(),
          error: (_, _) => const SizedBox.shrink(),
        ),
        Expanded(
          child: historyAsync.when(
            data: (records) {
              if (records.isEmpty) {
                return const EmptyStateView(icon: Icons.event_busy_outlined, message: 'Chưa có dữ liệu chấm công.');
              }
              final format = DateFormat('HH:mm');
              return ListView.separated(
                padding: const EdgeInsets.all(8),
                itemCount: records.length,
                separatorBuilder: (context, index) => const Divider(height: 1),
                itemBuilder: (context, index) {
                  final record = records[index];
                  final checkOutText = record.isCheckedOut ? format.format(record.checkOutAtUtc!.toLocal()) : '--:--';
                  return ListTile(
                    title: Text(
                      '${DateFormat('dd/MM/yyyy').format(record.workDate)}: '
                      '${format.format(record.checkInAtUtc!.toLocal())} → $checkOutText',
                    ),
                    subtitle: Text(record.checkInLocationName ?? ''),
                  );
                },
              );
            },
            loading: () => const Center(child: CircularProgressIndicator()),
            error: (error, _) => ErrorStateView(
              message: error is ApiException ? error.message : 'Không tải được lịch sử chấm công.',
              onRetry: () => ref.invalidate(attendanceHistoryProvider(args)),
            ),
          ),
        ),
      ],
    );
  }
}
