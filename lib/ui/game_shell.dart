import 'package:flame/game.dart';
import 'package:flutter/material.dart';

import '../data/content_registry.dart';
import '../game/echoes_game.dart';

class GameShell extends StatefulWidget {
  const GameShell({super.key, required this.contentRegistry});
  final ContentRegistry contentRegistry;

  @override
  State<GameShell> createState() => _GameShellState();
}

class _GameShellState extends State<GameShell> {
  late final EchoesGame game = EchoesGame(contentRegistry: widget.contentRegistry);

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF120F0E),
      body: Stack(
        children: [
          Positioned.fill(child: GameWidget(game: game)),

          // Top — boss bar + combat log (only after world is ready)
          ValueListenableBuilder<bool>(
            valueListenable: game.isReady,
            builder: (_, ready, __) {
              if (!ready) {
                return const Positioned(
                  top: 32, left: 0, right: 0,
                  child: Center(
                    child: Text('Loading Ruined Olympus...',
                      style: TextStyle(color: Color(0xFFE8B86D),
                        fontSize: 14, fontWeight: FontWeight.w600,
                        letterSpacing: 2)),
                  ),
                );
              }
              return Positioned(
                top: 18, left: 18, right: 18,
                child: Column(
                  children: [
                    ValueListenableBuilder<int>(
                      valueListenable: game.bossHp,
                      builder: (_, hp, __) => ValueListenableBuilder<int>(
                        valueListenable: game.bossPhase,
                        builder: (_, ph, __) => _bossBar(hp, game.bossMaxHp.value, ph),
                      ),
                    ),
                    const SizedBox(height: 8),
                    ValueListenableBuilder<String>(
                      valueListenable: game.combatLog,
                      builder: (_, value, __) => Container(
                        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                        decoration: BoxDecoration(
                          color: const Color(0xCCE8B86D),
                          borderRadius: BorderRadius.circular(8),
                          border: Border.all(color: const Color(0xFFB8860B), width: 1.5),
                        ),
                        child: Text(value, style: const TextStyle(color: Colors.black87, fontWeight: FontWeight.w700)),
                      ),
                    ),
                  ],
                ),
              );
            },
          ),

          // Bottom HUD — only after world is ready
          ValueListenableBuilder<bool>(
            valueListenable: game.isReady,
            builder: (_, ready, __) {
              if (!ready) return const SizedBox.shrink();
              return Positioned(
                bottom: 0, left: 0, right: 0,
                child: Container(
                  padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 14),
                  decoration: const BoxDecoration(
                    gradient: LinearGradient(
                      begin: Alignment.topCenter, end: Alignment.bottomCenter,
                      colors: [Color(0x00000000), Color(0xDD000000)],
                    ),
                  ),
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      ValueListenableBuilder<int>(
                        valueListenable: game.playerHp,
                        builder: (_, hp, __) => _orb(hp, game.playerMaxHp.value, const Color(0xFFE83C3C), 'HP'),
                      ),
                      const SizedBox(width: 10),
                      Expanded(
                        child: Wrap(
                          alignment: WrapAlignment.center,
                          spacing: 6, runSpacing: 6,
                          children: [
                            _skillBtn('titans_slam',    'Slam',     const Color(0xFFC9C9C9)),
                            _skillBtn('inferno_strike', 'Inferno',  const Color(0xFFFF6A1A)),
                            _skillBtn('aegis_ward',     'Aegis',    const Color(0xFFFFD400)),
                            _skillBtn('earthshatter',   'Shatter',  const Color(0xFFC9C9C9)),
                            _skillBtn('frost_nova',     'Frost',    const Color(0xFF4FD3FF)),
                            _skillBtn('void_smite',     'Void',     const Color(0xFF8B2FC9)),
                            _skillBtn('battle_cry',     'Cry',      const Color(0xFFE8B86D)),
                            _skillBtn('chain_lightning','Chain',    const Color(0xFFFFD400)),
                            _skillBtn('shield_charge',  'Charge',   const Color(0xFFC9C9C9)),
                            _skillBtn('colossus_form',  'Colossus', const Color(0xFFB048E8)),
                          ],
                        ),
                      ),
                      const SizedBox(width: 10),
                      ValueListenableBuilder<int>(
                        valueListenable: game.playerMana,
                        builder: (_, mn, __) => _orb(mn, game.playerMaxMana.value, const Color(0xFF4FA8E8), 'MP'),
                      ),
                    ],
                  ),
                ),
              );
            },
          ),

          // Death overlay
          ValueListenableBuilder<bool>(
            valueListenable: game.playerDead,
            builder: (_, dead, __) => dead
                ? Container(
                    color: const Color(0xCC000000),
                    alignment: Alignment.center,
                    child: const Text('YOU DIED',
                        style: TextStyle(color: Color(0xFFFF3030), fontSize: 48, fontWeight: FontWeight.bold, letterSpacing: 4)),
                  )
                : const SizedBox.shrink(),
          ),
          // Victory overlay
          ValueListenableBuilder<bool>(
            valueListenable: game.bossDefeated,
            builder: (_, won, __) => won
                ? Positioned(
                    top: MediaQuery.of(context).size.height * 0.18,
                    left: 0, right: 0,
                    child: Center(
                      child: Container(
                        padding: const EdgeInsets.all(20),
                        decoration: BoxDecoration(
                          color: const Color(0xDD120F0E),
                          border: Border.all(color: const Color(0xFFFF8C1A), width: 3),
                          borderRadius: BorderRadius.circular(12),
                        ),
                        child: const Text('THE FALLEN HOPLITE IS SHATTERED',
                            style: TextStyle(color: Color(0xFFFF8C1A), fontSize: 22, fontWeight: FontWeight.bold)),
                      ),
                    ),
                  )
                : const SizedBox.shrink(),
          ),
        ],
      ),
    );
  }

  Widget _bossBar(int hp, int maxHp, int phase) {
    final ratio = maxHp == 0 ? 0.0 : (hp / maxHp).clamp(0.0, 1.0);
    final phaseLabel = phase == 1 ? 'Phase 1' : (phase == 2 ? 'Phase 2 — Enraged' : 'Phase 3 — Shattered Fury');
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
      decoration: BoxDecoration(
        color: const Color(0xDD6B6358),
        border: Border.all(color: const Color(0xFFB8860B), width: 2),
        borderRadius: BorderRadius.circular(10),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Text('THE FALLEN HOPLITE',
                  style: TextStyle(color: Color(0xFFE8B86D), fontWeight: FontWeight.bold, letterSpacing: 1.4)),
              const Spacer(),
              Text(phaseLabel, style: const TextStyle(color: Color(0xFFFF8C1A), fontWeight: FontWeight.w600)),
            ],
          ),
          const SizedBox(height: 6),
          Container(
            height: 16,
            decoration: BoxDecoration(color: const Color(0xFF222222), borderRadius: BorderRadius.circular(4)),
            child: FractionallySizedBox(
              alignment: Alignment.centerLeft,
              widthFactor: ratio,
              child: Container(
                decoration: const BoxDecoration(
                  gradient: LinearGradient(colors: [Color(0xFFFF3030), Color(0xFF8B0000)]),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _orb(int value, int max, Color color, String label) {
    final ratio = max == 0 ? 0.0 : (value / max).clamp(0.0, 1.0);
    return Container(
      width: 76, height: 76,
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        border: Border.all(color: const Color(0xFFB8860B), width: 3),
        color: Colors.black,
        boxShadow: [BoxShadow(color: color.withOpacity(0.45), blurRadius: 16)],
      ),
      child: Stack(
        alignment: Alignment.center,
        children: [
          ClipOval(
            child: Align(
              alignment: Alignment.bottomCenter,
              heightFactor: ratio,
              child: Container(color: color.withOpacity(0.85), width: 76),
            ),
          ),
          Text('$value',
              style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 14,
                  shadows: [Shadow(offset: Offset(1, 1), blurRadius: 2, color: Colors.black)])),
          Positioned(
            top: 4,
            child: Text(label, style: const TextStyle(color: Colors.white70, fontSize: 9, fontWeight: FontWeight.bold)),
          ),
        ],
      ),
    );
  }

  Widget _skillBtn(String id, String label, Color color) {
    return GestureDetector(
      onTap: () async {
        await game.castSkill(id);
        if (mounted) setState(() {});
      },
      child: Container(
        width: 54, height: 54,
        decoration: BoxDecoration(
          color: const Color(0xFF1E1A18),
          border: Border.all(color: const Color(0xFFB8860B), width: 2),
          borderRadius: BorderRadius.circular(8),
          boxShadow: [BoxShadow(color: color.withOpacity(0.35), blurRadius: 10)],
        ),
        child: Stack(
          children: [
            Center(
              child: Text(label,
                  style: TextStyle(color: color, fontWeight: FontWeight.bold, fontSize: 10)),
            ),
            Positioned(
              bottom: 2, right: 4,
              child: ValueListenableBuilder(
                valueListenable: game.playerMana,
                builder: (_, __, ___) {
                  final cd = game.cooldownOf(id);
                  return cd > 0
                      ? Text(cd.toStringAsFixed(1),
                          style: const TextStyle(color: Colors.white70, fontSize: 9, fontWeight: FontWeight.w600))
                      : const SizedBox.shrink();
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
}
