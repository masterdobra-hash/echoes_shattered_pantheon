import 'dart:convert';
import 'package:flutter/services.dart' show rootBundle;
import 'models.dart';

class ContentRegistry {
  final Map<String, SkillDefinition> skills = {};
  final Map<String, ShardDefinition> shards = {};
  final Map<String, EnemyDefinition> enemies = {};
  final Map<String, LegendaryDefinition> legendaries = {};

  Future<void> load() async {
    final s = jsonDecode(await rootBundle.loadString('assets/data/skills.json')) as Map<String, dynamic>;
    for (final raw in (s['skills'] as List)) {
      final d = SkillDefinition.fromMap(raw as Map<String, dynamic>);
      skills[d.id] = d;
    }
    final sh = jsonDecode(await rootBundle.loadString('assets/data/shards.json')) as Map<String, dynamic>;
    for (final raw in (sh['shards'] as List)) {
      final d = ShardDefinition.fromMap(raw as Map<String, dynamic>);
      shards[d.id] = d;
    }
    final e = jsonDecode(await rootBundle.loadString('assets/data/enemies_ruined_olympus.json')) as Map<String, dynamic>;
    for (final raw in (e['enemies'] as List)) {
      final d = EnemyDefinition.fromMap(raw as Map<String, dynamic>);
      enemies[d.id] = d;
    }
    final it = jsonDecode(await rootBundle.loadString('assets/data/items.json')) as Map<String, dynamic>;
    for (final raw in (it['legendaries'] as List)) {
      final d = LegendaryDefinition.fromMap(raw as Map<String, dynamic>);
      legendaries[d.id] = d;
    }
  }
}
