# Echoes of the Shattered Pantheon — Vertical Slice MVP

**Genre:** Offline-first **isometric top-down Action RPG (Diablo 2 / Path of Exile lineage)**.
**NOT first-person. NOT side-scroller.**

## Camera & Controls
- Top-down isometric world view, camera follows player
- **Tap-to-move** on Android (point-and-click style)
- Bottom HUD: HP orb (red, left), skill bar (10 active skills), Mana orb (blue, right)
- Top HUD: boss name, phase indicator, HP bar, combat log

## MVP Scope (this build)
- Class: **Titan Warrior** — 10 active skills + 5 passive skills
- Biome: **Ruined Olympus** — pseudo-isometric stone tile floor + broken Greco columns
- Boss: **The Fallen Hoplite** — 3 phases with attack telegraphs (red ground zone, #FF3030)
- Trash mobs: Broken Hoplite, Temple Archer, Marble Guardian, Titan Spawn
- Loot system: rarity beams (common/magic/rare/epic/legendary per Bible)
- 3 Core Shards · 3 Legendary items (data-only for now)
- Status effects: Bleed, Burn, Freeze, Shock, Curse (all 5 damage types active)
- Background music + 6 sound effects

## Architecture (6-layer per Bible)
```
lib/
├── data/                       # Layer 5 — JSON-driven content
│   ├── content_registry.dart
│   └── models.dart
├── systems/                    # Layer 3 — Gameplay systems
│   ├── combat_system.dart
│   └── loot_system.dart
├── entities/                   # Layer 4 — Entities
│   ├── player/titan_warrior_component.dart
│   ├── enemy/enemy_component.dart  (+ boss AI with 3 phases)
│   └── loot/loot_drop_component.dart
├── vfx/telegraph_component.dart  # AOE warnings, hit flashes, floating damage
├── game/
│   ├── echoes_game.dart        # Layer 2 — main FlameGame, tap-to-move
│   └── ground_tile_layer.dart  # Pseudo-isometric stone tiles
├── ui/game_shell.dart          # Layer 1 — HUD (orbs + skill bar)
└── main.dart                   # Layer 6 — entry, landscape lock

assets/
├── data/                       # skills.json, shards.json, enemies_*.json, items.json
└── audio/                      # 6 SFX + 2 BGM tracks (~3 MB total)
```

## How to get the APK

### Option A — GitHub Actions (recommended, zero local setup)
1. Create a new repo on GitHub
2. Push this project to the repo
3. GitHub Actions runs `.github/workflows/build_apk.yml` automatically
4. Within 3-5 minutes, download `echoes-shattered-pantheon-release.apk` from the Actions tab

```bash
git init
git add .
git commit -m "Echoes MVP"
git remote add origin <YOUR_REPO_URL>
git push -u origin main
```

### Option B — local build (needs ≥4 GB RAM)
```bash
flutter pub get
flutter build apk --release
# APK appears at: build/app/outputs/flutter-apk/app-release.apk
```

## Platform Targets
- ✅ Android — `com.echoes.shatteredpantheon`
- ✅ Windows — included in the Flutter project (build with `flutter build windows`)
- ⏳ iOS — add via `flutter create --platforms=ios .` (requires macOS for build/signing)
- ⏳ Steam release — Windows build → Steamworks pipeline

## Monetization Note
This MVP intentionally contains **no monetization**. Per project decision, "донат реализуется отдельно" — to be wired separately in a later milestone, not in the MVP code.
