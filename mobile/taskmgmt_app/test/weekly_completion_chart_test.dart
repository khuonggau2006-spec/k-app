import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:taskmgmt_app/features/dashboard/domain/entities/weekly_completion.dart';
import 'package:taskmgmt_app/features/dashboard/presentation/widgets/weekly_completion_chart.dart';

void main() {
  testWidgets('Shows an empty state when no week has any completed task', (tester) async {
    final data = [
      WeeklyCompletion(weekStartDate: DateTime(2026, 8, 3), completedCount: 0),
      WeeklyCompletion(weekStartDate: DateTime(2026, 8, 10), completedCount: 0),
    ];

    await tester.pumpWidget(MaterialApp(home: Scaffold(body: WeeklyCompletionChart(data: data))));

    expect(find.text('Chưa có công việc hoàn thành trong 8 tuần qua.'), findsOneWidget);
    expect(find.byType(BarChart), findsNothing);
  });

  testWidgets('Renders a bar chart with a bar per week when data has completions', (tester) async {
    final data = [
      WeeklyCompletion(weekStartDate: DateTime(2026, 8, 3), completedCount: 2),
      WeeklyCompletion(weekStartDate: DateTime(2026, 8, 10), completedCount: 0),
      WeeklyCompletion(weekStartDate: DateTime(2026, 8, 17), completedCount: 5),
    ];

    await tester.pumpWidget(MaterialApp(home: Scaffold(body: WeeklyCompletionChart(data: data))));

    expect(find.byType(BarChart), findsOneWidget);
    final chart = tester.widget<BarChart>(find.byType(BarChart));
    expect(chart.data.barGroups.length, 3);
    expect(chart.data.barGroups[0].barRods.single.toY, 2);
    expect(chart.data.barGroups[1].barRods.single.toY, 0);
    expect(chart.data.barGroups[2].barRods.single.toY, 5);
  });

  testWidgets('Shows dd/MM week-start labels on the horizontal axis', (tester) async {
    final data = [
      WeeklyCompletion(weekStartDate: DateTime(2026, 8, 3), completedCount: 1),
      WeeklyCompletion(weekStartDate: DateTime(2026, 8, 10), completedCount: 1),
    ];

    await tester.pumpWidget(MaterialApp(home: Scaffold(body: WeeklyCompletionChart(data: data))));
    await tester.pump();

    expect(find.text('03/08'), findsOneWidget);
    expect(find.text('10/08'), findsOneWidget);
  });
}
