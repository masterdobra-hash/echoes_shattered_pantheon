import 'dart:math' as math;
import 'package:flame/components.dart';
import 'package:flame/events.dart';
import 'package:flame/experimental.dart';
import 'package:flame/game.dart';
import 'package:flame_audio/flame_audio.dart';
import 'package:flutter/material.dart';

import '../data/content_registry.dart';
import '../entities/enemy/enemy_component.dart';
import '../entities/loot/loot_drop_component.dart';
import '../entities/player/titan_warrior_component.dart';
import '../systems/loot_system.dart';
import '../vfx/telegraph_component.dart';
import 'ground_tile_layer.dart';

/// Top-down isometric ARPG (Diablo 2 lineage).
class EchoesGame extends FlameGame with TapCallbacks {
  EchoesGame({required this.contentRegistry})
      : super(
          camera: CameraComponent.withFixedResolution(width: 1280, height: 720),
        );

  final ContentRegistry contentRegistry;
  final LootSystem lootSystem = LootSystem();

  final ValueNotifier<bool> isReady = ValueNotifier<bool>(false);
  final ValueNotifier<String> combatLog =
      ValueNotifier<String>('Ruined Olympus — tap the ground to move, tap skills to fight.');
  final ValueNotifier<int> playerHp = ValueNotifier<int>(220);
  final ValueNotifier<int> playerMaxHp = ValueNotifier<int>(220);
  final ValueNotifier<int> playerMana = ValueNotifier<int>(100);
  final ValueNotifier<int> playerMaxMana = ValueNotifier<int>(100);
  final ValueNotifier<int> bossHp = ValueNotifier<int>(1400);
  final ValueNotifier<int> bossMaxHp = ValueNotifier<int>(1400);
  final ValueNotifier<int> bossPhase = ValueNotifier<int>(1);
  final ValueNotifier<bool> bossDefeated = ValueNotifier<bool>(false);
  final ValueNotifier<bool> playerDead = ValueNotifier<bool>(false);

  static final Vector2 worldSize = Vector2(1800, 1400);

  World? gameWorld;
  TitanWarriorComponent? player;
  EnemyComponent? boss;
  final List<EnemyComponent> trash = [];
  bool _audioReady = false;

  @override
  Future<void> onLoad() async {
    final w = World();
    gameWorld = w;
    await add(w);
    camera.world = w;

    _loadAudioBackground();

    await w.add(GroundTileLayer(size: worldSize));

    final p = TitanWarriorComponent(
      contentRegistry: contentRegistry,
      position: Vector2(worldSize.x / 2, worldSize.y / 2 + 220),
    );
    player = p;
    playerHp.value = p.currentHp;
    playerMaxHp.value = p.maxHp;
    playerMana.value = p.currentMana;
    playerMaxMana.value = p.maxMana;
    await w.add(p);

    final bossDef = contentRegistry.enemies['the_fallen_hoplite']!;
    final b = EnemyComponent(
      definition: bossDef,
      position: Vector2(worldSize.x / 2, 320),
      isBoss: true,
    );
    boss = b;
    bossMaxHp.value = b.maxHp;
    bossHp.value = b.currentHp;
    await w.add(b);

    final rng = math.Random(7);
    final roster = ['broken_hoplite', 'temple_archer', 'marble_guardian', 'titan_spawn'];
    for (var i = 0; i < 6; i++) {
      final id = roster[i % roster.length];
      final def = contentRegistry.enemies[id]!;
      final mob = EnemyComponent(
        definition: def,
        position: Vector2(
          400 + rng.nextDouble() * (worldSize.x - 800),
          500 + rng.nextDouble() * 400,
        ),
      );
      trash.add(mob);
      await w.add(mob);
    }

    camera.follow(p, snap: true);
    isReady.value = true;
  }

  Future<void> _loadAudioBackground() async {
    try {
      await FlameAudio.audioCache.loadAll([
        'sfx_titan_slam.mp3',
        'sfx_inferno_strike.mp3',
        'sfx_aegis_ward.mp3',
        'sfx_boss_telegraph.mp3',
        'sfx_loot_drop.mp3',
        'sfx_enemy_death.mp3',
        'bgm_ruined_olympus.mp3',
        'bgm_fallen_hoplite.mp3',
      ]);
      _audioReady = true;
      FlameAudio.bgm.initialize();
      await FlameAudio.bgm.play('bgm_ruined_olympus.mp3', volume: 0.45);
    } catch (_) {
      _audioReady = false;
    }
  }

  @override
  void render(Canvas canvas) {
    final r = canvas.getLocalClipBounds();
    canvas.drawRect(r, Paint()..color = const Color(0xFF1E1A18));
    super.render(canvas);
  }

  @override
  void update(double dt) {
    super.update(dt);
    final p = player;
    final b = boss;
    final w = gameWorld;
    if (p == null || b == null || w == null) return;
    if (!p.isAlive) {
      playerDead.value = true;
      return;
    }
    playerHp.value = p.currentHp;
    playerMana.value = p.currentMana;
    bossHp.value = b.currentHp;
    bossPhase.value = b.phase;

    for (final mob in trash) {
      if (!mob.isAlive) continue;
      final delta = p.position - mob.position;
      final dist = delta.length;
      final canMove = mob.freezeTimer <= 0;
      if (canMove && dist > 30) {
        mob.position += delta.normalized() * mob.definition.moveSpeed * dt;
      } else if (dist < 50) {
        if (math.Random().nextDouble() < dt * 0.7) {
          p.takeDamage(mob.definition.damage);
          combatLog.value = '${mob.definition.name} hits you for ${mob.definition.damage}';
        }
      }
    }

    final attack = b.tickAttack(p.position);
    if (attack != null) {
      _spawnBossTelegraphAndStrike(attack.center, attack.radius, attack.damage);
    }

    if (!b.isAlive && !b.lootDropped) {
      b.lootDropped = true;
      bossDefeated.value = true;
      _onBossKilled();
    }
  }

  Future<void> _spawnBossTelegraphAndStrike(Vector2 center, double radius, int damage) async {
    final w = gameWorld; final p = player;
    if (w == null || p == null) return;
    await w.add(TelegraphComponent(position: center, radius: radius, lifetime: 0.9));
    if (_audioReady) FlameAudio.play('sfx_boss_telegraph.mp3', volume: 0.7);
    Future.delayed(const Duration(milliseconds: 900), () {
      if (!p.isAlive) return;
      if (p.position.distanceTo(center) <= radius) {
        p.takeDamage(damage);
        combatLog.value = 'Telegraph hits! -$damage HP';
        w.add(HitFlashComponent(
          position: p.position, color: const Color(0xFFFF3030), radius: 50,
        ));
      }
    });
  }

  void _onBossKilled() {
    final w = gameWorld; final b = boss;
    if (w == null || b == null) return;
    combatLog.value = 'The Fallen Hoplite is SHATTERED! Loot drops on the marble.';
    if (_audioReady) {
      FlameAudio.bgm.stop();
      FlameAudio.play('sfx_enemy_death.mp3', volume: 1.0);
    }
    final drops = lootSystem.rollBossLoot();
    for (final drop in drops) {
      w.add(LootDropComponent(
        position: b.position + Vector2(drop.offsetX, drop.offsetY + 20),
        rarityColor: drop.rarityColor,
        label: drop.label,
      ));
    }
    if (_audioReady) {
      Future.delayed(const Duration(milliseconds: 300), () {
        FlameAudio.play('sfx_loot_drop.mp3', volume: 0.9);
      });
    }
  }

  @override
  void onTapDown(TapDownEvent event) {
    final p = player;
    if (p == null) return;
    final worldPoint = camera.globalToLocal(event.canvasPosition);
    p.commandMoveTo(worldPoint);
  }

  Future<bool> castSkill(String skillId) async {
    final p = player; final b = boss; final w = gameWorld;
    if (p == null || b == null || w == null) return false;
    if (!p.isAlive) return false;
    final result = await p.castSkill(skillId, target: b.position);
    if (result == null) return false;

    if (_audioReady) {
      final sfx = switch (skillId) {
        'titans_slam'    => 'sfx_titan_slam.mp3',
        'inferno_strike' => 'sfx_inferno_strike.mp3',
        'aegis_ward'     => 'sfx_aegis_ward.mp3',
        _ => 'sfx_titan_slam.mp3',
      };
      FlameAudio.play(sfx, volume: 0.85);
    }

    final impactColor = _impactColorForType(result.damageType);
    w.add(HitFlashComponent(
      position: p.position, color: impactColor, radius: result.radius * 0.6,
    ));

    final candidates = <EnemyComponent>[b, ...trash];
    final hit = candidates.where((e) => e.isAlive && e.position.distanceTo(p.position) < result.radius).toList();
    for (final e in hit) {
      e.takeDamage(result.damage, result.statusEffect);
      w.add(FloatingDamageText(
        position: e.position - Vector2(0, e.size.y + 8),
        text: result.crit ? '${result.damage}!' : '${result.damage}',
        color: result.crit ? const Color(0xFFFFD400) : impactColor,
      ));
    }

    combatLog.value = '${result.skillName} → ${result.damage}${result.crit ? "!" : ""} dmg · ${hit.length} hit'
        '${result.statusEffect != null ? " · ${result.statusEffect}" : ""}';
    return true;
  }

  Color _impactColorForType(String type) {
    switch (type) {
      case 'Fire':      return const Color(0xFFFF6A1A);
      case 'Cold':      return const Color(0xFF4FD3FF);
      case 'Lightning': return const Color(0xFFFFD400);
      case 'Void':      return const Color(0xFF8B2FC9);
      default:          return const Color(0xFFC9C9C9);
    }
  }

  double cooldownOf(String skillId) => player?.cooldownRemaining(skillId) ?? 0.0;
}
