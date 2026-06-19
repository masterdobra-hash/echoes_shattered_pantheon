import 'dart:math' as math;
import 'package:flutter/material.dart';

class LootDropSpec {
  LootDropSpec({required this.label, required this.rarityColor, required this.offsetX, required this.offsetY});
  final String label;
  final Color rarityColor;
  final double offsetX, offsetY;
}

class LootSystem {
  final math.Random _rng = math.Random();
  static const common    = Color(0xFFB0B0B0);
  static const magic     = Color(0xFF4FA8E8);
  static const rare      = Color(0xFFE8C24F);
  static const epic      = Color(0xFFB048E8);
  static const legendary = Color(0xFFFF8C1A);

  List<LootDropSpec> rollBossLoot() {
    final drops = <LootDropSpec>[
      LootDropSpec(label: 'Shattered Greaves', rarityColor: common, offsetX: -70, offsetY: 16),
      LootDropSpec(label: 'Stone Talisman',    rarityColor: magic,  offsetX:  70, offsetY: 20),
      LootDropSpec(label: 'Marble Sigil',      rarityColor: rare,   offsetX: -22, offsetY: 38),
      LootDropSpec(label: 'Aegis of Fallen',   rarityColor: epic,   offsetX:  22, offsetY: 44),
    ];
    if (_rng.nextDouble() < 0.40) {
      drops.add(LootDropSpec(label: "Stormbreaker's Wrath", rarityColor: legendary, offsetX: 0, offsetY: -22));
    }
    return drops;
  }
}
