import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import 'data/content_registry.dart';
import 'ui/game_shell.dart';

/// Bright loading screen — always shows immediately,
/// so user NEVER sees pure black during boot.
class _SplashScreen extends StatelessWidget {
  const _SplashScreen({this.message = 'Loading Ruined Olympus...'});
  final String message;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF2A1810),
      body: SafeArea(
        child: Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const SizedBox(
                width: 64, height: 64,
                child: CircularProgressIndicator(
                  color: Color(0xFFFFB142), strokeWidth: 5,
                ),
              ),
              const SizedBox(height: 28),
              const Text('ECHOES OF THE SHATTERED PANTHEON',
                  textAlign: TextAlign.center,
                  style: TextStyle(color: Color(0xFFFFB142),
                    fontSize: 16, fontWeight: FontWeight.bold, letterSpacing: 2)),
              const SizedBox(height: 12),
              Text(message,
                  style: const TextStyle(color: Colors.white70, fontSize: 13)),
            ],
          ),
        ),
      ),
    );
  }
}

class _ErrorScreen extends StatelessWidget {
  const _ErrorScreen({required this.error, this.stack});
  final Object error;
  final StackTrace? stack;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF2A0E0E),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(20),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text('STARTUP ERROR',
                  style: TextStyle(color: Color(0xFFFF6B6B),
                    fontSize: 22, fontWeight: FontWeight.bold)),
              const SizedBox(height: 12),
              Text(error.toString(),
                  style: const TextStyle(color: Colors.white, fontSize: 14)),
              const SizedBox(height: 14),
              Text(stack?.toString() ?? '',
                  style: const TextStyle(color: Colors.white60,
                    fontSize: 11, fontFamily: 'monospace')),
            ],
          ),
        ),
      ),
    );
  }
}

/// Theme used everywhere — never depends on initialization state.
final ThemeData _appTheme = ThemeData.dark(useMaterial3: true).copyWith(
  scaffoldBackgroundColor: const Color(0xFF2A1810),
  colorScheme: const ColorScheme.dark(
    primary: Color(0xFFB8860B),
    secondary: Color(0xFFE8B86D),
    surface: Color(0xFF6B6358),
  ),
);

void main() {
  // Capture framework errors visually — never let them silently produce black screen
  FlutterError.onError = (FlutterErrorDetails details) {
    FlutterError.presentError(details);
    debugPrint('[ECHOES][FlutterError] ${details.exceptionAsString()}');
  };

  runZonedGuarded<void>(() async {
    // FIRST: show a non-black UI immediately so the user always sees SOMETHING
    WidgetsFlutterBinding.ensureInitialized();
    debugPrint('[ECHOES] WidgetsFlutterBinding ready');

    // Mount the app right away with a splash. The registry loads in the background.
    final completer = Completer<ContentRegistry>();
    runApp(_BootApp(future: completer.future));

    // After first frame, do the heavy stuff so the splash is visible
    WidgetsBinding.instance.addPostFrameCallback((_) async {
      try {
        debugPrint('[ECHOES] post-frame: locking orientation');
        await SystemChrome.setPreferredOrientations(<DeviceOrientation>[
          DeviceOrientation.landscapeLeft,
          DeviceOrientation.landscapeRight,
        ]);
        await SystemChrome.setEnabledSystemUIMode(SystemUiMode.immersiveSticky);
        debugPrint('[ECHOES] orientation locked, loading content');

        final registry = ContentRegistry();
        await registry.load();
        debugPrint('[ECHOES] registry loaded: '
            'skills=${registry.skills.length}, '
            'shards=${registry.shards.length}, '
            'enemies=${registry.enemies.length}');
        completer.complete(registry);
      } catch (e, st) {
        debugPrint('[ECHOES][boot fail] $e\n$st');
        completer.completeError(e, st);
      }
    });
  }, (error, stack) {
    debugPrint('[ECHOES][zone error] $error\n$stack');
  });
}

class _BootApp extends StatelessWidget {
  const _BootApp({required this.future});
  final Future<ContentRegistry> future;

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      title: 'Echoes of the Shattered Pantheon',
      theme: _appTheme,
      home: FutureBuilder<ContentRegistry>(
        future: future,
        builder: (context, snap) {
          if (snap.connectionState != ConnectionState.done) {
            return const _SplashScreen();
          }
          if (snap.hasError) {
            return _ErrorScreen(error: snap.error!, stack: snap.stackTrace);
          }
          return GameShell(contentRegistry: snap.data!);
        },
      ),
    );
  }
}
