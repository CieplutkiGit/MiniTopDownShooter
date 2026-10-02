# Changelog

## Unreleased - Asset Store readiness

### Added

- Reusable `WeaponDefinition` ScriptableObject
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
- Mouse aiming with left-click firing
- Asset Store setup and validation editor window
- Open-scene checks for core gameplay objects, gun setup, camera fallback, enemy spawning, waves, spawn zones, and NavMesh
- EditMode tests for `Health`, `WaveRunner`, and weapon ammo state
- Buyer-facing quick start, weapon, wave, dependency, extension, and licensing documentation

### Changed

- `Gun` can use a Weapon Definition while preserving existing inline serialized values as backwards-compatible defaults
- Legacy guns remain projectile/automatic/infinite-ammo by default
- `Projectile.Initialize` accepts optional per-weapon motion overrides without breaking old callers
- `PlayerShoot` routes to an active loadout weapon when available and retains its original single-gun fallback
- `InputReader` preserves twin-stick aim-to-fire while adding explicit fire/reload/switch actions
- `EnemySpawner` can spawn an explicit prefab into selected zones while preserving `ISpawner.SpawnOne()`
- `WaveController` translates authored WaveSets into runtime spawn plans while preserving inline wave data
- `WaveRunner` supports an optional initial delay without taking a dependency on Unity
- `PlayerController` uses an assigned aim camera or falls back to `Camera.main`
- Gun startup reports missing delivery configuration clearly
- Editor validation understands hitscan weapons and explicit-only wave compositions

## 1.0.0

- Initial playable top-down shooter foundation
- Player movement and aiming
- Projectile combat
- Enemy AI and pooling
- Waves and scoring
- Menus, UI, audio, and gameplay feedback
