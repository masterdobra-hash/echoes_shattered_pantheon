import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import 'data/content_registry.dart';
import 'ui/game_shell.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await SystemChrome.setPreferredOrientations([
    DeviceOrientation.landscapeLeft, DeviceOrientation.landscapeRight,
  ]);
  await SystemChrome.setEnabledSystemUIMode(SystemUiMode.immersiveSticky);
  final registry = ContentRegistry();
  await registry.load();
  runApp(EchoesApp(registry: registry));
}

class EchoesApp extends StatelessWidget {
  const EchoesApp({super.key, required this.registry});
  final ContentRegistry registry;

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      title: 'Echoes of the Shattered Pantheon',
      theme: ThemeData.dark(useMaterial3: true).copyWith(
        colorScheme: const ColorScheme.dark(
          primary: Color(0xFFB8860B),
          secondary: Color(0xFFE8B86D),
          surface: Color(0xFF6B6358),
        ),
      ),
      home: GameShell(contentRegistry: registry),
    );
  }
}
