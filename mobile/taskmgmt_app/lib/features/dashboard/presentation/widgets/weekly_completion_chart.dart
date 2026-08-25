import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../../../shared/widgets/inline_empty_state.dart';
import '../../domain/entities/weekly_completion.dart';

class WeeklyCompletionChart extends StatelessWidget {
  const WeeklyCompletionChart({super.key, required this.data});

  final List<WeeklyCompletion> data;

  @override
  Widget build(BuildContext context) {
    final hasCompletions = data.any((week) => week.completedCount > 0);
    if (!hasCompletions) {
      return const InlineEmptyState(
        icon: Icons.bar_chart_outlined,
        message: 'Chưa có công việc hoàn thành trong 8 tuần qua.',
      );
    }

    final maxCount = data.map((week) => week.completedCount).reduce((a, b) => a > b ? a : b);
    final dateFormat = DateFormat('dd/MM');

    return SizedBox(
      height: 180,
      child: BarChart(
        BarChartData(
          maxY: (maxCount + 1).toDouble(),
          barGroups: [
            for (var i = 0; i < data.length; i++)
              BarChartGroupData(
                x: i,
                barRods: [
                  BarChartRodData(
                    toY: data[i].completedCount.toDouble(),
                    color: Theme.of(context).colorScheme.primary,
                    width: 16,
                    borderRadius: const BorderRadius.vertical(top: Radius.circular(4)),
                  ),
                ],
              ),
          ],
          titlesData: FlTitlesData(
            leftTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
            rightTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
            topTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
            bottomTitles: AxisTitles(
              sideTitles: SideTitles(
                showTitles: true,
                interval: 1,
                getTitlesWidget: (value, meta) {
                  final index = value.toInt();
                  if (index < 0 || index >= data.length) return const SizedBox.shrink();
                  return Padding(
                    padding: const EdgeInsets.only(top: 4),
                    child: Text(dateFormat.format(data[index].weekStartDate), style: Theme.of(context).textTheme.bodySmall),
                  );
                },
              ),
            ),
          ),
          gridData: const FlGridData(show: false),
          borderData: FlBorderData(show: false),
        ),
      ),
    );
  }
}
