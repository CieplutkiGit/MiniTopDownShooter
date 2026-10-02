# Mini Top Down Shooter

Mini Top Down Shooter is a lightweight Unity 6 framework and playable demo for building 3D top-down and twin-stick shooters.

The package focuses on a small, understandable runtime rather than taking over a customer's project. Core gameplay code is separated from Unity-facing code, enemies and effects are pooled, and common game systems are exposed through focused components and interfaces.

## Included systems

- Rigidbody player movement and rotation
- Mouse, keyboard, gamepad, and touch aiming/input
- Saved runtime input rebinding hooks
- Reusable Weapon Definition assets plus pistol, rifle, SMG, shotgun, and launcher presets
- Semi-auto, automatic, burst, and shotgun firing modes
- Projectile and hitscan weapon delivery
- Finite/infinite ammo, reload, empty-fire, ammo events, and ammo pickups
- Multi-weapon loadouts, next/previous switching, and weapon pickups
- Projectile pooling with per-weapon speed/lifetime overrides
- Multi-projectile spread, spread growth, and recovery
- Optional hitscan penetration and damage falloff
- Health, damage, and optional damage modifiers
- Legacy melee chase enemy plus shipped ranged, rusher, charger, tank/armor, and boss example prefabs
- NavMesh enemy movement
- Weighted enemy spawning with pooling
- Point, circle, and box spawn zones with distance/off-screen/NavMesh constraints
- Reusable Wave Set assets with per-wave enemy groups, weights, delays, zones, and boss entries
- Score and high score
- Dual-stick mobile controls, action buttons, safe-area support, and platform visibility
- Pause, menu, game-over, and HUD logic
- Audio hooks
- Screen shake, hit flash, muzzle flash, projectile trails, impact FX, and death FX
- Editor setup/validation tools with one-click Player, Enemy, Gun, Arena, Spawn Zone, Wave Controller and repair actions
- Custom inspectors for WeaponDefinition, WaveSet, EnemyStats and EnemySpawner
- EditMode tests plus PlayMode smoke coverage for SampleScene, ArenaShowcase, and MobileDemo
- GitHub Actions CI for Unity tests and a Linux smoke build

## Recommended Unity version

Developed with Unity 6000.3.10f1.

## Demo scenes

- `Assets/Scenes/SampleScene.unity`: minimal/legacy learning path.
- `Assets/Scenes/ArenaShowcase.unity`: authored WeaponDefinition, WaveSet, archetype enemies, boss entry, and spawn zones.
- `Assets/Scenes/MobileDemo.unity`: Arena showcase plus generated safe-area dual-stick touch controls.

## First steps

1. Open `Assets/Scenes/ArenaShowcase.unity` for the full framework showcase, or `SampleScene.unity` for the smallest learning scene.
2. Open **Tools > Mini Top Down Shooter > Setup & Validation**.
3. Click **Validate Open Scene**.
4. Enter Play Mode.
5. Create reusable weapon data with **Create Weapon Definition** and assign the asset to a `Gun`.
6. Create reusable wave composition with **Create Wave Set** and optionally add `SpawnZone` components.

See `QUICK_START.md` for setup, `WEAPONS.md` for weapon authoring, `WAVES.md` for wave composition, `ENEMIES.md` for archetypes, `MOBILE.md` for touch controls, `API.md` for extension points, `UPGRADING.md` for migrations, `CLEAN_IMPORT.md` for package verification, `REPLACE_ART.md` for presentation swaps, `RELEASE_CHECKLIST.md` for publishing, and `CI.md` for automated verification.

## Design goal

The demo is a reference implementation, not a project-settings takeover. Buyers should be able to replace art, create weapon variants, create enemy variants, tune waves, and integrate the runtime into an existing project without rewriting the framework.
