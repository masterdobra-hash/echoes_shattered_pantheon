import 'dart:math' as math;
import 'package:flame/components.dart';
import 'package:flutter/material.dart';

import '../../data/models.dart';

class EnemyComponent extends PositionComponent {
  EnemyComponent({
    required this.definition, required Vector2 position, this.isBoss = false,
  }) : super(
          position: position,
          size: isBoss ? Vector2(130, 160) : Vector2(64, 88),
          anchor: Anchor.bottomCenter,
          priority: 4,
        ) {
    currentHp = definition.maxHp;
    maxHp = currentHp;
  }

  final EnemyDefinition definition;
  final bool isBoss;
  late int maxHp;
  late int currentHp;
  bool lootDropped = false;
  double _flashTimer = 0;
  double facingX = 1.0;

  // Status effects
  double burnTimer = 0;
  double freezeTimer = 0;
  double shockTimer = 0;
  double bleedTimer = 0;

  // Boss phases
  int phase = 1;
  double _attackCooldown = 2.5;
  double _telegraphCooldown = 4.0;

  bool get isAlive => currentHp > 0;
  double get hpRatio => currentHp / maxHp;

  void takeDamage(int amount, String? status) {
    if (!isAlive) return;
    currentHp = math.max(0, currentHp - amount);
    _flashTimer = 0.20;
    switch (status) {
      case 'Burn':   burnTimer = math.max(burnTimer, 3.0); break;
      case 'Freeze': freezeTimer = math.max(freezeTimer, 1.5); break;
      case 'Shock':  shockTimer = math.max(shockTimer, 2.0); break;
      case 'Bleed':  bleedTimer = math.max(bleedTimer, 4.0); break;
    }
  }

  @override
  void update(double dt) {
    super.update(dt);
    if (_flashTimer > 0) _flashTimer -= dt;
    if (burnTimer > 0)   { burnTimer   -= dt; if (burnTimer % 0.5 < dt) currentHp = math.max(0, currentHp - 3); }
    if (freezeTimer > 0) freezeTimer -= dt;
    if (shockTimer > 0)  shockTimer  -= dt;
    if (bleedTimer > 0)  { bleedTimer -= dt; if (bleedTimer % 0.7 < dt) currentHp = math.max(0, currentHp - 2); }

    if (isBoss && isAlive) {
      _updateBossPhase();
      _attackCooldown -= dt;
      _telegraphCooldown -= dt;
    }
  }

  void _updateBossPhase() {
    final r = hpRatio;
    final newPhase = r > 0.66 ? 1 : (r > 0.33 ? 2 : 3);
    if (newPhase != phase) phase = newPhase;
  }

  /// AI: when [tickAttack] returns telegraph spec (boss only).
  ({Vector2 center, double radius, int damage})? tickAttack(Vector2 playerPos) {
    if (!isBoss || !isAlive) return null;
    if (_telegraphCooldown > 0) return null;
    // Phase-based attack frequency
    final cd = phase == 1 ? 4.5 : (phase == 2 ? 3.2 : 2.0);
    _telegraphCooldown = cd;
    final radius = 110.0 + phase * 25;
    final dmg = definition.damage + (phase - 1) * 8;
    // Predict slightly ahead of player
    return (center: playerPos.clone(), radius: radius, damage: dmg);
  }

  @override
  void render(Canvas canvas) {
    if (!isAlive) {
      canvas.drawOval(
        Rect.fromCenter(center: Offset(size.x / 2, size.y - 6), width: size.x, height: 18),
        Paint()..color = const Color(0x55000000),
      );
      return;
    }
    canvas.drawOval(
      Rect.fromCenter(center: Offset(size.x / 2, size.y - 4), width: size.x * 0.9, height: 16),
      Paint()..color = const Color(0x88000000),
    );
    final baseColor = isBoss
        ? (phase == 1 ? const Color(0xFF8B2FC9) : (phase == 2 ? const Color(0xFFE83C3C) : const Color(0xFFFF3030)))
        : _roleColor(definition.role);
    var flash = _flashTimer > 0 ? const Color(0xFFFFE3A0) : baseColor;
    if (freezeTimer > 0) flash = Color.lerp(flash, const Color(0xFF4FD3FF), 0.5)!;
    if (burnTimer > 0)   flash = Color.lerp(flash, const Color(0xFFFF6A1A), 0.3)!;

    // Body silhouette
    final body = Path()
      ..moveTo(size.x / 2, 8)
      ..lineTo(size.x - 6, size.y - 18)
      ..lineTo(size.x / 2, size.y - 4)
      ..lineTo(6, size.y - 18)
      ..close();
    canvas.drawPath(body, Paint()..color = flash);

    // Head
    canvas.drawCircle(Offset(size.x / 2, isBoss ? 18 : 14), isBoss ? 18 : 10, Paint()..color = flash);

    // Weapon
    if (isBoss) {
      canvas.drawRect(Rect.fromLTWH(size.x - 12, 4, 10, size.y - 28),
          Paint()..color = const Color(0xFFCFCFCF));
      canvas.drawPath(
        Path()
          ..moveTo(size.x - 17, 0)
          ..lineTo(size.x - 7, 0)
          ..lineTo(size.x - 7, 14)
          ..lineTo(size.x - 17, 14)
          ..close(),
        Paint()..color = const Color(0xFFB8860B),
      );
      // Phase aura
      canvas.drawCircle(
        Offset(size.x / 2, size.y / 2),
        size.x * 0.7,
        Paint()
          ..color = baseColor.withOpacity(0.18)
          ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 20),
      );
    }

    // Status overlay rings
    if (shockTimer > 0) {
      canvas.drawCircle(Offset(size.x / 2, size.y / 2), size.x * 0.6,
          Paint()..color = const Color(0xFFFFD400)..style = PaintingStyle.stroke..strokeWidth = 2);
    }

    // HP bar
    final hpW = size.x;
    canvas.drawRect(Rect.fromLTWH(0, -10, hpW, 5), Paint()..color = const Color(0xCC222222));
    canvas.drawRect(Rect.fromLTWH(0, -10, hpW * hpRatio, 5),
        Paint()..color = isBoss ? const Color(0xFFFF3030) : const Color(0xFFE83C3C));
  }

  Color _roleColor(String role) {
    switch (role) {
      case 'tank':   return const Color(0xFF8C8C8C);
      case 'ranged': return const Color(0xFF4FA8E8);
      case 'elite':  return const Color(0xFFB048E8);
      default:       return const Color(0xFFB8860B);
    }
  }
}
