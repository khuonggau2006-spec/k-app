import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'app.dart';
import 'core/di/injection.dart';

void main() {
  setupLocator();
  runApp(const ProviderScope(child: TaskMgmtApp()));
}
