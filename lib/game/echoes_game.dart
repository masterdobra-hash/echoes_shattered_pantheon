import "dart:math" as math;
import "package:flame/components.dart";
import "package:flame/events.dart";
import "package:flame/experimental.dart";
import "package:flame/game.dart";
import "package:flame_audio/flame_audio.dart";
import "package:flutter/material.dart";

import "../data/content_registry.dart";
import "../entities/enemy/enemy_component.dart";
import "../entities/loot/loot_drop_component.dart";
import "../entities/player/titan_warrior_component.dart";
import "../systems/loot_system.dart";
import "../vfx/telegraph_component.dart";
import "ground_tile_layer.dart";

/// Top-down isometric ARPG (Diablo 2 lineage).
class EchoesGame extends FlameGame with TapCallbacks {
  EchoesGame({required this.contentRegistry})
      : super(
          camera: CameraComponent.withFixedResolution(width: 1280, height: 720),
        );

  final ContentRegistry contentRegistry;
  final LootSystem lootSystem = LootSystem();

  final ValueNotifier<String> combatLog =
      ValueNotifier<String>("Ruined Olympus — tap the ground to move, tap skills to fight.");
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

  late final World gameWorld;
  late final TitanWarriorComponent player;
  late final EnemyComponent boss;
  final List<EnemyComponent> trash = [];
  bool _audioReady = false;

  @override
  Future<void> onLoad() async {
    // Set background color of the rendered area so screen is NEVER pure black
    // even before world is built.
    // (FlameGame has no built-in backgroundColor in 1.23; we set via render.)

    gameWorld = World();
    await add(gameWorld);
    camera.world = gameWorld;

    // Audio is optional — never let it block onLoad
    _loadAudioBackground();

    await gameWorld.add(GroundTileLayer(size: worldSize));

    player = TitanWarriorComponent(
      contentRegistry: contentRegistry,
      position: Vector2(worldSize.x / 2, worldSize.y / 2 + 220),
    );
    playerHp.value = player.currentHp;
    playerMaxHp.value = player.maxHp;
    playerMana.value = player.currentMana;
    playerMaxMana.value = player.maxMana;
    await gameWorld.add(player);

    final bossDef = contentRegistry.enemies["the_fallen_hoplite"]!;
    boss = EnemyComponent(
      definition: bossDef,
      position: Vector2(worldSize.x / 2, 320),
      isBoss: true,
    );
    bossMaxHp.value = boss.maxHp;
    bossHp.value = boss.currentHp;
    await gameWorld.add(boss);

    final rng = math.Random(7);
    final roster = ["broken_hoplite", "temple_archer", "marble_guardian", "titan_spawn"];
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
      await gameWorld.add(mob);
    }

    // Camera follows player. In Flame 1.23 use camera.follow.
    camera.follow(player, snap: true);
  }

  Future<void> _loadAudioBackground() async {
    try {
      await FlameAudio.audioCache.loadAll([
        "sfx_titan_slam.mp3",
        "sfx_inferno_strike.mp3",
        "sfx_aegis_ward.mp3",
        "sfx_boss_telegraph.mp3",
        "sfx_loot_drop.mp3",
        "sfx_enemy_death.mp3",
        "bgm_ruined_olympus.mp3",
        "bgm_fallen_hoplite.mp3",
      ]);
      _audioReady = true;
      FlameAudio.bgm.initialize();
      await FlameAudio.bgm.play("bgm_ruined_olympus.mp3", volume: 0.45);
    } catch (_) {
      _audioReady = false;
    }
  }

  @override
  void render(Canvas canvas) {
    // Paint a fallback background so we never see pure black if world fails.
    final r = canvas.getLocalClipBounds();
    canvas.drawRect(r, Paint()..color = const Color(0xFF1E1A18));
    super.render(canvas);
  }

  @override
  void update(double dt) {
    super.update(dt);
    if (!player.isAlive) {
      playerDead.value = true;
      return;
    }
    playerHp.value = player.currentHp;
    playerMana.value = player.currentMana;
    bossHp.value = boss.currentHp;
    bossPhase.value = boss.phase;

    for (final mob in trash) {
      if (!mob.isAlive) continue;
      final delta = player.position - mob.position;
      final dist = delta.length;
      final canMove = mob.freezeTimer <= 0;
      if (canMove && dist > 30) {
        mob.position += delta.normalized() * mob.definition.moveSpeed * dt;
      } else if (dist < 50) {
        if (math.Random().nextDouble() < dt * 0.7) {
          player.takeDamage(mob.definition.damage);
          combatLog.value = "${mob.definition.name} hits you for ${mob.definition.damage}";
        }
      }
    }

    final attack = boss.tickAttack(player.position);
    if (attack != null) {
      _spawnBossTelegraphAndStrike(attack.center, attack.radius, attack.damage);
    }

    if (!boss.isAlive && !boss.lootDropped) {
      boss.lootDropped = true;
      bossDefeated.value = true;
      _onBossKilled();
    }
  }

  Future<void> _spawnBossTelegraphAndStrike(Vector2 center, double radius, int damage) async {
    await gameWorld.add(TelegraphComponent(position: center, radius: radius, lifetime: 0.9));
    if (_audioReady) FlameAudio.play("sfx_boss_telegraph.mp3", volume: 0.7);
    Future.delayed(const Duration(milliseconds: 900), () {
      if (!player.isAlive) return;
      if (player.position.distanceTo(center) <= radius) {
        player.takeDamage(damage);
        combatLog.value = "Telegraph hits! -$damage HP";
        gameWorld.add(HitFlashComponent(
          position: player.position, color: const Color(0xFFFF3030), radius: 50,
        ));
      }
    });
  }

  void _onBossKilled() {
    combatLog.value = "The Fallen Hoplite is SHATTERED! Loot drops on the marble.";
    if (_audioReady) {
      FlameAudio.bgm.stop();
      FlameAudio.play("sfx_enemy_death.mp3", volume: 1.0);
    }
    final drops = lootSystem.rollBossLoot();
    for (final drop in drops) {
      gameWorld.add(LootDropComponent(
        position: boss.position + Vector2(drop.offsetX, drop.offsetY + 20),
        rarityColor: drop.rarityColor,
        label: drop.label,
      ));
    }
    if (_audioReady) {
      Future.delayed(const Duration(milliseconds: 300), () {
        FlameAudio.play("sfx_loot_drop.mp3", volume: 0.9);
      });
    }
  }

  @override
  void onTapDown(TapDownEvent event) {
    // Convert tap to world coordinates through the camera.
    final worldPoint = camera.globalToLocal(event.canvasPosition);
    player.commandMoveTo(worldPoint);
  }

  Future<bool> castSkill(String skillId) async {
    if (!player.isAlive) return false;
    final result = await player.castSkill(skillId, target: boss.position);
    if (result == null) return false;

    if (_audioReady) {
      final sfx = switch (skillId) {
        "titans_slam"    => "sfx_titan_slam.mp3",
        "inferno_strike" => "sfx_inferno_strike.mp3",
        "aegis_ward"     => "sfx_aegis_ward.mp3",
        _ => "sfx_titan_slam.mp3",
      };
      FlameAudio.play(sfx, volume: 0.85);
    }

    final impactColor = _impactColorForType(result.damageType);
    gameWorld.add(HitFlashComponent(
      position: player.position, color: impactColor, radius: result.radius * 0.6,
    ));

    final candidates = <EnemyComponent>[boss, ...trash];
    final hit = candidates.where((e) => e.isAlive && e.position.distanceTo(player.position) < result.radius).toList();
    for (final e in hit) {
      e.takeDamage(result.damage, result.statusEffect);
      gameWorld.add(FloatingDamageText(
        position: e.position - Vector2(0, e.size.y + 8),
        text: result.crit ? "${result.damage}!" : "${result.damage}",
        color: result.crit ? const Color(0xFFFFD400) : impactColor,
      ));
    }

    combatLog.value = "${result.skillName} → ${result.damage}${result.crit ? "!" : ""} dmg · ${hit.length} hit"
        "${result.statusEffect != null ? " · ${result.statusEffect}" : ""}";
    return true;
  }

  Color _impactColorForType(String type) {
    switch (type) {
      case "Fire":      return const Color(0xFFFF6A1A);
      case "Cold":      return const Color(0xFF4FD3FF);
      case "Lightning": return const Color(0xFFFFD400);
      case "Void":      return const Color(0xFF8B2FC9);
      default:          return const Color(0xFFC9C9C9);
    }
  }

  double cooldownOf(String skillId) => player.cooldownRemaining(skillId);
}
