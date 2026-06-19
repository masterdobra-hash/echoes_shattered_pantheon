class SkillDefinition {
  SkillDefinition({
    required this.id, required this.name, required this.kind,
    required this.damageType, required this.baseDamage, required this.cooldown,
    required this.windupMs, required this.impactMs, required this.recoveryMs,
    required this.radius, required this.manaCost, this.description,
  });
  final String id, name, kind, damageType;
  final int baseDamage, windupMs, impactMs, recoveryMs, manaCost;
  final double cooldown, radius;
  final String? description;
  factory SkillDefinition.fromMap(Map<String, dynamic> m) => SkillDefinition(
    id: m['id'], name: m['name'], kind: m['kind'],
    damageType: m['damageType'], baseDamage: (m['baseDamage'] as num).toInt(),
    cooldown: (m['cooldown'] as num).toDouble(),
    windupMs: (m['windupMs'] as num).toInt(),
    impactMs: (m['impactMs'] as num).toInt(),
    recoveryMs: (m['recoveryMs'] as num).toInt(),
    radius: (m['radius'] as num).toDouble(),
    manaCost: (m['manaCost'] as num).toInt(),
    description: m['description'] as String?,
  );
}

class ShardDefinition {
  ShardDefinition({required this.id, required this.name, required this.slot, required this.description});
  final String id, name, slot, description;
  factory ShardDefinition.fromMap(Map<String, dynamic> m) =>
      ShardDefinition(id: m['id'], name: m['name'], slot: m['slot'], description: m['description']);
}

class EnemyDefinition {
  EnemyDefinition({required this.id, required this.name, required this.role, required this.maxHp, required this.moveSpeed, required this.damage});
  final String id, name, role;
  final int maxHp, damage;
  final double moveSpeed;
  factory EnemyDefinition.fromMap(Map<String, dynamic> m) => EnemyDefinition(
    id: m['id'], name: m['name'], role: m['role'],
    maxHp: (m['maxHp'] as num).toInt(),
    moveSpeed: (m['moveSpeed'] as num).toDouble(),
    damage: (m['damage'] as num).toInt(),
  );
}

class LegendaryDefinition {
  LegendaryDefinition({required this.id, required this.name, required this.slot, required this.mod});
  final String id, name, slot, mod;
  factory LegendaryDefinition.fromMap(Map<String, dynamic> m) =>
      LegendaryDefinition(id: m['id'], name: m['name'], slot: m['slot'], mod: m['mod']);
}
