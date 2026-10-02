# Changelog

## Unreleased - Asset Store readiness

### Added

- Reusable `WeaponDefinition` ScriptableObject
- Pistol, rifle, SMG, shotgun, and launcher WeaponDefinition presets plus matching Gun prefabs
- Semi-auto, automatic, burst, and shotgun firing modes
- Projectile and hitscan delivery modes
- Finite/infinite ammo, reload timing, auto-reload, empty-fire events, and reload cancellation hooks
- Per-instance `WeaponAmmoState` with EditMode tests
- Projectile speed/lifetime overrides
- Spread growth and recovery
- Hitscan range, layer mask, penetration, and optional damage falloff
- `WeaponLoadout` with slot equip, next/previous switching, and loadout events
- `WeaponPickup` and `AmmoPickup`
- Runtime fire/reload/weapon-switch input plus saved interactive rebinding hooks
- Reusable `WaveSet` ScriptableObject
- Per-wave enemy groups with guaranteed counts and weighted fill
- Initial wave delays and per-group delays
- Per-wave spawn-zone selection
- Point, circle, and box `SpawnZone` components with player-distance, off-screen, and NavMesh validation
- Optional per-wave boss entries
- Authored Arena showcase WaveSet using enemy groups, zone selection, and a boss entry
- Modular EnemyBehaviorBase extension path while preserving the original melee state machine
- RangedEnemyBehavior that reuses the Gun/WeaponDefinition framework
- RusherEnemyBehavior and ChargerEnemyBehavior examples
- EnemyArmor and IDamageModifier support for tank/bruiser variants
- BossPhaseController with pooled phase reset and phase-change events
- MobileInputState shared touch input model
- Dual MobileJoystick controls with dead zones, multi-touch ownership, and optional aim-to-fire
- MobileActionButton for fire, reload, weapon switching, and pause
- SafeAreaFitter and MobileControlsVisibility
- ArenaShowcase and MobileDemo scenes plus demo touch-control bootstrap
- Shared DamageAffiliation combat teams and friendly-fire rules for projectile/hitscan weapons
- Mouse aiming with left-click firing
- Asset Store setup and validation editor window
- One-click Player, Enemy, Gun, Arena, Spawn Zone and Wave Controller creation plus common-reference repair
- Custom inspectors for WeaponDefinition, WaveSet, EnemyStats and EnemySpawner
- Open-scene checks for core gameplay objects, gun setup, camera fallback, enemy spawning, waves, spawn zones, and NavMesh
- EditMode tests for `Health`, `WaveRunner`, weapon ammo state, mobile input state, enemy armor, and combat affiliation
- PlayMode smoke tests for all three demo scenes
- GitHub Actions Unity CI with test execution and Linux player smoke build
- Buyer-facing quick start, weapon, wave, dependency, extension, licensing, API, upgrade, clean-import, art-replacement, CI and release-checklist documentation

### Changed

- Demo scenes and baked NavMesh moved under `Assets/_Project/Scenes` so the commercial package can be exported from one root.
- Removed unused Collab, Rider, Visual Studio, Multiplayer Center, Timeline, Visual Scripting and VContainer direct dependencies
- Version 1.0 support target is pinned to Unity 6000.3.10f1 until other Unity releases are explicitly tested
- `Gun` can use a Weapon Definition while preserving existing inline serialized values as backwards-compatible defaults
- Legacy guns remain projectile/automatic/infinite-ammo by default
- `Projectile.Initialize` accepts optional per-weapon motion overrides plus source affiliation without breaking old callers
- `PlayerShoot` routes to an active loadout weapon when available and retains its original single-gun fallback
- `InputReader` preserves twin-stick aim-to-fire while adding explicit fire/reload/switch actions
- `EnemySpawner` can spawn an explicit prefab into selected zones while preserving `ISpawner.SpawnOne()`
- `WaveController` translates authored WaveSets into runtime spawn plans while preserving inline wave data
- `WaveRunner` supports an optional initial delay without taking a dependency on Unity
- `PlayerController` uses an assigned aim camera or falls back to `Camera.main`
- Gun startup reports missing delivery configuration clearly
- Editor validation understands hitscan weapons, explicit-only wave compositions, enemy archetype requirements, and mobile control setup
- `InputReader` merges touch input with existing mouse, keyboard, and gamepad input
- `HealthComponent` resolves optional `IDamageModifier` components before applying damage
- Player/enemy weapon damage now uses source-root and team-aware friendly-fire filtering

## 1.0.0

- Initial playable top-down shooter foundation
- Player movement and aiming
- Projectile combat
- Enemy AI and pooling
- Waves and scoring
- Menus, UI, audio, and gameplay feedback
