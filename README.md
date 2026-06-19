# Echoes: Shattered Pantheon

Top-down isometric ARPG (Diablo II style), tap-to-move. Built on **Unity 6 LTS** (6000.0.23f1).

## Stack
- **Engine**: Unity 6000.0.23f1 LTS
- **Render**: URP 2D Renderer
- **Scripting**: C# / IL2CPP
- **Input**: Unity Input System (tap-to-move)
- **Targets**: Android, iOS, Windows (Steam)
- **Package ID**: `com.echoes.shatteredpantheon`

## MVP scope
- Playable character: **Titan Warrior** (10 active + 5 passive skills)
- Location: **Ruined Olympus** (5 enemy types)
- Boss: **The Fallen Hoplite** (3-phase AI with telegraphs)
- HUD: HP/Mana orbs + skill bar
- Loot: 3 legendary items + 3 shards

## Build via GitHub Actions (GameCI)
Workflow `.github/workflows/build_apk.yml` builds a release APK on every push to `main`.

### Required GitHub Secrets
| Secret | Description |
|---|---|
| `UNITY_LICENSE` | Contents of `Unity_lic.ulf` (Personal license file) |
| `UNITY_EMAIL` | Unity ID email |
| `UNITY_PASSWORD` | Unity ID password |

### Manual trigger
Actions tab → "Build Android APK" → Run workflow → main.
APK artifact `echoes-shattered-pantheon-apk` is uploaded after build (~10–15 min).

## Project layout
```
Assets/
  Scripts/    Core, Entities, Systems, UI, Input
  Resources/  Data (JSON), Audio
  Scenes/     Bootstrap, RuinedOlympus
  Prefabs/    Player, Enemy, Boss, Loot, Telegraph
  Settings/   URP Pipeline + Renderer2D
ProjectSettings/
Packages/
.github/workflows/
```
