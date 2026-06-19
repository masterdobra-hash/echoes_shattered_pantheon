import 'dart:math' as math;
import 'package:flame/components.dart';
import 'package:flutter/material.dart';

import '../../data/content_registry.dart';
import '../../data/models.dart';
import '../../systems/combat_system.dart';

enum ActorState { idle, move, castWindup, castRelease, recovery }

class SkillCastResult {
  SkillCastResult({required this.damage, required this.statusEffect,
      required this.skillName, required this.crit, required this.radius, required this.damageType});
  final int damage;
  final String? statusEffect;
  final String skillName, damageType;
  final bool crit;
  final double radius;
}

/// Top-down (Diablo 2-style) player.
/// Renders flat on ground plane. Movement is tap-to-move.
class TitanWarriorComponent extends PositionComponent {
  TitanWarriorComponent({required this.contentRegistry, required Vector2 position})
      : super(position: position, size: Vector2(72, 96), anchor: Anchor.bottomCenter, priority: 5);

  final ContentRegistry contentRegistry;
  final CombatSystem combatSystem = CombatSystem();

  Vector2? _moveTarget;
  double moveSpeed = 200.0;
  double facingX = 1.0;

  ActorState state = ActorState.idle;
  int level = 1;
  int strength = 16;
  double critChance = 0.12;
  double critDamage = 1.7;

  int maxHp = 220;
  int currentHp = 220;
  int maxMana = 100;
  int currentMana = 100;
  double _manaRegen = 0;
  double _hpRegen = 0;

  final Map<String, double> _cooldowns = {};

  void commandMoveTo(Vector2 target) {
    if (state == ActorState.castWindup || state == ActorState.castRelease) return;
    _moveTarget = target;
    state = ActorState.move;
  }

  @override
  void update(double dt) {
    super.update(dt);
    _cooldowns.updateAll((k, v) => (v - dt).clamp(0, 999));

    // Regen
    _manaRegen += dt;
    if (_manaRegen >= 0.5) {
      _manaRegen = 0;
      if (currentMana < maxMana) currentMana = math.min(maxMana, currentMana + 2);
    }
    _hpRegen += dt;
    if (_hpRegen >= 2.0) {
      _hpRegen = 0;
      if (currentHp < maxHp) currentHp = math.min(maxHp, currentHp + (maxHp * 0.01).round());
    }

    if (_moveTarget != null && state == ActorState.move) {
      final delta = _moveTarget! - position;
      final dist = delta.length;
      if (dist < 4) {
        _moveTarget = null;
        state = ActorState.idle;
      } else {
        final dir = delta.normalized();
        if (dir.x.abs() > 0.1) facingX = dir.x.sign;
        position += dir * moveSpeed * dt;
      }
    }
  }

  Future<SkillCastResult?> castSkill(String skillId, {required Vector2 target}) async {
    final skill = contentRegistry.skills[skillId];
    if (skill == null) return null;
    if (skill.kind != 'active') return null;
    if ((_cooldowns[skillId] ?? 0) > 0) return null;
    if (currentMana < skill.manaCost) return null;

    currentMana -= skill.manaCost;
    _moveTarget = null;
    final dirToTarget = target - position;
    if (dirToTarget.x.abs() > 1) facingX = dirToTarget.x.sign;
    state = ActorState.castWindup;
    await Future.delayed(Duration(milliseconds: skill.windupMs));
    state = ActorState.castRelease;
    final result = combatSystem.resolveSkill(
      skill: skill, strength: strength, critChance: critChance, critDamage: critDamage);
    _cooldowns[skillId] = skill.cooldown;
    Future.delayed(Duration(milliseconds: skill.recoveryMs), () {
      if (state == ActorState.castRelease) state = ActorState.idle;
    });
    return SkillCastResult(
      damage: result.damage, statusEffect: result.statusEffect,
      skillName: skill.name, crit: result.crit,
      radius: skill.radius, damageType: skill.damageType,
    );
  }

  double cooldownRemaining(String skillId) => _cooldowns[skillId] ?? 0;

  void takeDamage(int amount) {
    currentHp = math.max(0, currentHp - amount);
  }

  bool get isAlive => currentHp > 0;

  @override
  void render(Canvas canvas) {
    // Foot shadow on ground (pseudo-isometric)
    canvas.drawOval(
      Rect.fromCenter(center: Offset(size.x / 2, size.y - 4), width: 60, height: 18),
      Paint()..color = const Color(0x88000000),
    );

    // Cape/armor body — diamond silhouette
    final body = Path()
      ..moveTo(size.x / 2, 16)
      ..lineTo(size.x - 6, size.y - 26)
      ..lineTo(size.x / 2, size.y - 8)
      ..lineTo(6, size.y - 26)
      ..close();
    canvas.drawPath(
      body,
      Paint()..shader = const LinearGradient(
        begin: Alignment.topCenter, end: Alignment.bottomCenter,
        colors: [Color(0xFFC9A24B), Color(0xFF6E531F)],
      ).createShader(Rect.fromLTWH(0, 0, size.x, size.y)),
    );
    // Belt
    canvas.drawRect(
      Rect.fromLTWH(10, size.y - 44, size.x - 20, 7),
      Paint()..color = const Color(0xFF3A2A12),
    );
    // Helmet
    canvas.drawCircle(Offset(size.x / 2, 18), 15, Paint()..color = const Color(0xFFE8C682));
    canvas.drawArc(
      Rect.fromCircle(center: Offset(size.x / 2, 18), radius: 15),
      math.pi, math.pi, false,
      Paint()..color = const Color(0xFFB8860B)..style = PaintingStyle.stroke..strokeWidth = 3,
    );
    // Plume (Greco hoplite)
    canvas.drawRect(
      Rect.fromLTWH(size.x / 2 - 3, 0, 6, 16),
      Paint()..color = const Color(0xFF7A1F1F),
    );

    // Warhammer — big silhouette (>40% of height per Bible)
    canvas.save();
    canvas.translate(size.x / 2, size.y / 2);
    canvas.scale(facingX, 1);
    final hammer = Path()
      ..moveTo(20, -size.y / 2 + 30)
      ..lineTo(30, -size.y / 2 + 60)
      ..lineTo(8, size.y / 2 - 26)
      ..lineTo(-2, size.y / 2 - 32)
      ..close();
    canvas.drawPath(hammer, Paint()..color = const Color(0xFF2A2A2A));
    canvas.drawRect(
      Rect.fromLTWH(18, -size.y / 2 + 24, 26, 18),
      Paint()..color = const Color(0xFFB8860B),
    );
    canvas.drawRect(
      Rect.fromLTWH(18, -size.y / 2 + 24, 26, 18),
      Paint()..color = const Color(0xFF6B4A11)..style = PaintingStyle.stroke..strokeWidth = 2,
    );
    canvas.restore();

    // Cast windup glow
    if (state == ActorState.castWindup || state == ActorState.castRelease) {
      canvas.drawCircle(
        Offset(size.x / 2, size.y / 2),
        58,
        Paint()..color = const Color(0x66FF8C1A)..maskFilter = const MaskFilter.blur(BlurStyle.normal, 14),
      );
    }
  }
}
