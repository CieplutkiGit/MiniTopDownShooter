# Changelog

## Unreleased - Asset Store readiness

### Added

- Reusable `WeaponDefinition` ScriptableObject
- Multi-projectile shots and configurable spread
- Reusable `WaveSet` ScriptableObject
- Mouse aiming with left-click firing
- Asset Store setup and validation editor window
- Open-scene checks for core gameplay objects, gun setup, camera fallback, enemy spawning, waves, and NavMesh
- EditMode tests for `Health` and `WaveRunner`
- Buyer-facing quick start, weapon, wave, dependency, extension, and licensing documentation

### Changed

- `Gun` can use a Weapon Definition while preserving existing inline serialized values as backwards-compatible defaults
- `WaveController` can use a Wave Set while preserving existing inline wave data
- `PlayerController` uses an assigned aim camera or falls back to `Camera.main`
- Gun startup reports missing projectile and spawn-point configuration clearly

## 1.0.0

- Initial playable top-down shooter foundation
- Player movement and aiming
- Projectile combat
- Enemy AI and pooling
- Waves and scoring
- Menus, UI, audio, and gameplay feedback
