import "package:flutter/material.dart";
import "package:flutter/services.dart";

import "data/content_registry.dart";
import "ui/game_shell.dart";

Future<void> main() async {
  // Catch ALL errors visually instead of producing a black screen.
  runZonedAndCatch(() async {
    WidgetsFlutterBinding.ensureInitialized();
    final registry = ContentRegistry();
    runApp(BootstrapApp(future: _boot(registry), registry: registry));
  });
}

Future<void> _boot(ContentRegistry registry) async {
  // Defer orientation lock until AFTER first frame to avoid race on Android 14+
  await Future<void>.delayed(const Duration(milliseconds: 50));
  try {
    await SystemChrome.setPreferredOrientations([
      DeviceOrientation.landscapeLeft,
      DeviceOrientation.landscapeRight,
    ]);
    await SystemChrome.setEnabledSystemUIMode(SystemUiMode.immersiveSticky);
  } catch (_) {/* non-fatal */}
  await registry.load();
}

void runZonedAndCatch(void Function() body) {
  // Pipe Flutter framework errors to default red-screen error widget,
  // do NOT hide them silently.
  FlutterError.onError = (details) {
    FlutterError.presentError(details);
  };
  body();
}

class BootstrapApp extends StatelessWidget {
  const BootstrapApp({super.key, required this.future, required this.registry});
  final Future<void> future;
  final ContentRegistry registry;

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      title: "Echoes of the Shattered Pantheon",
      theme: ThemeData.dark(useMaterial3: true).copyWith(
        colorScheme: const ColorScheme.dark(
          primary: Color(0xFFB8860B),
          secondary: Color(0xFFE8B86D),
          surface: Color(0xFF6B6358),
        ),
      ),
      home: FutureBuilder<void>(
        future: future,
        builder: (context, snap) {
          if (snap.connectionState != ConnectionState.done) {
            return const _LoadingScreen();
          }
          if (snap.hasError) {
            return _ErrorScreen(error: snap.error, stack: snap.stackTrace);
          }
          return GameShell(contentRegistry: registry);
        },
      ),
    );
  }
}

class _LoadingScreen extends StatelessWidget {
  const _LoadingScreen();

  @override
  Widget build(BuildContext context) {
    return const Scaffold(
      backgroundColor: Color(0xFF120F0E),
      body: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            SizedBox(
              width: 56, height: 56,
              child: CircularProgressIndicator(
                color: Color(0xFFB8860B), strokeWidth: 4,
              ),
            ),
            SizedBox(height: 20),
            Text("ECHOES OF THE SHATTERED PANTHEON",
                style: TextStyle(color: Color(0xFFE8B86D),
                  fontWeight: FontWeight.bold, letterSpacing: 2)),
            SizedBox(height: 8),
            Text("Loading Ruined Olympus...",
                style: TextStyle(color: Colors.white70, fontSize: 12)),
          ],
        ),
      ),
    );
  }
}

class _ErrorScreen extends StatelessWidget {
  const _ErrorScreen({this.error, this.stack});
  final Object? error;
  final StackTrace? stack;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF1B0F0F),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(20),
          child: SingleChildScrollView(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text("STARTUP ERROR",
                    style: TextStyle(color: Color(0xFFFF3030),
                      fontSize: 22, fontWeight: FontWeight.bold)),
                const SizedBox(height: 12),
                Text("\$error", style: const TextStyle(color: Colors.white)),
                const SizedBox(height: 12),
                Text("\${stack ?? \"\"}",
                    style: const TextStyle(color: Colors.white60, fontSize: 11,
                      fontFamily: "monospace")),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
