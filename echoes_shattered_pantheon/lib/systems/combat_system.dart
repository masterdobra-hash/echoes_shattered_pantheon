import 'dart:math' as math;
import '../data/models.dart';

class CombatResult {
  CombatResult({required this.damage, required this.statusEffect, required this.crit});
  final int damage;
  final String? statusEffect;
  final bool crit;
}

class CombatSystem {
  final math.Random _rng = math.Random();
  CombatResult resolveSkill({
    required SkillDefinition skill, required int strength,
    required double critChance, required double critDamage,
  }) {
    final base = skill.baseDamage + (strength * 2);
    final crit = _rng.nextDouble() < critChance;
    final dmg = crit ? (base * critDamage).round() : base;
    final st = switch (skill.damageType) {
      'Physical' => 'Bleed',
      'Fire' => 'Burn',
      'Cold' => 'Freeze',
      'Lightning' => 'Shock',
      'Void' => 'Curse',
      _ => null,
    };
    return CombatResult(damage: dmg, statusEffect: st, crit: crit);
  }
}
